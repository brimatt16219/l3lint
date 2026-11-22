using System.Globalization;
using L3Lint.Core.Fields;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Csv;

/// <summary>Converts CSV text to the type of a canonical field, invariant culture only.</summary>
internal static class CsvValues
{
    private const NumberStyles DecimalStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

    /// <summary>
    /// Converts text for a field.
    /// </summary>
    /// <param name="field">The canonical field.</param>
    /// <param name="text">The resolved cell, constant or default text.</param>
    /// <param name="minorUnits">The currency when the value is an amount in minor units; otherwise <see langword="null"/>.</param>
    /// <param name="value">The converted value.</param>
    /// <param name="expectation">What the field needs, for the Invalid note when conversion fails.</param>
    /// <returns><see langword="true"/> when the text converts.</returns>
    public static bool TryConvert(CanonicalField field, string text, Currency? minorUnits, out object value, out string expectation)
    {
        value = text;
        expectation = Resources.Strings.ExpectText;
        var trimmed = text.Trim();
        switch (field.Kind)
        {
            case FieldKind.Decimal:
                expectation = minorUnits is null ? Resources.Strings.ExpectNumber : Resources.Strings.ExpectMinorUnits;
                if (!decimal.TryParse(trimmed, DecimalStyle, CultureInfo.InvariantCulture, out var number))
                {
                    return false;
                }

                if (minorUnits is { } currency)
                {
                    if (number != decimal.Truncate(number))
                    {
                        return false;
                    }

                    number = Money.FromMinorUnits(number, currency).Value;
                }

                value = number;
                return true;
            case FieldKind.Integer:
                expectation = Resources.Strings.ExpectInteger;
                if (int.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                {
                    value = integer;
                    return true;
                }

                return false;
            case FieldKind.Boolean:
                expectation = Resources.Strings.ExpectBoolean;
                if (TryFlag(trimmed, out var flag))
                {
                    value = flag;
                    return true;
                }

                return false;
            case FieldKind.Date:
                expectation = Resources.Strings.ExpectDate;
                if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    value = date;
                    return true;
                }

                return false;
            case FieldKind.Enum:
                expectation = Messages.Format(Resources.Strings.ExpectEnum, string.Join(", ", field.EnumValues ?? []));
                return TryEnum(field.Path, trimmed, out value);
            case FieldKind.TextList:
                // One cell holds every advice line, separated by "|".
                value = text.Split('|').ToList();
                return true;
            default:
                return true;
        }
    }

    private static bool TryFlag(string text, out bool flag)
    {
        flag = text.ToUpperInvariant() is "TRUE" or "Y" or "YES" or "1";
        return flag || text.ToUpperInvariant() is "FALSE" or "N" or "NO" or "0";
    }

    private static bool TryEnum(string path, string text, out object value)
    {
        value = text;
        switch (path)
        {
            case "/channel" when CanonicalNames.TryParse(text, out Channel channel):
                value = channel;
                return true;
            case "/card/brand" when CanonicalNames.TryParse(text, out CardBrand brand):
                value = brand;
                return true;
            case "/card/product" when CanonicalNames.TryParse(text, out CardProduct product):
                value = product;
                return true;
            case "/tax/indicator" when CanonicalNames.TryParse(text, out TaxIndicator indicator):
                value = indicator;
                return true;
            case "/lineItems/*/commodityScheme" when CanonicalNames.TryParse(text, out CommodityScheme scheme):
                value = scheme;
                return true;
            case "/lineItems/*/debitCreditIndicator" when CanonicalNames.TryParse(text, out DebitCredit indicator):
                value = indicator;
                return true;
            default:
                return false;
        }
    }
}
