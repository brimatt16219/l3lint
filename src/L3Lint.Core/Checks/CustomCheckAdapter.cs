using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>Kind <c>custom</c>: delegates to a compiled <see cref="ICustomCheck"/> registered under the pack's name for it.</summary>
internal sealed class CustomCheckAdapter(ICustomCheck check, IReadOnlyList<string> fields) : IRuleCheck
{
    public RuleCheck Description { get; } = new("custom", fields, [new("check", check.Name)]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink) => check.Evaluate(context, rule, sink);
}
