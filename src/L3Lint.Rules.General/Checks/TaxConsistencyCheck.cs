using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-ARITH-003 (formula 3): when any line carries tax, |T - sum t_i| must be within the total tolerance.
/// No patch: l3lint never decides a tax amount (NG1). Skipped when a line tax amount is invalid, so one bad value
/// gives one finding (ADR-0036).
/// </summary>
internal sealed class TaxConsistencyCheck : ICustomCheck
{
    private const string Pointer = "/tax/amount";

    public string Name => "TaxConsistency";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var facts = context.Facts;
        if (!facts.AnyLineTax || context.Transaction.Tax.Amount is not { } header || facts.Currency is null || AnyLineTaxInvalid(context))
        {
            return;
        }

        if (Math.Abs(header - facts.SumLineTax) <= facts.TotalTolerance)
        {
            return;
        }

        var arguments = CheckArguments.For(Pointer, null);
        arguments["reported"] = CheckArguments.Number(header);
        arguments["expected"] = CheckArguments.Number(facts.SumLineTax);
        sink.Report(new FindingDraft(rule, Pointer)
        {
            Actual = CheckArguments.Number(header),
            Expected = CheckArguments.Number(facts.SumLineTax),
            Arguments = arguments,
        });
    }

    private static bool AnyLineTaxInvalid(in RuleContext context)
    {
        for (var i = 0; i < context.Transaction.LineItems.Count; i++)
        {
            if (context.IsInvalid("/lineItems/" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/taxAmount"))
            {
                return true;
            }
        }

        return false;
    }
}
