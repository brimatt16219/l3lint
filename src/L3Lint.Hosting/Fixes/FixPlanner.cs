using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Fixes;

namespace L3Lint.Hosting.Fixes;

/// <summary>
/// Builds the patch for <c>fix</c> and <c>suggest_fixes</c> (FR-027, US-06, ADR-0022).
/// </summary>
/// <remarks>
/// Only mechanical patches are applied: proposals (such as BX to PCE) are listed for a person, and the
/// transaction amount changes only with <c>includeAmount</c> (<c>--fix-amount</c>). When two findings patch the
/// same pointer, the first in finding order wins; object values added at the same pointer are merged, so
/// "add /amounts {discount}" and "add /amounts {duty}" become one operation.
/// </remarks>
public static class FixPlanner
{
    private const string AmountPointer = "/amount";

    /// <summary>
    /// Plans the fixes for each transaction of a result.
    /// </summary>
    /// <param name="result">The lint result.</param>
    /// <param name="includeAmount">Whether to recompute the transaction amount.</param>
    /// <returns>One plan per transaction.</returns>
    public static IReadOnlyList<FixPlan> Plan(LintResult result, bool includeAmount)
    {
        ArgumentNullException.ThrowIfNull(result);
        return [.. result.Transactions.Where(t => t.Kind == TransactionResultKind.Transaction).Select(t => Plan(t, includeAmount))];
    }

    /// <summary>
    /// Plans the fixes for one transaction.
    /// </summary>
    /// <param name="transaction">The transaction result.</param>
    /// <param name="includeAmount">Whether to recompute the transaction amount.</param>
    /// <returns>The plan.</returns>
    public static FixPlan Plan(TransactionResult transaction, bool includeAmount)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        var operations = new SortedDictionary<string, PatchOperation>(StringComparer.Ordinal);
        var applied = new List<Finding>();
        var needsHuman = new List<Finding>();
        var targetsCanonical = false;
        foreach (var finding in transaction.Findings)
        {
            var amountChange = finding.Path.Canonical == AmountPointer && !includeAmount;
            if (finding.Patch is not { } patch || finding.NeedsHuman || amountChange)
            {
                if (finding.Severity >= Severity.Warning || finding.NeedsHuman)
                {
                    needsHuman.Add(finding);
                }

                continue;
            }

            targetsCanonical |= patch.TargetsCanonical;
            foreach (var operation in patch.Operations)
            {
                if (!operations.TryGetValue(operation.Path, out var existing))
                {
                    operations[operation.Path] = operation;
                }
                else if (existing.Value is JsonObject left && operation.Value is JsonObject right)
                {
                    operations[operation.Path] = existing with { Value = Merge(left, right) };
                }
            }

            applied.Add(finding);
        }

        var ordered = operations.Values.OrderBy(o => o.Path, Core.Fields.PointerComparer.Instance).ToList();
        return new FixPlan(transaction.Index, ordered, targetsCanonical, applied, needsHuman);
    }

    private static JsonObject Merge(JsonObject left, JsonObject right)
    {
        var merged = (JsonObject)left.DeepClone();
        foreach (var (key, value) in right)
        {
            merged[key] = merged[key] is JsonObject inner && value is JsonObject other ? Merge(inner, other) : value?.DeepClone();
        }

        return merged;
    }
}
