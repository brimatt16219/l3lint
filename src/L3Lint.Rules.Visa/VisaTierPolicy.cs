using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Rules.Visa;

/// <summary>
/// Visa's tier ladder (ADR-0020): Product 3 for eligible commercial cards, plus Product 2 while the as-of date
/// is on or before 2026-04-17 (the last day Level 2 applied). Consumer and commercial prepaid cards, MCCs
/// excluded from Product 3, and federal cards at excluded medical MCCs get no enhanced-data tier.
/// </summary>
internal sealed class VisaTierPolicy : ITierPolicy
{
    private static readonly DateOnly LastLevel2Day = new(2026, 4, 17);

    public CardBrand Brand => CardBrand.Visa;

    public TierLadder GetLadder(in RuleContext context)
    {
        var card = context.Transaction.Card;
        var mcc = context.Transaction.Merchant.Mcc;
        if (card.Product is CardProduct.Consumer or CardProduct.CommercialPrepaid)
        {
            return TierLadder.NoProgram;
        }

        if (mcc is not null && context.Lists.Contains("visaProduct3ExcludedMcc", mcc))
        {
            return TierLadder.NoProgram;
        }

        if (card.IsFederalGovernment == true && mcc is not null && context.Lists.Contains("visaFederalExcludedMcc", mcc))
        {
            return TierLadder.NoProgram;
        }

        return context.AsOf <= LastLevel2Day
            ? new TierLadder([new TierRung("product3", RuleLevel.L3), new TierRung("product2", RuleLevel.L2)], "belowProduct2")
            : new TierLadder([new TierRung("product3", RuleLevel.L3)], "belowProduct3");
    }
}
