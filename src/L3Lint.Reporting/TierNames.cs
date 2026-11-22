using System.Globalization;
using L3Lint.Core.Tiers;

namespace L3Lint.Reporting;

/// <summary>Display names for tiers and statuses, in advisory wording (D5).</summary>
public static class TierNames
{
    /// <summary>Gets the display name of a tier, for example "Product 3" for product3.</summary>
    /// <param name="tier">The tier name from <see cref="TierAssessment"/>.</param>
    /// <returns>The display name.</returns>
    public static string Display(string tier) => tier switch
    {
        "product3" => Resources.Strings.TierProduct3,
        "product2" => Resources.Strings.TierProduct2,
        "belowProduct3" => Resources.Strings.TierBelowProduct3,
        "belowProduct2" => Resources.Strings.TierBelowProduct2,
        "dataRate3" => Resources.Strings.TierDataRate3,
        "dataRate2" => Resources.Strings.TierDataRate2,
        "dataRate1" => Resources.Strings.TierDataRate1,
        "none" => Resources.Strings.TierNone,
        _ => tier,
    };

    /// <summary>
    /// Describes an assessment, for example "below Product 3 (blocked by 1 error)".
    /// </summary>
    /// <param name="tier">The assessment.</param>
    /// <returns>The description.</returns>
    public static string Describe(TierAssessment tier)
    {
        ArgumentNullException.ThrowIfNull(tier);
        return tier.Status switch
        {
            TierStatus.Meets => string.Format(CultureInfo.InvariantCulture, Resources.Strings.TierMeets, Display(tier.Likely)),
            TierStatus.AtRisk => string.Format(CultureInfo.InvariantCulture, Resources.Strings.TierAtRisk, Display(tier.Likely)),
            TierStatus.Blocked => string.Format(CultureInfo.InvariantCulture, tier.BlockingErrors == 1 ? Resources.Strings.TierBlockedOne : Resources.Strings.TierBlockedMany, Display(tier.Likely), tier.BlockingErrors),
            _ => Resources.Strings.TierNoBenefit,
        };
    }
}
