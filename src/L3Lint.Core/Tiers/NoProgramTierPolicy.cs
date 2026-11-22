using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Tiers;

/// <summary>
/// A tier policy for brands with no enhanced-data rate program (American Express, Discover): the ladder is always
/// empty, so the rollup reports <see cref="TierStatus.NoBenefit"/>.
/// </summary>
/// <param name="brand">The brand.</param>
public sealed class NoProgramTierPolicy(CardBrand brand) : ITierPolicy
{
    /// <inheritdoc/>
    public CardBrand Brand => brand;

    /// <inheritdoc/>
    public TierLadder GetLadder(in RuleContext context) => TierLadder.NoProgram;
}
