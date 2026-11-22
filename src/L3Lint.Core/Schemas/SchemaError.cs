namespace L3Lint.Core.Schemas;

/// <summary>One schema violation.</summary>
/// <param name="Pointer">RFC 6901 pointer to the offending value (or to the object missing a property).</param>
/// <param name="Keyword">The JSON Schema keyword that failed, for example "type" or "additionalProperties".</param>
/// <param name="Detail">The validator's own description, kept for diagnostics only; never shown as a user message.</param>
internal readonly record struct SchemaError(string Pointer, string Keyword, string Detail);
