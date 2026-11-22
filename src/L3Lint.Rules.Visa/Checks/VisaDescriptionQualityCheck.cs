using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Visa.Checks;

/// <summary>
/// VISA-DQ-001: the same detectors as GEN-DQ-001 and GEN-DQ-002, at error severity because public sources say
/// Visa rejects these exact patterns. It supersedes the general findings at the same path (ADR-0032).
/// </summary>
internal sealed class VisaDescriptionQualityCheck : ICustomCheck
{
    public string Name => "VisaDescriptionQuality";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var placeholders = LineScans.PlaceholderDescriptions(context).ToHashSet();
        foreach (var index in placeholders)
        {
            Report(context, rule, sink, index, variant: null);
        }

        foreach (var index in LineScans.MerchantNameDescriptions(context))
        {
            if (!placeholders.Contains(index))
            {
                Report(context, rule, sink, index, "merchantName");
            }
        }
    }

    private static void Report(in RuleContext context, RuleMetadata rule, IFindingSink sink, int index, string? variant)
    {
        var pointer = LineScans.Pointer(index, "description");
        sink.Report(new FindingDraft(rule, pointer)
        {
            Actual = context.Transaction.LineItems[index].Description,
            MessageVariant = variant,
            Arguments = CheckArguments.For(pointer, index + 1),
        });
    }
}
