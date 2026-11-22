using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>Source-generated serialization metadata for the canonical model.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Transaction))]
internal sealed partial class CanonicalJsonContext : JsonSerializerContext;
