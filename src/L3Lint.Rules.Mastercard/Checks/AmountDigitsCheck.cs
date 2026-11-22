using System.Globalization;
using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Mastercard.Checks;

/// <summary>
/// MC-FMT-001: amount fields hold 9 digits in minor units at J.P. Morgan; longer values are truncated from the
/// right. Checks the transaction amount, header money fields and line money fields.
/// </summary>
internal sealed class AmountDigitsCheck : ICustomCheck
{
    private const int MaxDigits = 9;

    private static readonly string[] Header = ["/amount", "/tax/amount", "/amounts/discount", "/amounts/freight", "/amounts/duty", "/amounts/shippingTax"];
    private static readonly string[] Line = ["unitCost", "discountAmount", "taxAmount", "total"];

    public string Name => "AmountDigits";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        if (context.Facts.Currency is not { } currency)
        {
            return;
        }

        var transaction = context.Transaction;
        foreach (var pointer in Header)
        {
            CanonicalFields.TryGet(pointer, out var field);
            Check(rule, sink, pointer, field.ReadHeader!(transaction), currency);
        }

        for (var i = 0; i < transaction.LineItems.Count; i++)
        {
            foreach (var name in Line)
            {
                CanonicalFields.TryGet("/lineItems/*/" + name, out var field);
                Check(rule, sink, Pointers.Line(i, name), field.ReadLine!(transaction.LineItems[i]), currency);
            }
        }
    }

    private static void Check(RuleMetadata rule, IFindingSink sink, string pointer, FieldValue value, Currency currency)
    {
        if (value.Kind != FieldValueKind.Number)
        {
            return;
        }

        var minor = decimal.Truncate(Math.Abs(new Money(value.Number, currency).ToMinorUnits()));
        var digits = minor.ToString(CultureInfo.InvariantCulture).Length;
        if (digits <= MaxDigits)
        {
            return;
        }

        var arguments = CheckArguments.For(pointer, null);
        arguments["length"] = CheckArguments.Number(digits);
        arguments["max"] = CheckArguments.Number(MaxDigits);
        sink.Report(new FindingDraft(rule, pointer) { Actual = CheckArguments.Number(value.Number), Arguments = arguments });
    }
}
