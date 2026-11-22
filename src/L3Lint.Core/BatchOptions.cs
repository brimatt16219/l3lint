namespace L3Lint.Core;

/// <summary>
/// Thresholds for the batch heuristic GEN-DQ-007 (FR-017).
/// </summary>
public sealed record BatchOptions
{
    /// <summary>Gets the fewest transactions in a run before the heuristic applies. Default 20.</summary>
    public int MinTransactions { get; init; } = 20;

    /// <summary>Gets the share of transactions (0 to 1) that must carry the same value to fire. Default 0.80.</summary>
    public decimal SameValueShare { get; init; } = 0.80m;
}
