using System.Text;
using L3Lint.Core.Packs;
using L3Lint.Core.Rules;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Core.Tests;

/// <summary>
/// Writes a small rule pack to a temporary folder, with a correct manifest unless a test breaks it on purpose,
/// so tests can exercise the pack loader and every check kind through the real engine.
/// </summary>
internal sealed class TestPack : IDisposable
{
    public const string Header = """
        pack:
          id: test.rules.general
          version: 1.0.0
          brand: general
          publishedOn: 2026-10-25
        """;

    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public TestPack(string rulesYaml)
    {
        Folder = Path.Combine(Path.GetTempPath(), "l3lint-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
        _files["test.rules.yaml"] = rulesYaml;
    }

    public string Folder { get; }

    /// <summary>A rule in the general scope with the given check, ready to append under <c>rules:</c>.</summary>
    public static string Rule(string id, string check, string message = "Problem at {field}.", string extra = "") => $"""
          - id: {id}
            title: Test rule {id}
            level: general
            severity: error
            confidence: policy
            check: {check}
            message: "{message}"
            fix: Fix it.
            impact: hygiene
            sources: [https://example.org/spec]
        {extra}
        """;

    public static string Pack(params string[] rules) => Header + "\nrules:\n" + string.Concat(rules);

    public TestPack With(string relativePath, string content)
    {
        _files[relativePath] = content;
        return this;
    }

    /// <summary>Writes the files and a manifest; <paramref name="tamper"/> may change the manifest before it is written.</summary>
    public DirectoryRulePackSource Write(Action<SortedDictionary<string, string>>? tamper = null, bool manifest = true)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (path, content) in _files)
        {
            var full = Path.Combine(Folder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            var bytes = Encoding.UTF8.GetBytes(content.ReplaceLineEndings("\n"));
            File.WriteAllBytes(full, bytes);
            hashes[path] = PackHashing.Sha256(bytes);
        }

        if (manifest)
        {
            tamper?.Invoke(hashes);
            var text = new PackManifest { Pack = "test.rules.general", Version = "1.0.0", Files = hashes }.Write();
            File.WriteAllText(Path.Combine(Folder, PackManifest.FileName), text);
        }

        return new DirectoryRulePackSource(Folder);
    }

    public ILinter Build() => Linter(Write());

    public static ILinter Linter(IRulePackSource source) =>
        new LinterBuilder()
            .AddRulePack(source)
            .UseTimeProvider(new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)))
            .Build();

    public void Dispose() => Directory.Delete(Folder, recursive: true);
}
