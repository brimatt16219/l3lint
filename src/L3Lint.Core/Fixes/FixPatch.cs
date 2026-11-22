namespace L3Lint.Core.Fixes;

/// <summary>
/// A mechanical fix for one finding, as JSON Patch operations against the source format (FR-027, ADR-0022).
/// </summary>
/// <param name="Operations">The operations, in order.</param>
/// <param name="TargetsCanonical">
/// <see langword="true"/> when the operations target the canonical document rather than the source format
/// (CSV input, which has no pointer syntax).
/// </param>
public sealed record FixPatch(IReadOnlyList<PatchOperation> Operations, bool TargetsCanonical = false);
