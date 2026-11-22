using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Core.Engine;

/// <summary>
/// The tier rollup (clarification 10, ADR-0020): start at the brand's top tier, step down past every tier
/// blocked by an error of that brand or of the general pack; a Visa validation risk marks Product 3 at risk.
/// </summary>
internal static class TierRollup
{
    public static TierAssessment Assess(ITierPolicy policy, in RuleContext context, IReadOnlyList<Finding> findings)
    {
        var brand = policy.Brand;
        var ladder = policy.GetLadder(context);
        if (ladder.Rungs.Count == 0)
        {
            return new TierAssessment(brand, "none", "none", TierStatus.NoBenefit, 0);
        }

        var relevant = findings.Where(f => f.Brand is null || f.Brand == brand).ToList();
        var level3Errors = relevant.Count(f => f.Severity == Severity.Error && f.Impact == Impact.BlocksLevel3);
        var level2Errors = relevant.Count(f => f.Severity == Severity.Error && f.Impact == Impact.BlocksLevel2);
        var reachable = ladder.Rungs.Where(r => r.Level switch
        {
            RuleLevel.L3 => level3Errors == 0 && level2Errors == 0,
            _ => level2Errors == 0,
        }).ToList();

        var best = ladder.Rungs[0].Name;
        var likely = reachable.Count > 0 ? reachable[0].Name : ladder.Base;
        if (likely != best)
        {
            var blocking = ladder.Rungs[0].Level == RuleLevel.L3 ? level3Errors + level2Errors : level2Errors;
            return new TierAssessment(brand, best, likely, TierStatus.Blocked, blocking);
        }

        var atRisk = brand == CardBrand.Visa
            && relevant.Any(f => f.Severity >= Severity.Warning && f.Impact == Impact.RiskVisaValidation);
        return new TierAssessment(brand, best, likely, atRisk ? TierStatus.AtRisk : TierStatus.Meets, 0);
    }
}
