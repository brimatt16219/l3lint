using System.Text;

namespace L3Lint.Fuzz.Tests;

/// <summary>Seed inputs per target, read from the repository's own fixtures, spec examples and packs.</summary>
internal static class Seeds
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    public static string Root => RootPath.Value;

    public static IReadOnlyList<byte[]> For(string target) => target switch
    {
        "json" => Json(),
        "config" => Read(Path.Combine("tests", "L3Lint.Core.Tests", "SpecExamples", "config-example.yaml"), Path.Combine("src", "L3Lint.Cli", "Templates", "l3lint.yaml")),
        "csv" => Files(Path.Combine("fixtures", "adapters", "csv"), "*.csv"),
        "rulepack" => Files("src", "*.rules.yaml"),
        _ => throw new ArgumentException("Unknown target " + target, nameof(target)),
    };

    private static List<byte[]> Json()
    {
        var seeds = Read(Path.Combine("tests", "L3Lint.Core.Tests", "SpecExamples", "canonical-example.json"));
        foreach (var gateway in new[] { "stripe", "adyen", "braintree", "cybersource", "authorizenet", "checkoutcom" })
        {
            seeds.AddRange(Files(Path.Combine("fixtures", "adapters", gateway), "*.json").Take(4));
        }

        // An NDJSON batch of three compact copies of the spec example.
        var compact = System.Text.Json.Nodes.JsonNode.Parse(seeds[0])!.ToJsonString();
        seeds.Add(Encoding.UTF8.GetBytes(string.Join("\n", compact, compact, compact) + "\n"));
        return seeds;
    }

    private static List<byte[]> Read(params string[] relativePaths) => [.. relativePaths.Select(p => File.ReadAllBytes(Path.Combine(Root, p)))];

    private static List<byte[]> Files(string folder, string pattern) =>
        [.. Directory.EnumerateFiles(Path.Combine(Root, folder), pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(File.ReadAllBytes)];

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("L3Lint.slnx was not found above " + AppContext.BaseDirectory);
    }
}
