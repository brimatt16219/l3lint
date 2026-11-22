using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-ARITH-004: a debit line (the default) must not carry a negative quantity or unit cost.</summary>
internal sealed class NegativeDebitLineCheck : ICustomCheck
{
    public string Name => "NegativeDebitLine";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.DebitCreditIndicator == DebitCredit.Credit)
            {
                continue;
            }

            Report(rule, sink, i, "quantity", line.Quantity);
            Report(rule, sink, i, "unitCost", line.UnitCost);
        }
    }

    private static void Report(RuleMetadata rule, IFindingSink sink, int index, string field, decimal? value)
    {
        if (value is not < 0m)
        {
            return;
        }

        var pointer = Pointers.Line(index, field);
        sink.Report(new FindingDraft(rule, pointer)
        {
            Actual = CheckArguments.Number(value.Value),
            Expected = Core.Resources.Strings.ExpectedNonNegative,
            Arguments = CheckArguments.For(pointer, index + 1),
        });
    }
}
