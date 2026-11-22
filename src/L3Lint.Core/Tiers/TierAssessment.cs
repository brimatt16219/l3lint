using L3Lint.Core.Model;

namespace L3Lint.Core.Tiers;

/// <summary>
/// The tier rollup for one brand: the best tier the card allows and the tier the data likely reaches.
/// </summary>
/// <param name="Brand">The brand.</param>
/// <param name="Best">The best tier the card and MCC allow, or "none".</param>
/// <param name="Likely">The tier the payload likely reaches.</param>
/// <param name="Status">How the likely tier compares with the best.</param>
/// <param name="BlockingErrors">How many error findings blocked tiers.</param>
public sealed record TierAssessment(CardBrand Brand, string Best, string Likely, TierStatus Status, int BlockingErrors);
