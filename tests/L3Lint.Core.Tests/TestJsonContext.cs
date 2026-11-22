using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace L3Lint.Core.Tests;

[JsonSerializable(typeof(JsonNode))]
internal sealed partial class TestJsonContext : JsonSerializerContext;
