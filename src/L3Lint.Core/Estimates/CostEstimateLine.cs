using L3Lint.Core.Model;

namespace L3Lint.Core.Estimates;

/// <summary>
/// The estimate for one brand: cost at the likely tier, cost at the best tier and the difference.
/// </summary>
/// <param name="Brand">The brand.</param>
/// <param name="LikelyTier">The rate table tier used for the likely outcome.</param>
/// <param name="LikelyCost">Estimated cost at the likely tier, or <see langword="null"/> when the table has no matching category.</param>
/// <param name="BestTier">The rate table tier used for the best outcome.</param>
/// <param name="BestCost">Estimated cost at the best tier, or <see langword="null"/> when the table has no matching category.</param>
/// <param name="Difference">Likely minus best, when both exist: what the downgrade likely costs.</param>
public sealed record CostEstimateLine(CardBrand Brand, string LikelyTier, decimal? LikelyCost, string BestTier, decimal? BestCost, decimal? Difference);
