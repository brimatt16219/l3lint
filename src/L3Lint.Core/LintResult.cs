namespace L3Lint.Core;

/// <summary>
/// The outcome of linting one document or transaction: per-transaction results, all findings, the summary and
/// the versions that produced them.
/// </summary>
/// <remarks>
/// Identical input, options and pack versions give an identical result (NFR-001): findings sort by transaction
/// index, canonical path, then rule ID, and no timestamps are recorded.
/// </remarks>
public sealed record LintResult
{
    /// <summary>Gets the per-transaction results in order.</summary>
    public required IReadOnlyList<TransactionResult> Transactions { get; init; }

    /// <summary>Gets every finding in sort order.</summary>
    public required IReadOnlyList<Finding> Findings { get; init; }

    /// <summary>Gets the counts and the fail decision.</summary>
    public required LintSummary Summary { get; init; }

    /// <summary>Gets the engine version.</summary>
    public required string EngineVersion { get; init; }

    /// <summary>Gets the version of each loaded rule pack, by pack ID.</summary>
    public required IReadOnlyDictionary<string, string> RulePackVersions { get; init; }

    /// <summary>Gets the lowercase hex SHA-256 of the input bytes, or of the canonical JSON for an in-memory transaction.</summary>
    public required string InputSha256 { get; init; }

    /// <summary>Gets the input name.</summary>
    public string? InputName { get; init; }

    /// <summary>Gets the format the input was read as.</summary>
    public string? Format { get; init; }

    /// <summary>Gets inputs or transactions that could not be linted.</summary>
    public IReadOnlyList<InputError> InputErrors { get; init; } = [];

    /// <summary>Gets a value indicating whether <c>--strict-pci</c> stopped processing because a card number was found.</summary>
    public bool StoppedForCardNumber { get; init; }

    /// <summary>Gets a value indicating whether a rule failed internally (exit code 3).</summary>
    public bool HasInternalError => Findings.Any(f => f.RuleId == Engine.EngineDiagnostics.RuleFailedId);
}
