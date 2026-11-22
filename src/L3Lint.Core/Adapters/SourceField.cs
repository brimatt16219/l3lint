using System.Text.Json.Nodes;
using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Converts a canonical value into the value the source format expects, for fix patches.
/// </summary>
/// <param name="canonicalValue">The canonical JSON value, for example 120.00 for a line total.</param>
/// <returns>The source JSON value, for example 12000 for a minor-unit format.</returns>
public delegate JsonNode? SourceValueConverter(JsonNode? canonicalValue);

/// <summary>
/// Where a canonical value lives in the source format.
/// </summary>
/// <param name="Pointer">The source location: a JSON Pointer, or a "row 14, column F" label for CSV.</param>
/// <param name="ToSource">Converts canonical values to source values for patches; <see langword="null"/> means the value is written unchanged.</param>
/// <param name="Location">A known file position (CSV); JSON positions come from the document's token index.</param>
public readonly record struct SourceField(string Pointer, SourceValueConverter? ToSource = null, SourceLocation? Location = null);
