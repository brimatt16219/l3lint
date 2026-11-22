using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-DQ-002: a line description equal or nearly equal (Jaro-Winkler at least 0.92) to the merchant name.</summary>
internal sealed class MerchantNameDescriptionCheck : ICustomCheck
{
    public string Name => "MerchantNameDescription";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var index in LineScans.MerchantNameDescriptions(context))
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
