using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Fixes;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Mastercard.Checks;

/// <summary>
/// MC-L3-008: per line, the tax indicator (<c>taxable</c>) must be present, the discount indicator must be
/// present when the line has a discount, and the tax type code must be present when the line has tax. The
/// discount indicator and the taxable flag follow from the amounts, so they get mechanical fixes.
/// </summary>
internal sealed class MastercardLineIndicatorsCheck : ICustomCheck
{
    public string Name => "MastercardLineIndicators";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Taxable is null)
            {
                var fix = line.TaxAmount is > 0m ? Set(Pointers.Line(i, "taxable"), true) : null;
                Report(rule, sink, i, "taxable", fix);
            }

            if (line.DiscountIndicator is null && line.DiscountAmount is > 0m)
            {
                Report(rule, sink, i, "discountIndicator", Set(Pointers.Line(i, "discountIndicator"), true));
            }

            if (string.IsNullOrWhiteSpace(line.TaxTypeCode) && line.TaxAmount is > 0m)
            {
                Report(rule, sink, i, "taxTypeCode", null);
            }
        }
    }

    private static FixIntent Set(string pointer, bool value) =>
        new([new PatchOperation("add", pointer, System.Text.Json.Nodes.JsonValue.Create(value))]);

    private static void Report(RuleMetadata rule, IFindingSink sink, int index, string field, FixIntent? fix)
    {
        var pointer = Pointers.Line(index, field);
        sink.Report(new FindingDraft(rule, pointer) { Arguments = CheckArguments.For(pointer, index + 1), Fix = fix });
    }
}
