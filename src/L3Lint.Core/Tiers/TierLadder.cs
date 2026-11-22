namespace L3Lint.Core.Tiers;

/// <summary>
/// The tiers a transaction can reach for one brand, top first.
/// </summary>
/// <param name="Rungs">Reachable tiers, best first; empty when no enhanced-data rate exists.</param>
/// <param name="Base">The tier name when every rung is blocked, for example belowProduct3.</param>
public sealed record TierLadder(IReadOnlyList<TierRung> Rungs, string Base)
{
    /// <summary>Gets a ladder with no enhanced-data tier.</summary>
    public static TierLadder NoProgram { get; } = new([], "none");
}
