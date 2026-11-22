using System.Text.Json.Nodes;
using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Fixes;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-FMT-003: the amount must be above 0, and no money field may have more decimals than the currency
/// exponent. Unit cost is exempt because unit prices often carry 3 or 4 decimals (Card brand rules tab).
/// </summary>
internal sealed class AmountPrecisionCheck : ICustomCheck
{
    private static readonly string[] HeaderMoney = ["/amount", "/tax/amount", "/amounts/discount", "/amounts/freight", "/amounts/duty", "/amounts/shippingTax", "/amounts/handling"];
    private static readonly string[] LineMoney = ["discountAmount", "taxAmount", "total"];

    public string Name => "AmountPrecision";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var transaction = context.Transaction;
        if (!context.IsInvalid("/amount") && transaction.Amount is not > 0m)
        {
            sink.Report(new FindingDraft(rule, "/amount")
            {
                MessageVariant = "notPositive",
                Actual = transaction.Amount is { } amount ? CheckArguments.Number(amount) : null,
                Expected = Core.Resources.Strings.ExpectedPositive,
            });
        }

        if (context.Facts.Currency is not { } currency)
        {
            return;
        }

        foreach (var pointer in HeaderMoney)
        {
            CanonicalFields.TryGet(pointer, out var field);
            Check(context, rule, sink, pointer, field.ReadHeader!(transaction), currency);
        }

        for (var i = 0; i < transaction.LineItems.Count; i++)
        {
            foreach (var name in LineMoney)
            {
                CanonicalFields.TryGet("/lineItems/*/" + name, out var field);
                Check(context, rule, sink, Pointers.Line(i, name), field.ReadLine!(transaction.LineItems[i]), currency);
            }
        }
    }

    private static void Check(in RuleContext context, RuleMetadata rule, IFindingSink sink, string pointer, FieldValue value, Currency currency)
    {
        if (value.Kind != FieldValueKind.Number || context.IsInvalid(pointer) || !new Money(value.Number, currency).HasExcessPrecision)
        {
            return;
        }

        var arguments = CheckArguments.For(pointer, null);
        arguments["currency"] = currency.Code;
        var rounded = new Money(value.Number, currency).Rounded(context.Facts.Rounding);
        sink.Report(new FindingDraft(rule, pointer)
        {
            Actual = CheckArguments.Number(value.Number),
            Expected = CheckArguments.Number(rounded),
            Arguments = arguments,
            Fix = new FixIntent([new PatchOperation("add", pointer, JsonValue.Create(rounded))]),
        });
    }
}
