namespace L3Lint.Core;

/// <summary>
/// The result of running one rule's examples.
/// </summary>
/// <param name="RuleId">The rule.</param>
/// <param name="PassExampleOk">Whether the passing example did not raise the rule.</param>
/// <param name="FailExampleOk">Whether the failing example raised the rule.</param>
/// <param name="Detail">Why the rule failed its examples, or <see langword="null"/> when it passed.</param>
public sealed record RuleExampleOutcome(string RuleId, bool PassExampleOk, bool FailExampleOk, string? Detail)
{
    /// <summary>Gets a value indicating whether both examples behaved as expected.</summary>
    public bool Passed => PassExampleOk && FailExampleOk;
}
