using System.Text.Json;
using System.Text.Json.Serialization;

namespace L3Lint.Core.Packs;

/// <summary>The <c>manifest.json</c> of a rule pack: pack ID, version and the SHA-256 of every other file.</summary>
internal sealed class PackManifest
{
    public const string FileName = "manifest.json";

    [JsonPropertyName("pack")]
    public required string Pack { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; init; } = "sha256";

    [JsonPropertyName("files")]
    public required SortedDictionary<string, string> Files { get; init; }

    public static PackManifest Read(Stream stream) =>
        JsonSerializer.Deserialize(stream, PackManifestContext.Default.PackManifest)
        ?? throw new JsonException("The manifest is empty.");

    public string Write() => JsonSerializer.Serialize(this, PackManifestContext.Default.PackManifest).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
}

/// <summary>Source-generated metadata for pack manifests.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 2, NewLine = "\n")]
[JsonSerializable(typeof(PackManifest))]
internal sealed partial class PackManifestContext : JsonSerializerContext;
