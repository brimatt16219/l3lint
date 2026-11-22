using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>
/// Built-in custom check <c>NoRateProgram</c>: reports once that the brand has no enhanced-data rate program
/// (AMEX-ELIG-001, DISC-ELIG-001).
/// </summary>
internal sealed class NoRateProgramCheck : ICustomCheck
{
    public string Name => "NoRateProgram";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink) =>
        sink.Report(new FindingDraft(rule, "/card/brand"));
}
