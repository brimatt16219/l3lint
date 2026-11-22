using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Rules.Discover;

/// <summary>
/// The Discover rule pack (<c>l3lint.rules.discover</c>): 3 format and limit rules and a tier policy that always
/// reports no rate program.
/// </summary>
public sealed class DiscoverRulePack : EmbeddedRulePackSource
{
    /// <summary>Initializes a new instance.</summary>
    public DiscoverRulePack()
        : base(typeof(DiscoverRulePack).Assembly, "discover")
    {
    }

    /// <inheritdoc/>
    public override ITierPolicy TierPolicy { get; } = new NoProgramTierPolicy(Core.Model.CardBrand.Discover);
}
