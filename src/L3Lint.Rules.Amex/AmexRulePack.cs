using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;
using L3Lint.Rules.Amex.Checks;

namespace L3Lint.Rules.Amex;

/// <summary>
/// The American Express rule pack (<c>l3lint.rules.amex</c>): 9 format and completeness rules, the ANSI X12 unit
/// list, and a tier policy that always reports no public rate program.
/// </summary>
public sealed class AmexRulePack : EmbeddedRulePackSource
{
    /// <summary>Initializes a new instance.</summary>
    public AmexRulePack()
        : base(typeof(AmexRulePack).Assembly, "amex")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ICustomCheck> CustomChecks { get; } = [new AdviceTextCheck()];

    /// <inheritdoc/>
    public override ITierPolicy TierPolicy { get; } = new NoProgramTierPolicy(Core.Model.CardBrand.Amex);
}
