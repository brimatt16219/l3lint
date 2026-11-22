using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Reporting;

/// <summary>
/// Everything one run produced, ready for the output writers: per-transaction results in run order, batch
/// findings, input errors, the summary and the exit code.
/// </summary>
/// <remarks>
/// A report is immutable and contains no timestamps, so identical input, options and pack versions give an
/// identical report and identical JSON and SARIF (NFR-001). When <see cref="FindingsOmitted"/> is above 0 the
/// transactions hold only the findings that fit <c>--max-findings</c>; the summary still counts all of them.
/// </remarks>
public sealed record LintReport
{
    /// <summary>Gets the engine version.</summary>
    public required string ToolVersion { get; init; }

    /// <summary>Gets the loaded rule pack versions by pack ID, ordered by ID.</summary>
    public required IReadOnlyDictionary<string, string> RulePacks { get; init; }

    /// <summary>Gets the <c>--as-of</c> date, or <see langword="null"/> when each transaction used its own date.</summary>
    public DateOnly? AsOf { get; init; }

    /// <summary>Gets the processor profile used.</summary>
    public string Profile { get; init; } = "generic";

    /// <summary>Gets the inputs read, in order.</summary>
    public IReadOnlyList<ReportInput> Inputs { get; init; } = [];

    /// <summary>Gets the per-transaction results, in run order.</summary>
    public IReadOnlyList<TransactionResult> Transactions { get; init; } = [];

    /// <summary>Gets batch findings (GEN-DQ-007), each pointing at the first transaction that carries the value.</summary>
    public IReadOnlyList<Finding> BatchFindings { get; init; } = [];

    /// <summary>Gets inputs or transactions that could not be linted (exit code 2).</summary>
    public IReadOnlyList<InputError> InputErrors { get; init; } = [];

    /// <summary>Gets run-level notes, such as a warning that payload values may appear in logs.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Gets the counts over every finding, including omitted ones.</summary>
    public required LintSummary Summary { get; init; }

    /// <summary>Gets how many findings were left out of the output by <c>--max-findings</c>.</summary>
    public int FindingsOmitted { get; init; }

    /// <summary>Gets the lowest failing severity, or <see langword="null"/> when the run never fails on findings.</summary>
    public Severity? FailOn { get; init; } = Severity.Error;

    /// <summary>Gets the process exit code: 0 clean, 1 findings at or above fail-on, 2 input error, 3 pack, config or internal error.</summary>
    public required int ExitCode { get; init; }

    /// <summary>Gets a short hash of the effective configuration, used as the SARIF automation category.</summary>
    public string ConfigHash { get; init; } = string.Empty;

    /// <summary>
    /// Gets the metadata of the rules that produced findings, ordered by ID. SARIF rule descriptors and Markdown
    /// rule links read titles and requirements from here; a rule missing from this list is described by its ID only.
    /// </summary>
    public IReadOnlyList<RuleMetadata> Rules { get; init; } = [];

    /// <summary>Gets every finding shown in the output: transaction findings in run order, then batch findings.</summary>
    /// <returns>The findings.</returns>
    public IEnumerable<Finding> AllFindings() => Transactions.SelectMany(t => t.Findings).Concat(BatchFindings);
}
