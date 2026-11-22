using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>A compiled check: one of the declarative kinds or a custom check.</summary>
internal interface IRuleCheck
{
    /// <summary>Gets a readable description for explain output and docs.</summary>
    RuleCheck Description { get; }

    /// <summary>Gets the list names the check reads.</summary>
    IReadOnlyList<string> ListNames { get; }

    void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink);
}
