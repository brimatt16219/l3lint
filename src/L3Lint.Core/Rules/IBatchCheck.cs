namespace L3Lint.Core.Rules;

/// <summary>
/// Creates a fresh <see cref="IBatchRule"/> for each run, for a pack rule that names it with <c>kind: custom</c>.
/// </summary>
public interface IBatchCheck
{
    /// <summary>Gets the name a pack uses to refer to this check.</summary>
    string Name { get; }

    /// <summary>
    /// Creates the per-run rule instance.
    /// </summary>
    /// <param name="rule">The metadata of the rule that names this check.</param>
    /// <param name="options">The run's options, including batch thresholds.</param>
    /// <returns>A new batch rule.</returns>
    IBatchRule Create(RuleMetadata rule, LintOptions options);
}
