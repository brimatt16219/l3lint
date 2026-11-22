using L3Lint.Core.Estimates;
using L3Lint.Core.Model;
using L3Lint.Core.Tiers;

namespace L3Lint.Core;

/// <summary>
/// The outcome for one transaction: findings sorted by path then rule ID, tier rollup, estimate and notes.
/// </summary>
/// <remarks>Immutable. The <see cref="Transaction"/> is the effective, masked transaction the rules saw.</remarks>
public sealed record TransactionResult
{
    /// <summary>Gets whether this is a transaction or the trailing batch result of a run (ADR-0034).</summary>
    public TransactionResultKind Kind { get; init; } = TransactionResultKind.Transaction;

    /// <summary>Gets the 0-based transaction index in the run.</summary>
    public required int Index { get; init; }

    /// <summary>Gets the 0-based index of the input file or document.</summary>
    public int Input { get; init; }

    /// <summary>Gets the input name: a file path, "-" for stdin, or a label.</summary>
    public string? InputName { get; init; }

    /// <summary>Gets the transaction ID from the payload.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the adapter that read the transaction, for example "stripe".</summary>
    public string? Format { get; init; }

    /// <summary>Gets the effective card brand.</summary>
    public CardBrand Brand { get; init; }

    /// <summary>Gets every brand evaluated.</summary>
    public IReadOnlyList<CardBrand> Brands { get; init; } = [];

    /// <summary>Gets the effective card product.</summary>
    public CardProduct? CardProduct { get; init; }

    /// <summary>Gets the effective MCC.</summary>
    public string? Mcc { get; init; }

    /// <summary>Gets the date whose rules applied.</summary>
    public DateOnly AsOf { get; init; }

    /// <summary>Gets the 1-based line in the input file where the transaction starts.</summary>
    public int FirstLine { get; init; } = 1;

    /// <summary>Gets the findings, sorted by canonical path then rule ID (NFR-001).</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];

    /// <summary>Gets the tier rollup per evaluated brand.</summary>
    public IReadOnlyList<TierAssessment> Tiers { get; init; } = [];

    /// <summary>Gets the cost estimate, when a rate table was supplied.</summary>
    public CostEstimate? CostEstimate { get; init; }

    /// <summary>Gets notes such as context overrides and adapter remarks.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Gets the effective, masked transaction.</summary>
    public Transaction? Transaction { get; init; }

    /// <summary>Gets the tier for the transaction's own brand, or the first evaluated brand.</summary>
    public TierAssessment? PrimaryTier => Tiers.FirstOrDefault(t => t.Brand == Brand) ?? (Tiers.Count > 0 ? Tiers[0] : null);
}
