using System.Text.Json.Nodes;

namespace L3Lint.Core.Fixes;

/// <summary>
/// One RFC 6902 JSON Patch operation.
/// </summary>
/// <param name="Op">The operation: add, remove or replace (l3lint emits only these).</param>
/// <param name="Path">The JSON Pointer the operation targets.</param>
/// <param name="Value">The value for add and replace; <see langword="null"/> for remove.</param>
public sealed record PatchOperation(string Op, string Path, JsonNode? Value);
