using System.Text.Json;
using L3Lint.Core.Yaml;

namespace L3Lint.Core.Tests;

/// <summary>Reads the spec's own example blocks, copied verbatim into SpecExamples/.</summary>
internal static class SpecExamples
{
    public static string Text(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", name));

    public static JsonElement Json(string name) => JsonDocument.Parse(Text(name)).RootElement.Clone();

    public static JsonElement YamlAsJson(string name)
    {
        var node = YamlJson.Parse(Text(name)) ?? throw new InvalidOperationException(name + " is empty");
        return JsonSerializer.SerializeToElement(node, TestJsonContext.Default.JsonNode);
    }
}
