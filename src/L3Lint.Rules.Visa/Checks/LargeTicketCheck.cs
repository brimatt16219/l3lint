using L3Lint.Core.Checks;
using L3Lint.Core.Estimates;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Visa.Checks;

/// <summary>
/// VISA-LT-001 (clarification 8, ADR-0018): with a rate table that has Visa <c>product3</c> and
/// <c>largeTicket</c> categories for the card product, the break-even amount is where both cost the same:
/// a x p3 + f3 = a x lt + fLT, so a = (fLT - f3) / (p3 - lt). Program fees apply to both and cancel out.
/// Reports when the amount is at or above the break-even. Without such a table the rule never fires.
/// </summary>
internal sealed class LargeTicketCheck : ICustomCheck
{
    private const string Pointer = "/amount";

    public string Name => "LargeTicket";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        if (context.Options.Rates is not { } rates || context.Transaction.Amount is not { } amount)
        {
            return;
        }

        var product = context.Transaction.Card.Product;
        var product3 = CostEstimator.Find(rates, CardBrand.Visa, product, "product3");
        var largeTicket = CostEstimator.Find(rates, CardBrand.Visa, product, "largeTicket");
        if (product3 is null || largeTicket is null || product3.Percent <= largeTicket.Percent)
        {
            return;
        }

        var breakEven = (largeTicket.Fixed - product3.Fixed) / ((product3.Percent - largeTicket.Percent) / 100m);
        if (amount < breakEven)
        {
            return;
        }

        var arguments = CheckArguments.For(Pointer, null);
        arguments["detail"] = rates.Name;
        arguments["expected"] = CheckArguments.Number(Math.Round(breakEven, 2, MidpointRounding.AwayFromZero));
        sink.Report(new FindingDraft(rule, Pointer)
        {
            Actual = CheckArguments.Number(amount),
            Expected = arguments["expected"],
            Arguments = arguments,
        });
    }
}
