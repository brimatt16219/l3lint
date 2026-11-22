using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace L3Lint.Core.Pci;

/// <summary>Source-generated metadata for writing masked JSON trees.</summary>
[JsonSerializable(typeof(JsonNode))]
internal sealed partial class PciJsonContext : JsonSerializerContext;
