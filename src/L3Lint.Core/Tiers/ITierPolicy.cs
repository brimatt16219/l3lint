using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Tiers;

/// <summary>
/// Describes a brand's enhanced-data tiers and which ones a transaction can reach before its data is judged.
/// </summary>
/// <remarks>
/// Each brand pack supplies one. The engine then steps down the ladder past every tier blocked by an error
/// finding (ADR-0020). Eligibility comes from the brand's own lists, so disabling an eligibility rule cannot
/// change the tier.
/// </remarks>
public interface ITierPolicy
{
    /// <summary>Gets the brand.</summary>
    CardBrand Brand { get; }

    /// <summary>
    /// Builds the tier ladder for one transaction.
    /// </summary>
    /// <param name="context">The transaction and its context, including the as-of date.</param>
    /// <returns>The tiers the card and MCC allow, top first.</returns>
    TierLadder GetLadder(in RuleContext context);
}
