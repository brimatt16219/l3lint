using System.Text.Json;
using System.Text.Json.Nodes;

namespace L3Lint.Mcp.Tests;

/// <summary>Payloads built from the spec's worked example by one named mutation each.</summary>
internal static class Payloads
{
    public static string SpecExampleText { get; } = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    /// <summary>Gets the spec example as a JSON object. It lints with exactly one Visa warning, VISA-L3-010.</summary>
    public static JsonElement SpecExample => Element(SpecExampleText);

    /// <summary>Line 1 total raised from 120.00 to 125.00; 10 x 12.50 - 5.00 = 120.00.</summary>
    public static JsonElement WrongLineTotal => Mutate(p => p["lineItems"]![0]!["total"] = 125.00m);

    /// <summary>The spec example with the given number of copies of line 1, for latency tests.</summary>
    public static JsonElement WithLines(int count) => Mutate(p =>
    {
        var line = p["lineItems"]![0]!;
        var lines = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            var copy = line.DeepClone();
            copy["sequence"] = i + 1;
            lines.Add(copy);
        }

        p["lineItems"] = lines;
    });

    public static JsonElement Mutate(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(SpecExampleText)!.AsObject();
        mutation(root);
        return Element(root.ToJsonString());
    }

    public static JsonElement Element(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
