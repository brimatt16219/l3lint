using L3Lint.Core.Rules;

namespace L3Lint.Core.Tiers;

/// <summary>
/// One enhanced-data tier.
/// </summary>
/// <param name="Name">The tier name used in output, for example product3 or dataRate2.</param>
/// <param name="Level">The data level the tier needs; errors with impact blocksLevel3 remove L3 tiers, blocksLevel2 remove both.</param>
public sealed record TierRung(string Name, RuleLevel Level);
