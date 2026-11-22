using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;
using L3Lint.Hosting;

namespace L3Lint.Fuzz;

/// <summary>
/// A rule pack held in memory: one fuzzed <c>fuzz.rules.yaml</c>, the list and example files of the five shipped
/// packs (so references in a mutated shipped pack still resolve), and a manifest written to match them.
/// </summary>
/// <remarks>
/// The manifest takes the pack ID and version from the body's first <c>id:</c> and <c>version:</c> lines, and hashes
/// files the way the manifest format documents (SHA-256 after CRLF becomes LF), so most inputs pass the checksum and
/// the identity check and the fuzzer explores the stages behind them.
/// </remarks>
public sealed partial class InMemoryPackSource : IRulePackSource
{
    public const string RulesFile = "fuzz.rules.yaml";

    private const string ManifestFile = "manifest.json";

    private static readonly Lazy<Shared> Support = new(LoadSupport);

    private readonly Dictionary<string, byte[]> _files;

    public InMemoryPackSource(byte[] rulesBody)
    {
        ArgumentNullException.ThrowIfNull(rulesBody);
        _files = new Dictionary<string, byte[]>(Support.Value.Files, StringComparer.Ordinal) { [RulesFile] = rulesBody };
        _files[ManifestFile] = Manifest(rulesBody, _files);
        Files = [.. _files.Keys.Order(StringComparer.Ordinal)];
    }

    public string Origin => "fuzz";

    public IReadOnlyList<string> Files { get; }

    public IReadOnlyList<ICustomCheck> CustomChecks => Support.Value.CustomChecks;

    public IReadOnlyList<IBatchCheck> BatchChecks => Support.Value.BatchChecks;

    public ITierPolicy? TierPolicy => null;

    public Stream Open(string relativePath) =>
        _files.TryGetValue(relativePath, out var bytes) ? new MemoryStream(bytes, writable: false) : throw new FileNotFoundException(relativePath);

    private static byte[] Manifest(byte[] rulesBody, Dictionary<string, byte[]> files)
    {
        var text = Encoding.UTF8.GetString(rulesBody);
        var id = IdLine().Match(text) is { Success: true } idMatch ? idMatch.Groups[1].Value : "fuzz";
        var version = VersionLine().Match(text) is { Success: true } versionMatch ? versionMatch.Groups[1].Value : "1.0.0";

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("pack", id);
            writer.WriteString("version", version);
            writer.WriteString("algorithm", "sha256");
            writer.WriteStartObject("files");
            foreach (var (name, bytes) in files.Where(f => f.Key != ManifestFile).OrderBy(f => f.Key, StringComparer.Ordinal))
            {
                writer.WriteString(name, Sha256(bytes));
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return buffer.ToArray();
    }

    // SHA-256 of the bytes with every CR that starts a CRLF removed, as documented for pack manifests.
    private static string Sha256(byte[] bytes)
    {
        var normalized = new List<byte>(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!(bytes[i] == (byte)'\r' && i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n'))
            {
                normalized.Add(bytes[i]);
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData([.. normalized]));
    }

    private static Shared LoadSupport()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var checks = new Dictionary<string, ICustomCheck>(StringComparer.Ordinal);
        var batchChecks = new Dictionary<string, IBatchCheck>(StringComparer.Ordinal);
        foreach (var pack in L3LintFactory.EmbeddedPacks())
        {
            foreach (var file in pack.Files.Where(f => f != ManifestFile && !(f.EndsWith(".rules.yaml", StringComparison.Ordinal) && !f.Contains('/', StringComparison.Ordinal))))
            {
                if (!files.ContainsKey(file))
                {
                    using var stream = pack.Open(file);
                    using var copy = new MemoryStream();
                    stream.CopyTo(copy);
                    files[file] = copy.ToArray();
                }
            }

            foreach (var check in pack.CustomChecks)
            {
                checks.TryAdd(check.Name, check);
            }

            foreach (var check in pack.BatchChecks)
            {
                batchChecks.TryAdd(check.GetType().FullName!, check);
            }
        }

        return new Shared(files, [.. checks.Values], [.. batchChecks.Values]);
    }

    [GeneratedRegex(@"^\s*id:\s*[""']?([^""'\s#]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex IdLine();

    [GeneratedRegex(@"^\s*version:\s*[""']?([^""'\s#]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex VersionLine();

    private sealed record Shared(IReadOnlyDictionary<string, byte[]> Files, IReadOnlyList<ICustomCheck> CustomChecks, IReadOnlyList<IBatchCheck> BatchChecks);
}
