using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace L3Lint.Core.Yaml;

/// <summary>Turns a JSON tree read from YAML into a detached <see cref="JsonElement"/> for schema validation and reading.</summary>
internal static class JsonElementFromYaml
{
    public static JsonElement Convert(JsonNode? node) =>
        node is null
            ? JsonDocument.Parse("null").RootElement.Clone()
            : JsonSerializer.SerializeToElement(node, YamlJsonContext.Default.JsonNode);
}

/// <summary>Source-generated metadata for JSON trees.</summary>
[JsonSerializable(typeof(JsonNode))]
internal sealed partial class YamlJsonContext : JsonSerializerContext;
