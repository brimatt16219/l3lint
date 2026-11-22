using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using L3Lint.Core.Fields;
using L3Lint.Core.Fixes;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// The mechanical fixes l3lint may propose (US-06, ADR-0022): values that follow from other data in the
/// payload, documented unit maps and trimming. Descriptions, commodity codes and tax amounts are never invented.
/// </summary>
/// <remarks>Operations use RFC 6902 <c>add</c>, which also replaces an existing member.</remarks>
internal static class MechanicalFixes
{
    /// <summary>A fix for a missing value, when the payload proves what it must be.</summary>
    public static FixIntent? ForMissing(in RuleContext context, string pointer, CanonicalField field)
    {
        var transaction = context.Transaction;
        switch (field.Path)
        {
            case "/tax/indicator" when transaction.Tax.Amount > 0m:
                return Set(pointer, JsonValue.Create("taxable"));
            case "/lineItems/*/debitCreditIndicator":
                // Absent already means debit (canonical dictionary), so stating it changes nothing else.
                return Set(pointer, JsonValue.Create("debit"));
            case "/lineItems/*/taxable" when LineAt(context, pointer) is { TaxAmount: > 0m }:
                return Set(pointer, JsonValue.Create(true));
            case "/lineItems/*/discountAmount" when LineBalancesWithoutDiscount(context, pointer):
                return Set(pointer, JsonValue.Create(ZeroFor(context)));
            case "/amounts/discount" or "/amounts/freight" or "/amounts/duty" when TotalBalances(context):
                return Set(pointer, JsonValue.Create(ZeroFor(context)));
            default:
                return null;
        }
    }

    /// <summary>Trims text to its first <paramref name="max"/> characters.</summary>
    public static FixIntent Trim(string pointer, string text, int max)
    {
        var builder = new StringBuilder();
        var count = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (count++ == max)
            {
                break;
            }

            builder.Append(rune.ToString());
        }

        return Set(pointer, JsonValue.Create(builder.ToString().TrimEnd()));
    }

    /// <summary>Maps a unit to a listed code: case fixes and certain maps are mechanical; proposals need a person.</summary>
    public static FixIntent? UnitOfMeasure(in RuleContext context, string listName, string pointer, string value)
    {
        var list = context.Lists.Values(listName);
        var upper = value.Trim().ToUpperInvariant();
        if (list.Contains(upper))
        {
            return Set(pointer, JsonValue.Create(upper));
        }

        if (UnitOfMeasureMaps.Certain.TryGetValue(listName, out var certain) && certain.TryGetValue(value.Trim(), out var mapped) && list.Contains(mapped))
        {
            return Set(pointer, JsonValue.Create(mapped));
        }

        if (UnitOfMeasureMaps.Proposed.TryGetValue(listName, out var proposed) && proposed.TryGetValue(value.Trim(), out var guess) && list.Contains(guess))
        {
            return new FixIntent(
                [new PatchOperation("add", pointer, JsonValue.Create(guess))],
                NeedsHuman: true,
                Note: string.Format(CultureInfo.InvariantCulture, Resources.Strings.FixProposalUnit, value, guess));
        }

        return null;
    }

    private static FixIntent Set(string pointer, JsonNode? value) => new([new PatchOperation("add", pointer, value)]);

    // 0 written with the currency's scale (0.00 for USD, 0 for JPY), matching how amounts are sent.
    private static decimal ZeroFor(in RuleContext context) =>
        context.Facts.Currency is { } currency ? 0m * currency.MinorUnit : 0m;

    private static LineItem? LineAt(in RuleContext context, string pointer)
    {
        var parts = pointer.Split('/');
        return parts.Length > 2 && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            && index < context.Transaction.LineItems.Count
            ? context.Transaction.LineItems[index]
            : null;
    }

    // A missing line discount is provably 0 when q x u (+ included tax) already equals the reported total.
    private static bool LineBalancesWithoutDiscount(in RuleContext context, string pointer)
    {
        var line = LineAt(context, pointer);
        if (line is not { Quantity: { } q, UnitCost: { } u, Total: { } total } || context.Facts.Currency is not { } currency)
        {
            return false;
        }

        var included = line.AmountIncludesTax == true ? line.TaxAmount ?? 0m : 0m;
        var expected = Math.Round(q * u, currency.Exponent, context.Facts.Rounding) + included;
        return Math.Abs(expected - total) <= context.Facts.LineTolerance;
    }

    // Missing summary amounts count as 0 in formula 2; when the total already balances, 0 is provably right.
    private static bool TotalBalances(in RuleContext context) =>
        context.Facts.ExpectedAmount is { } expected && context.Transaction.Amount is { } amount
        && Math.Abs(expected - amount) <= context.Facts.TotalTolerance;
}
