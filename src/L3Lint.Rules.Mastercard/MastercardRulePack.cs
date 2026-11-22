using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;
using L3Lint.Rules.Mastercard.Checks;

namespace L3Lint.Rules.Mastercard;

/// <summary>
/// The Mastercard rule pack (<c>l3lint.rules.mastercard</c>): 17 rules for Data Rate II and III, with the
/// checks they name and Mastercard's tier ladder.
/// </summary>
public sealed class MastercardRulePack : EmbeddedRulePackSource
{
    /// <summary>Initializes a new instance.</summary>
    public MastercardRulePack()
        : base(typeof(MastercardRulePack).Assembly, "mastercard")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ICustomCheck> CustomChecks { get; } = [new MastercardLineIndicatorsCheck(), new AmountDigitsCheck()];

    /// <inheritdoc/>
    public override ITierPolicy TierPolicy { get; } = new MastercardTierPolicy();
}
