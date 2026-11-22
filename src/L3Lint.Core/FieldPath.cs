namespace L3Lint.Core;

/// <summary>
/// Where a finding points, in canonical form and in the user's original format (FR-007).
/// </summary>
/// <param name="Canonical">An RFC 6901 pointer into the canonical model, such as /lineItems/0/total. The empty string is the document root.</param>
/// <param name="Source">The same location in the original format, such as /amount_details/line_items/0/unit_cost for Stripe or "row 14, column F" for CSV; <see langword="null"/> when the format cannot carry the field.</param>
public readonly record struct FieldPath(string Canonical, string? Source);
