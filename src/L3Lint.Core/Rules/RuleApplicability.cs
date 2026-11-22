using L3Lint.Core.Model;

namespace L3Lint.Core.Rules;

/// <summary>
/// When a rule applies: card products, MCC lists and field conditions.
/// </summary>
/// <remarks>
/// An unknown card product or MCC does not skip a rule, so data problems still show when context is missing
/// (ADR-0032). <see cref="When"/> conditions must all hold; the rule is skipped when every
/// <see cref="Unless"/> condition holds.
/// </remarks>
public sealed record RuleApplicability
{
    /// <summary>Gets an applicability that always holds.</summary>
    public static RuleApplicability Always { get; } = new();

    /// <summary>Gets the card products the rule applies to; empty means all.</summary>
    public IReadOnlyList<CardProduct> CardProducts { get; init; } = [];

    /// <summary>Gets list names; the rule runs only when the MCC is on one of them.</summary>
    public IReadOnlyList<string> MccIn { get; init; } = [];

    /// <summary>Gets list names; the rule is skipped when the MCC is on any of them.</summary>
    public IReadOnlyList<string> MccNotIn { get; init; } = [];

    /// <summary>Gets conditions that must all hold.</summary>
    public IReadOnlyList<RuleCondition> When { get; init; } = [];

    /// <summary>Gets conditions that skip the rule when all hold.</summary>
    public IReadOnlyList<RuleCondition> Unless { get; init; } = [];
}
