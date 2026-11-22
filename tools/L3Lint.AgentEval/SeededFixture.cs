using YamlDotNet.RepresentationModel;

namespace L3Lint.AgentEval;

/// <summary>A seeded-defect fixture from <c>fixtures/seeded</c> and the findings its mutation must cause.</summary>
internal sealed record SeededFixture(string Name, string Payload, string Operator, string Description, IReadOnlyDictionary<string, string> Context, IReadOnlyList<(string RuleId, string Path)> Defects)
{
    /// <summary>
    /// Loads up to <paramref name="limit"/> fixtures that carry at least one expected finding, taking operators in
    /// turn so a small sample still covers all ten.
    /// </summary>
    public static IReadOnlyList<SeededFixture> Load(string root, int limit)
    {
        var all = Directory.EnumerateFiles(Path.Combine(root, "fixtures", "seeded"), "*.json")
            .Order(StringComparer.Ordinal)
            .Select(Read)
            .Where(f => f.Defects.Count > 0)
            .ToList();
        var byOperator = all.GroupBy(f => f.Operator, StringComparer.Ordinal).Select(g => new Queue<SeededFixture>(g)).ToList();
        var picked = new List<SeededFixture>();
        while (picked.Count < limit && byOperator.Any(q => q.Count > 0))
        {
            foreach (var queue in byOperator.Where(q => q.Count > 0))
            {
                if (picked.Count == limit)
                {
                    break;
                }

                picked.Add(queue.Dequeue());
            }
        }

        return picked;
    }

    public static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Run the evaluation from inside the l3lint repository.");
    }

    private static SeededFixture Read(string payloadPath)
    {
        var yaml = new YamlStream();
        using (var reader = new StreamReader(payloadPath[..^".json".Length] + ".expect.yaml"))
        {
            yaml.Load(reader);
        }

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var expect = (YamlMappingNode)root["expect"];
        var context = ((YamlMappingNode)root["context"]).Children.ToDictionary(p => ((YamlScalarNode)p.Key).Value!, p => ((YamlScalarNode)p.Value).Value!, StringComparer.Ordinal);
        var defects = ((YamlSequenceNode)expect["findings"]).Children.Cast<YamlMappingNode>()
            .Select(f => (((YamlScalarNode)f["ruleId"]).Value!, ((YamlScalarNode)f["path"]).Value!))
            .ToList();
        return new SeededFixture(
            Path.GetFileNameWithoutExtension(payloadPath),
            File.ReadAllText(payloadPath),
            ((YamlScalarNode)root["operator"]).Value!,
            ((YamlScalarNode)root["description"]).Value!,
            context,
            defects);
    }
}
