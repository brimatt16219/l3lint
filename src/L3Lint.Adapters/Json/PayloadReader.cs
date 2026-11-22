using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Reads values from a gateway document through routes, tolerantly: a value of the wrong type becomes an
/// <see cref="AdapterNoteKind.Invalid"/> note and stays absent, never an exception (ADR-0036).
/// </summary>
/// <remarks>Not thread-safe; one reader per document.</remarks>
internal sealed class PayloadReader
{
    private readonly HashSet<string> _brokenReported = new(StringComparer.Ordinal);
    private readonly HashSet<string> _alpha3 = new(StringComparer.Ordinal);

    /// <summary>Initializes a reader.</summary>
    /// <param name="root">The scrubbed document root.</param>
    /// <param name="log">Where notes and invalid pointers go.</param>
    public PayloadReader(JsonElement root, MappingLog log)
    {
        Root = root;
        Log = log;
    }

    /// <summary>The result of following a pointer.</summary>
    public enum Lookup
    {
        /// <summary>The value is absent or JSON null.</summary>
        Absent,

        /// <summary>The value exists.</summary>
        Found,

        /// <summary>A container on the way has the wrong type (a plain value, or an array where an object belongs), so the value cannot be read.</summary>
        Broken,
    }

    /// <summary>Gets the document root.</summary>
    public JsonElement Root { get; }

    /// <summary>Gets the mapping log.</summary>
    public MappingLog Log { get; }

    /// <summary>Gets or sets the currency used to convert minor units (FR-008).</summary>
    public Currency MinorUnitCurrency { get; set; } = new("USD", 2);

    /// <summary>Gets the canonical pointers whose source values were alpha-3 country codes converted to alpha-2.</summary>
    public IReadOnlySet<string> Alpha3Countries => _alpha3;

    /// <summary>Follows a concrete pointer from the root.</summary>
    /// <param name="pointer">The source pointer.</param>
    /// <param name="value">The value when found.</param>
    /// <param name="brokenAt">The pointer of the container with the wrong type, when broken.</param>
    /// <returns>Whether the value was found, absent or unreachable.</returns>
    public Lookup Find(string pointer, out JsonElement value, out string? brokenAt)
    {
        value = default;
        brokenAt = null;
        var current = Root;
        var path = string.Empty;
        foreach (var segment in JsonPointerText.Split(pointer))
        {
            JsonElement next;
            switch (current.ValueKind)
            {
                case JsonValueKind.Object:
                    if (!current.TryGetProperty(segment, out next))
                    {
                        return Lookup.Absent;
                    }

                    break;
                case JsonValueKind.Array:
                    if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    {
                        // An array where the format has an object: nothing inside it can be read.
                        brokenAt = path;
                        return Lookup.Broken;
                    }

                    if (index >= current.GetArrayLength())
                    {
                        return Lookup.Absent;
                    }

                    next = current[index];
                    break;
                case JsonValueKind.Null:
                    return Lookup.Absent;
                default:
                    brokenAt = path;
                    return Lookup.Broken;
            }

            current = next;
            path = JsonPointerText.Append(path, segment);
        }

        if (current.ValueKind == JsonValueKind.Null)
        {
            return Lookup.Absent;
        }

        value = current;
        return Lookup.Found;
    }

    /// <summary>Gets the element at a pointer, or <see langword="null"/> when it is absent or unreachable.</summary>
    public JsonElement? Element(string pointer) => Find(pointer, out var value, out _) == Lookup.Found ? value : null;

    /// <summary>Records that a container on the way to a canonical field has the wrong type.</summary>
    public void ReportBroken(string brokenAt, string canonical)
    {
        if (_brokenReported.Add(brokenAt))
        {
            Log.AddInvalid(brokenAt, canonical, Messages.Format(Resources.Strings.InvalidContainer, brokenAt));
        }
        else
        {
            Log.MarkInvalid(canonical);
        }
    }

    /// <summary>Reads a text field (text, currency or country shapes).</summary>
    public string? ReadText(Route? route, int key, string canonical)
    {
        if (!TryLocate(route, key, canonical, out var pointer, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            ReportInvalid(pointer, canonical, route!.Shape);
            return null;
        }

        var text = value.GetString()!;
        switch (route!.Shape)
        {
            case Shape.LowercaseCurrency:
                return text.ToUpperInvariant();
            case Shape.Country:
                if (text.Length == 3 && CountryCodes.TryGetAlpha2(text, out var alpha2))
                {
                    _alpha3.Add(canonical);
                    return alpha2;
                }

                if (text.Length != 2)
                {
                    Log.Add(AdapterNoteKind.Ambiguous, pointer, canonical, Messages.Format(Resources.Strings.NoteCountryNotCode, pointer));
                }

                return text;
            default:
                return text;
        }
    }

    /// <summary>Reads a number: an amount (converted to major units) or a quantity.</summary>
    public decimal? ReadNumber(Route? route, int key, string canonical)
    {
        if (!TryLocate(route, key, canonical, out var pointer, out var value))
        {
            return null;
        }

        var shape = route!.Shape;
        decimal number;
        switch (shape)
        {
            case Shape.MinorUnits:
            case Shape.WholeNumber:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out number) || number != decimal.Truncate(number))
                {
                    ReportInvalid(pointer, canonical, shape);
                    return null;
                }

                break;
            case Shape.MinorUnitsText:
            case Shape.WholeNumberText:
                if (value.ValueKind != JsonValueKind.String
                    || !decimal.TryParse(value.GetString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number))
                {
                    ReportInvalid(pointer, canonical, shape);
                    return null;
                }

                break;
            default:
                if (!TryDecimal(value, out number))
                {
                    ReportInvalid(pointer, canonical, shape);
                    return null;
                }

                break;
        }

        return shape is Shape.MinorUnits or Shape.MinorUnitsText ? Money.FromMinorUnits(number, MinorUnitCurrency).Value : number;
    }

    /// <summary>Reads a boolean field.</summary>
    public bool? ReadFlag(Route? route, int key, string canonical)
    {
        if (!TryLocate(route, key, canonical, out var pointer, out var value))
        {
            return null;
        }

        if (!TryFlag(value, route!.Shape == Shape.YesNoFlag, out var flag))
        {
            ReportInvalid(pointer, canonical, route.Shape);
            return null;
        }

        return route.Shape == Shape.InverseFlag ? !flag : flag;
    }

    /// <summary>Reads a date field.</summary>
    public DateOnly? ReadDate(Route? route, int key, string canonical)
    {
        if (!TryLocate(route, key, canonical, out var pointer, out var value))
        {
            return null;
        }

        var format = route!.Shape == Shape.DateDayMonthYear ? "ddMMyy" : "yyyy-MM-dd";
        if (value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        ReportInvalid(pointer, canonical, route.Shape);
        return null;
    }

    /// <summary>Reads a tax exempt or taxable flag as a tax indicator.</summary>
    public TaxIndicator? ReadTaxIndicator(Route? route, string canonical)
    {
        if (!TryLocate(route, 0, canonical, out var pointer, out var value))
        {
            return null;
        }

        if (!TryFlag(value, yesNo: false, out var flag))
        {
            ReportInvalid(pointer, canonical, route!.Shape);
            return null;
        }

        var exempt = route!.Shape == Shape.Taxable ? !flag : flag;
        return exempt ? TaxIndicator.Exempt : TaxIndicator.Taxable;
    }

    /// <summary>Reads a debit or credit indicator.</summary>
    public DebitCredit? ReadDebitCredit(Route? route, int key, string canonical)
    {
        if (!TryLocate(route, key, canonical, out var pointer, out var value))
        {
            return null;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (string.Equals(text, "debit", StringComparison.OrdinalIgnoreCase))
        {
            return DebitCredit.Debit;
        }

        if (string.Equals(text, "credit", StringComparison.OrdinalIgnoreCase))
        {
            return DebitCredit.Credit;
        }

        ReportInvalid(pointer, canonical, route!.Shape);
        return null;
    }

    /// <summary>Describes what a shape expects, for Invalid notes.</summary>
    public static string Expectation(Shape shape) => shape switch
    {
        Shape.MinorUnits => Resources.Strings.ExpectMinorUnits,
        Shape.MinorUnitsText => Resources.Strings.ExpectMinorUnitsText,
        Shape.DecimalText => Resources.Strings.ExpectDecimal,
        Shape.WholeNumber => Resources.Strings.ExpectWholeNumber,
        Shape.WholeNumberText => Resources.Strings.ExpectWholeNumberText,
        Shape.Flag or Shape.FlagText or Shape.InverseFlag or Shape.TaxExempt or Shape.TaxExemptText or Shape.Taxable => Resources.Strings.ExpectFlag,
        Shape.YesNoFlag => Resources.Strings.ExpectYesNo,
        Shape.Date => Resources.Strings.ExpectDate,
        Shape.DateDayMonthYear => Resources.Strings.ExpectDateDayMonthYear,
        Shape.DebitCredit => Resources.Strings.ExpectDebitCredit,
        _ => Resources.Strings.ExpectText,
    };

    private static bool TryDecimal(JsonElement value, out decimal number)
    {
        number = 0m;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out number),
            JsonValueKind.String => decimal.TryParse(value.GetString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number),
            _ => false,
        };
    }

    private static bool TryFlag(JsonElement value, bool yesNo, out bool flag)
    {
        flag = false;
        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                flag = true;
                return true;
            case JsonValueKind.False:
                return true;
            case JsonValueKind.String:
                var text = value.GetString();
                if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || (yesNo && string.Equals(text, "Y", StringComparison.OrdinalIgnoreCase)))
                {
                    flag = true;
                    return true;
                }

                return string.Equals(text, "false", StringComparison.OrdinalIgnoreCase) || (yesNo && string.Equals(text, "N", StringComparison.OrdinalIgnoreCase));
            default:
                return false;
        }
    }

    private bool TryLocate(Route? route, int key, string canonical, out string pointer, out JsonElement value)
    {
        pointer = string.Empty;
        value = default;
        if (route is null)
        {
            return false;
        }

        pointer = route.Source.Format(key);
        switch (Find(pointer, out value, out var brokenAt))
        {
            case Lookup.Found:
                return true;
            case Lookup.Broken:
                ReportBroken(brokenAt!, canonical);
                return false;
            default:
                return false;
        }
    }

    private void ReportInvalid(string pointer, string canonical, Shape shape) =>
        Log.AddInvalid(pointer, canonical, Messages.Format(Resources.Strings.InvalidValue, pointer, Expectation(shape)));
}
