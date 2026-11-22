using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-DQ-003: a product code or unit of measure that is zeros, "na", "none", "sku" or the line number.</summary>
internal sealed class PlaceholderCodeCheck : ICustomCheck
{
    public string Name => "PlaceholderCode";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var (index, field, value) in LineScans.PlaceholderCodes(context, includeCommodity: false))
        {
            var pointer = LineScans.Pointer(index, field);
            sink.Report(new FindingDraft(rule, pointer) { Actual = value, Arguments = CheckArguments.For(pointer, index + 1) });
        }
    }
}
