using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Reporting.Rules;

/// <summary>Plain descriptions of rule metadata shared by explain output, rule pages and the MCP server.</summary>
public static class RuleText
{
    /// <summary>Gets the brand name, or "all brands" for general rules.</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The text.</returns>
    public static string Brand(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return rule.Brand is { } brand ? CanonicalNames.ToName(brand) : Resources.Strings.RuleAllBrands;
    }

    /// <summary>Describes the effective dates, for example "from 2026-04-17".</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The text.</returns>
    public static string Effective(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return (rule.EffectiveFrom, rule.EffectiveUntil) switch
        {
            (null, null) => Resources.Strings.RuleEffectiveAlways,
            ({ } from, null) => string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleEffectiveFrom, Date(from)),
            (null, { } until) => string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleEffectiveUntil, Date(until)),
            ({ } from, { } until) => string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleEffectiveRange, Date(from), Date(until)),
        };
    }

    /// <summary>Describes when the rule applies, one line per condition.</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The lines.</returns>
    public static IEnumerable<string> AppliesTo(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return AppliesToLines(rule);
    }

    /// <summary>Describes the check kind, fields and parameters.</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The text.</returns>
    public static string Check(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var check = rule.Check;
        var fields = check.Fields.Count > 0 ? " " + string.Join(", ", check.Fields) : string.Empty;
        var parameters = check.Parameters.Count > 0 ? " (" + string.Join("; ", check.Parameters.Select(p => p.Key + ": " + p.Value)) + ")" : string.Empty;
        return check.Kind + fields + parameters;
    }

    /// <summary>Formats a date as YYYY-MM-DD.</summary>
    /// <param name="date">The date.</param>
    /// <returns>The text.</returns>
    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static IEnumerable<string> AppliesToLines(RuleMetadata rule)
    {
        var applies = rule.AppliesTo;
        if (applies.CardProducts.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesProducts, string.Join(", ", applies.CardProducts.Select(CanonicalNames.ToName)));
        }

        if (applies.MccIn.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesMccIn, string.Join(", ", applies.MccIn));
        }

        if (applies.MccNotIn.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesMccNotIn, string.Join(", ", applies.MccNotIn));
        }

        foreach (var condition in applies.When)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesWhen, Condition(condition));
        }

        if (applies.Unless.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesUnless, string.Join(" and ", applies.Unless.Select(Condition)));
        }

        if (rule.Profiles.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleAppliesProfiles, WireNames.Of(rule.Severity), string.Join(", ", rule.Profiles));
        }
    }

    private static string Condition(RuleCondition condition) =>
        condition.Field + " " + condition.Operator + " " + string.Join(", ", condition.Operands);
}
