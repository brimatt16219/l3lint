namespace L3Lint.Core.Engine;

/// <summary>
/// One GEN-FMT-001 problem: a schema violation, a value an adapter could not convert, or CSV rows that disagree.
/// </summary>
/// <param name="CanonicalPointer">The canonical pointer, or the empty string when the value maps to no canonical field.</param>
/// <param name="SourcePointer">The source location when it differs from what the path map gives.</param>
/// <param name="Detail">A plain message from resources describing the problem.</param>
internal sealed record SchemaIssue(string CanonicalPointer, string? SourcePointer, string Detail);
