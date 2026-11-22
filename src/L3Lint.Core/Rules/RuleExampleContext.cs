namespace L3Lint.Core.Rules;

/// <summary>
/// Context needed to run a rule's examples.
/// </summary>
/// <param name="AsOf">The as-of date, for rules with an end date.</param>
/// <param name="Processor">The processor profile, for profile-scoped rules.</param>
/// <param name="StrictProfiles">Whether to run single-processor rules at catalog severity.</param>
/// <param name="RatesYaml">A rate table, for rules that need one (VISA-LT-001).</param>
public sealed record RuleExampleContext(DateOnly? AsOf = null, string? Processor = null, bool StrictProfiles = false, string? RatesYaml = null)
{
    /// <summary>Gets an empty context.</summary>
    public static RuleExampleContext None { get; } = new();
}
