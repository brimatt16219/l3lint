using YamlDotNet.RepresentationModel;

namespace L3Lint.Corpus.Tests;

/// <summary>An expected finding: <c>Path</c> "*" accepts any path, and a null severity accepts any.</summary>
public sealed record Expected(string RuleId, string Path, string? Severity);

/// <summary>A corpus fixture and its <c>*.expect.yaml</c> (Test and delivery tab, "Fixture corpus").</summary>
public sealed record Fixture(
    string Name,
    string PayloadPath,
    string Description,
    string? Operator,
    IReadOnlyDictionary<string, string> Context,
    int? ExitCode,
    IReadOnlyList<Expected> Findings,
    IReadOnlyList<string> Absent,
    string? TierBrand,
    string? TierStatus)
{
    public static string RepositoryRoot { get; } = FindRoot();

    /// <summary>Loads every fixture of one corpus set, in ordinal name order.</summary>
    public static IReadOnlyList<Fixture> Load(string set)
    {
        var folder = Path.Combine(RepositoryRoot, "fixtures", set);
        return [.. Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(Read)];
    }

    public static Fixture Read(string payloadPath)
    {
        var expectPath = payloadPath[..^".json".Length] + ".expect.yaml";
        var yaml = new YamlStream();
        using (var reader = new StreamReader(expectPath))
        {
            yaml.Load(reader);
        }

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var expect = (YamlMappingNode)root["expect"];
        var context = ((YamlMappingNode)root["context"]).Children.ToDictionary(p => ((YamlScalarNode)p.Key).Value!, p => ((YamlScalarNode)p.Value).Value!, StringComparer.Ordinal);
        var findings = expect.Children.TryGetValue(new YamlScalarNode("findings"), out var list)
            ? ((YamlSequenceNode)list).Children.Cast<YamlMappingNode>().Select(f => new Expected(Scalar(f, "ruleId")!, Scalar(f, "path")!, Scalar(f, "severity"))).ToList()
            : [];
        var absent = expect.Children.TryGetValue(new YamlScalarNode("absent"), out var absentNode)
            ? ((YamlSequenceNode)absentNode).Children.Cast<YamlScalarNode>().Select(s => s.Value!).ToList()
            : [];
        var tier = expect.Children.TryGetValue(new YamlScalarNode("likelyTier"), out var tierNode) ? (YamlMappingNode)tierNode : null;
        return new Fixture(
            Path.GetRelativePath(Path.Combine(RepositoryRoot, "fixtures"), payloadPath).Replace('\\', '/'),
            payloadPath,
            Scalar(root, "description") ?? string.Empty,
            Scalar(root, "operator"),
            context,
            Scalar(expect, "exitCode") is { } code ? int.Parse(code, System.Globalization.CultureInfo.InvariantCulture) : null,
            findings,
            absent,
            tier is null ? null : Scalar(tier, "brand"),
            tier is null ? null : Scalar(tier, "status"));
    }

    public override string ToString() => Name;

    private static string? Scalar(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? ((YamlScalarNode)value).Value : null;

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("L3Lint.slnx not found above " + AppContext.BaseDirectory);
    }
}
