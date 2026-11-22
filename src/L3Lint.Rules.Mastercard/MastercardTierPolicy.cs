using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Rules.Mastercard;

/// <summary>
/// Mastercard's tier ladder (ADR-0020): Data Rate III for corporate and purchasing cards whose MCC is on neither
/// exclusion list and whose product code is not MBS or MEF; Data Rate II for business, corporate and purchasing
/// cards whose MCC is not on the Level 2 list; Data Rate I below that. An unknown product gets the full ladder.
/// </summary>
internal sealed class MastercardTierPolicy : ITierPolicy
{
    private static readonly TierRung DataRate3 = new("dataRate3", RuleLevel.L3);
    private static readonly TierRung DataRate2 = new("dataRate2", RuleLevel.L2);

    public CardBrand Brand => CardBrand.Mastercard;

    public TierLadder GetLadder(in RuleContext context)
    {
        var card = context.Transaction.Card;
        var mcc = context.Transaction.Merchant.Mcc;
        var product = card.Product ?? CardProduct.Unknown;
        var level2Excluded = mcc is not null && context.Lists.Contains("mastercardLevel2ExcludedMcc", mcc);
        var level3Excluded = level2Excluded || (mcc is not null && context.Lists.Contains("mastercardLevel3ExcludedMcc", mcc));
        var level2Product = product is CardProduct.Unknown or CardProduct.Business or CardProduct.Corporate or CardProduct.Purchasing;
        var level3Product = product is CardProduct.Unknown or CardProduct.Corporate or CardProduct.Purchasing;
        var code = card.ProductCode?.Trim().ToUpperInvariant();
        var level3Code = code is not ("MBS" or "MEF");

        var rungs = new List<TierRung>();
        if (level3Product && level3Code && !level3Excluded)
        {
            rungs.Add(DataRate3);
        }

        if (level2Product && !level2Excluded)
        {
            rungs.Add(DataRate2);
        }

        return rungs.Count == 0 ? TierLadder.NoProgram : new TierLadder(rungs, "dataRate1");
    }
}
