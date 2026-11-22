using L3Lint.Core.Model;
using L3Lint.Core.Tiers;

namespace L3Lint.Core.Estimates;

/// <summary>
/// Computes cost estimates from a rate table and the tier rollup (FR-028). A tier below the brand's ladder
/// maps to the card-present or card-not-present category, as the spec's example shows.
/// </summary>
internal static class CostEstimator
{
    public static CostEstimate? Estimate(Transaction transaction, IReadOnlyList<TierAssessment> tiers, RateTable rates)
    {
        if (transaction.Amount is not { } amount || !string.Equals(transaction.Currency, rates.Currency, StringComparison.Ordinal)
            || !Iso4217.TryGetCurrency(rates.Currency, out var currency))
        {
            return null;
        }

        var lines = new List<CostEstimateLine>();
        foreach (var tier in tiers)
        {
            if (tier.Status == TierStatus.NoBenefit)
            {
                continue;
            }

            var bestTier = tier.Best;
            var likelyTier = CategoryTier(tier.Likely, transaction.Channel, rates, tier.Brand, transaction.Card.Product);
            var best = Cost(rates, tier.Brand, transaction, bestTier, amount, currency);
            var likely = Cost(rates, tier.Brand, transaction, likelyTier, amount, currency);
            if (best is null && likely is null)
            {
                continue;
            }

            lines.Add(new CostEstimateLine(tier.Brand, likelyTier, likely, bestTier, best, likely - best));
        }

        return lines.Count == 0 ? null : new CostEstimate(rates.Name, rates.Effective, rates.Currency, lines);
    }

    /// <summary>Finds the category for a brand, product and tier; categories without products match any product.</summary>
    public static RateCategory? Find(RateTable rates, CardBrand brand, CardProduct? product, string tier) =>
        rates.Categories.FirstOrDefault(c => c.Brand == brand && c.Tier == tier && product is { } p && c.Products.Contains(p))
        ?? rates.Categories.FirstOrDefault(c => c.Brand == brand && c.Tier == tier && c.Products.Count == 0);

    /// <summary>Cost of one category including applicable program fees, rounded to the currency.</summary>
    public static decimal? Cost(RateTable rates, CardBrand brand, Transaction transaction, string tier, decimal amount, Currency currency)
    {
        if (Find(rates, brand, transaction.Card.Product, tier) is not { } category)
        {
            return null;
        }

        var enhanced = transaction.LineItems.Count > 0 || transaction.Tax.Amount is not null || transaction.Customer.Reference is not null;
        var cost = (amount * category.Percent / 100m) + category.Fixed;
        foreach (var fee in rates.ProgramFees.Where(f => f.Brand == brand && (f.When == "always" || enhanced)))
        {
            cost += (amount * fee.Percent / 100m) + fee.Fixed;
        }

        return Math.Round(cost, currency.Exponent, MidpointRounding.AwayFromZero);
    }

    private static string CategoryTier(string likely, Channel? channel, RateTable rates, CardBrand brand, CardProduct? product)
    {
        if (!likely.StartsWith("below", StringComparison.Ordinal) && Find(rates, brand, product, likely) is not null)
        {
            return likely;
        }

        return channel == Channel.CardPresent ? "cardPresent" : "cardNotPresent";
    }
}
