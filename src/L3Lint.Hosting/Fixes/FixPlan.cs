using L3Lint.Core;
using L3Lint.Core.Fixes;

namespace L3Lint.Hosting.Fixes;

/// <summary>
/// The mechanical fixes for one transaction, merged into one RFC 6902 patch, and the findings left for a person.
/// </summary>
/// <param name="TransactionIndex">The transaction the patch belongs to.</param>
/// <param name="Operations">The merged operations, in pointer order.</param>
/// <param name="TargetsCanonical">Whether the patch targets the canonical document (CSV input) rather than the source.</param>
/// <param name="Applied">The findings the patch fixes.</param>
/// <param name="NeedsHuman">Findings a person must decide: proposals, findings with no mechanical fix, and amount changes without <c>--fix-amount</c>.</param>
public sealed record FixPlan(
    int TransactionIndex,
    IReadOnlyList<PatchOperation> Operations,
    bool TargetsCanonical,
    IReadOnlyList<Finding> Applied,
    IReadOnlyList<Finding> NeedsHuman);
