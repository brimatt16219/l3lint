namespace L3Lint.Core;

/// <summary>
/// Counts over a set of findings, plus whether they fail the run.
/// </summary>
/// <param name="Transactions">How many transactions were linted.</param>
/// <param name="Errors">Error findings.</param>
/// <param name="Warnings">Warning findings.</param>
/// <param name="Information">Information findings.</param>
/// <param name="ByRule">Finding counts per rule ID, ordered by rule ID.</param>
/// <param name="Failed">Whether any finding is at or above the fail-on severity.</param>
public sealed record LintSummary(int Transactions, int Errors, int Warnings, int Information, IReadOnlyDictionary<string, int> ByRule, bool Failed)
{
    /// <summary>
    /// Builds a summary.
    /// </summary>
    /// <param name="findings">The findings.</param>
    /// <param name="transactions">How many transactions were linted.</param>
    /// <param name="failOn">The lowest failing severity, or <see langword="null"/> to never fail.</param>
    /// <returns>The summary.</returns>
    public static LintSummary From(IEnumerable<Finding> findings, int transactions, Severity? failOn)
    {
        ArgumentNullException.ThrowIfNull(findings);
        int errors = 0, warnings = 0, information = 0;
        var failed = false;
        var byRule = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var finding in findings)
        {
            switch (finding.Severity)
            {
                case Severity.Error:
                    errors++;
                    break;
                case Severity.Warning:
                    warnings++;
                    break;
                default:
                    information++;
                    break;
            }

            byRule[finding.RuleId] = byRule.GetValueOrDefault(finding.RuleId) + 1;
            failed |= failOn is { } level && finding.Severity >= level;
        }

        return new LintSummary(transactions, errors, warnings, information, byRule, failed);
    }
}
