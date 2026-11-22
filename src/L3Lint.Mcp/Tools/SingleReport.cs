using L3Lint.Core;
using L3Lint.Reporting;

namespace L3Lint.Mcp.Tools;

/// <summary>
/// Wraps the result of one tool call in a <see cref="LintReport"/>, so <c>lint_payload</c> returns the same JSON
/// document as <c>l3lint lint --output json</c>, validated by <c>results.schema.json</c>.
/// </summary>
internal static class SingleReport
{
    public static LintReport From(LintResult result, LintOptions options, int? maxFindings, IReadOnlyDictionary<string, string> packs)
    {
        var all = result.Transactions.SelectMany(t => t.Findings).ToList();
        var summary = LintSummary.From(all, result.Transactions.Count, options.FailOn);
        var transactions = result.Transactions;
        var omitted = 0;
        if (maxFindings is { } max && all.Count > max)
        {
            var left = Math.Max(0, max);
            var kept = new List<TransactionResult>(transactions.Count);
            foreach (var transaction in transactions)
            {
                var take = Math.Min(left, transaction.Findings.Count);
                kept.Add(transaction with { Findings = [.. transaction.Findings.Take(take)] });
                left -= take;
            }

            omitted = all.Count - Math.Max(0, max);
            transactions = kept;
        }

        var exitCode = result.HasInternalError ? 3 : result.InputErrors.Count > 0 ? 2 : summary.Failed ? 1 : 0;
        return new LintReport
        {
            ToolVersion = EngineInfo.Version,
            RulePacks = new SortedDictionary<string, string>(packs.ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal),
            AsOf = options.AsOf,
            Profile = options.ProcessorProfile?.Id ?? "generic",
            Inputs = [new ReportInput("payload", result.Format ?? "unknown", result.InputSha256, result.Transactions.Count)],
            Transactions = transactions,
            InputErrors = result.InputErrors,
            Summary = summary,
            FindingsOmitted = omitted,
            FailOn = options.FailOn,
            ExitCode = exitCode,
        };
    }
}
