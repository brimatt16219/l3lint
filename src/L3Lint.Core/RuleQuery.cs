using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core;

/// <summary>
/// Filters for <see cref="ILinter.Rules"/>: brand, level and free text.
/// </summary>
public sealed record RuleQuery
{
    /// <summary>Gets the brand to show; <see cref="CardBrand.Unknown"/> selects the general rules; <see langword="null"/> shows all.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>Gets the level to show; <see langword="null"/> shows all.</summary>
    public RuleLevel? Level { get; init; }

    /// <summary>Gets text that must appear in the ID, title or requirement (case-insensitive).</summary>
    public string? Text { get; init; }

    /// <summary>
    /// Tests a rule against the query.
    /// </summary>
    /// <param name="rule">The rule metadata.</param>
    /// <returns><see langword="true"/> when the rule matches every filter.</returns>
    public bool Matches(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (Brand is { } brand && (brand == CardBrand.Unknown ? rule.Brand is not null : rule.Brand != brand))
        {
            return false;
        }

        if (Level is { } level && rule.Level != level)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(Text)
            || rule.Id.Contains(Text, StringComparison.OrdinalIgnoreCase)
            || rule.Title.Contains(Text, StringComparison.OrdinalIgnoreCase)
            || (rule.Requirement?.Contains(Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
