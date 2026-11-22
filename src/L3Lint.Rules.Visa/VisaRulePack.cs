using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;
using L3Lint.Rules.Visa.Checks;

namespace L3Lint.Rules.Visa;

/// <summary>
/// The Visa rule pack (<c>l3lint.rules.visa</c>): 26 rules for the Commercial Enhanced Data Program, with the
/// checks they name and Visa's tier ladder (Product 3, and Product 2 until the April 2026 sunset).
/// </summary>
public sealed class VisaRulePack : EmbeddedRulePackSource
{
    /// <summary>Initializes a new instance.</summary>
    public VisaRulePack()
        : base(typeof(VisaRulePack).Assembly, "visa")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ICustomCheck> CustomChecks { get; } =
    [
        new ExplicitTaxCheck(),
        new VisaDescriptionQualityCheck(),
        new VisaCodeQualityCheck(),
        new LargeTicketCheck(),
    ];

    /// <inheritdoc/>
    public override ITierPolicy TierPolicy { get; } = new VisaTierPolicy();
}
