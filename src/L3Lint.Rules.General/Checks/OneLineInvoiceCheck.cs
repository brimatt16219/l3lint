using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-DQ-005: exactly one line, quantity 1, and a line total equal to the amount minus tax and freight (within
/// the total tolerance): the auto-filled pattern CEDP flags.
/// </summary>
internal sealed class OneLineInvoiceCheck : ICustomCheck
{
    public string Name => "OneLineInvoice";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var transaction = context.Transaction;
        if (transaction.LineItems.Count != 1 || transaction.Amount is not { } amount)
        {
            return;
        }

        var line = transaction.LineItems[0];
        if (line.Quantity != 1m || line.Total is not { } total)
        {
            return;
        }

        var mirrored = amount - (transaction.Tax.Amount ?? 0m) - (transaction.Amounts.Freight ?? 0m);
        if (Math.Abs(total - mirrored) <= context.Facts.TotalTolerance)
        {
            sink.Report(new FindingDraft(rule, "/lineItems/0") { Actual = CheckArguments.Number(total), Arguments = CheckArguments.For("/lineItems/0", 1) });
        }
    }
}
