using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-DQ-001: a line description that is a placeholder ("Item", "N/A", one character, digits only).</summary>
internal sealed class PlaceholderDescriptionCheck : ICustomCheck
{
    public string Name => "PlaceholderDescription";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var index in LineScans.PlaceholderDescriptions(context))
        {
            var pointer = LineScans.Pointer(index, "description");
            sink.Report(new FindingDraft(rule, pointer)
            {
                Actual = context.Transaction.LineItems[index].Description,
                Arguments = CheckArguments.For(pointer, index + 1),
            });
        }
    }
}
