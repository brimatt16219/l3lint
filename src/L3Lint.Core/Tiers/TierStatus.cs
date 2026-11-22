namespace L3Lint.Core.Tiers;

/// <summary>
/// How the likely tier compares with the best one.
/// </summary>
public enum TierStatus
{
    /// <summary>The payload meets the documented requirement for the best tier. It is not a guarantee (D5).</summary>
    Meets,

    /// <summary>The best tier is reachable on paper, but findings suggest Visa's validation may fail it.</summary>
    AtRisk,

    /// <summary>Errors block the best tier; the likely tier is lower.</summary>
    Blocked,

    /// <summary>No enhanced-data rate exists for this card, brand or MCC.</summary>
    NoBenefit,
}
