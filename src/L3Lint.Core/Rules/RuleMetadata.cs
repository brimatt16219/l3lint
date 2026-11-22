using L3Lint.Core.Model;

namespace L3Lint.Core.Rules;

/// <summary>
/// Everything known about a rule: identity, scope, severity, provenance, wording and examples (FR-012).
/// </summary>
/// <remarks>
/// Metadata comes from rule pack YAML for pack rules, or from code for custom <see cref="IRule"/>
/// implementations. It is immutable and shared by every evaluation.
/// </remarks>
public sealed record RuleMetadata
{
    /// <summary>Gets the rule ID, for example VISA-L3-004.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the short title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the requirement in one sentence. Defaults to the title.</summary>
    public string? Requirement { get; init; }

    /// <summary>Gets why the rule exists.</summary>
    public string? Rationale { get; init; }

    /// <summary>Gets notes, such as disagreements between sources (risk R2).</summary>
    public string? Notes { get; init; }

    /// <summary>Gets the ID of the pack that defines the rule, for example l3lint.rules.visa.</summary>
    public required string PackId { get; init; }

    /// <summary>Gets the version of that pack.</summary>
    public required string PackVersion { get; init; }

    /// <summary>Gets the brand the rule belongs to, or <see langword="null"/> for rules that apply to all brands.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>Gets the enhanced-data level.</summary>
    public required RuleLevel Level { get; init; }

    /// <summary>Gets the default severity from the catalog.</summary>
    public required Severity Severity { get; init; }

    /// <summary>Gets the confidence label.</summary>
    public required Confidence Confidence { get; init; }

    /// <summary>Gets the impact class.</summary>
    public required Impact Impact { get; init; }

    /// <summary>Gets the first date the rule applies, or <see langword="null"/> for always.</summary>
    public DateOnly? EffectiveFrom { get; init; }

    /// <summary>Gets the last date the rule applies (inclusive), or <see langword="null"/> for no end.</summary>
    public DateOnly? EffectiveUntil { get; init; }

    /// <summary>Gets the applicability gates.</summary>
    public RuleApplicability AppliesTo { get; init; } = RuleApplicability.Always;

    /// <summary>Gets the processor profiles under which the catalog severity applies; other profiles cap it at warning (ADR-0016).</summary>
    public IReadOnlyList<string> Profiles { get; init; } = [];

    /// <summary>Gets rules whose findings at the same path this rule replaces (ADR-0032).</summary>
    public IReadOnlyList<string> Supersedes { get; init; } = [];

    /// <summary>Gets a readable description of the check.</summary>
    public required RuleCheck Check { get; init; }

    /// <summary>Gets the message template, with placeholders such as {index} and {actual}.</summary>
    public required string Message { get; init; }

    /// <summary>Gets named alternative message templates used by custom checks.</summary>
    public IReadOnlyDictionary<string, string> Messages { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the fix, stated as an action.</summary>
    public required string Fix { get; init; }

    /// <summary>Gets the public sources (D1).</summary>
    public required IReadOnlyList<Uri> Sources { get; init; }

    /// <summary>Gets a value indicating whether the rule is decided over a whole run (a batch rule such as GEN-DQ-007).</summary>
    public bool IsBatch { get; init; }

    /// <summary>Gets the pass and fail examples, when the pack ships them.</summary>
    public RuleExamples? Examples { get; init; }

    /// <summary>
    /// Checks whether the rule is in force on a date.
    /// </summary>
    /// <param name="asOf">The as-of date.</param>
    /// <returns><see langword="true"/> when the date is within the effective range, ends included.</returns>
    public bool IsEffective(DateOnly asOf) =>
        (EffectiveFrom is null || asOf >= EffectiveFrom) && (EffectiveUntil is null || asOf <= EffectiveUntil);
}
