using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-ARITH-005: a header discount above 0 that equals the sum of line discounts (within the total tolerance)
/// is likely the same discount counted twice.
/// </summary>
internal sealed class DoubleDiscountCheck : ICustomCheck
{
    private const string Pointer = "/amounts/discount";

    public string Name => "DoubleDiscount";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var facts = context.Facts;
        if (context.Transaction.Amounts.Discount is not { } header || header <= 0m || facts.SumLineDiscount <= 0m)
        {
            return;
        }

        if (Math.Abs(header - facts.SumLineDiscount) > facts.TotalTolerance)
        {
            return;
        }

        var arguments = CheckArguments.For(Pointer, null);
        arguments["reported"] = CheckArguments.Number(header);
        sink.Report(new FindingDraft(rule, Pointer) { Actual = CheckArguments.Number(header), Arguments = arguments });
    }
}
