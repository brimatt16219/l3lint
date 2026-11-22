using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-DQ-004: a customer code or PO number that is filler such as 123456, "po" or "none".</summary>
internal sealed class FillerReferenceCheck : ICustomCheck
{
    public string Name => "FillerReference";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        Check(rule, sink, "/customer/reference", context.Transaction.Customer.Reference);
        Check(rule, sink, "/order/purchaseOrderNumber", context.Transaction.Order.PurchaseOrderNumber);
    }

    private static void Check(RuleMetadata rule, IFindingSink sink, string pointer, string? value)
    {
        if (value is not null && DataQuality.IsFillerReference(value))
        {
            sink.Report(new FindingDraft(rule, pointer) { Actual = value, Arguments = CheckArguments.For(pointer, null) });
        }
    }
}
