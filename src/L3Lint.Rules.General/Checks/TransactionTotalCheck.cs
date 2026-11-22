using System.Text.Json.Nodes;
using L3Lint.Core.Checks;
using L3Lint.Core.Fixes;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-ARITH-002 (formula 2): A = sum(total_i - t_i x I_i) + T + F + D + S - H, with credit lines subtracting
/// (ADR-0014). When a line already fails formula 1, the message names that as the likely cause so the user
/// fixes one thing, not two. The amount patch is applied only with <c>--fix-amount</c>. When a value the sum needs
/// is invalid, the check is skipped: the schema finding for that value is the one finding (ADR-0036).
/// </summary>
internal sealed class TransactionTotalCheck : ICustomCheck
{
    private const string Pointer = "/amount";

    // Handling is not part of the sum (ADR-0014), so an invalid handling amount does not stop the check.
    private static readonly string[] HeaderComponents = ["/tax/amount", "/amounts/discount", "/amounts/freight", "/amounts/duty", "/amounts/shippingTax"];

    private static readonly string[] LineComponents = ["total", "taxAmount", "amountIncludesTax", "debitCreditIndicator"];

    public string Name => "TransactionTotal";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var facts = context.Facts;
        if (facts.ExpectedAmount is not { } expected || context.Transaction.Amount is not { } amount || context.IsInvalid(Pointer)
            || AnyComponentInvalid(context))
        {
            return;
        }

        var difference = amount - expected;
        if (Math.Abs(difference) <= facts.TotalTolerance)
        {
            return;
        }

        var arguments = CheckArguments.For(Pointer, null);
        arguments["reported"] = CheckArguments.Number(amount);
        arguments["expected"] = CheckArguments.Number(expected);
        arguments["difference"] = CheckArguments.Number(difference);
        sink.Report(new FindingDraft(rule, Pointer)
        {
            Actual = CheckArguments.Number(amount),
            Expected = CheckArguments.Number(expected),
            Arguments = arguments,
            MessageVariant = facts.AnyLineTotalMismatch ? "lineCause" : context.Transaction.Amounts.Handling is > 0m ? "handling" : null,
            Fix = new FixIntent([new PatchOperation("add", Pointer, JsonValue.Create(expected))]),
        });
    }

    private static bool AnyComponentInvalid(in RuleContext context)
    {
        foreach (var pointer in HeaderComponents)
        {
            if (context.IsInvalid(pointer))
            {
                return true;
            }
        }

        for (var i = 0; i < context.Transaction.LineItems.Count; i++)
        {
            foreach (var field in LineComponents)
            {
                if (context.IsInvalid("/lineItems/" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + field))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
