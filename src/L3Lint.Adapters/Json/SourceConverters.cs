using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Converts canonical values back into a gateway's representation so fix patches can be written in the source
/// format (ADR-0022, FR-007): major units to minor-unit integers, decimals to strings, enums to the gateway's flags.
/// </summary>
/// <remarks>A value that does not have the expected canonical type is returned unchanged.</remarks>
internal sealed class SourceConverters
{
    private readonly Currency _currency;
    private readonly IReadOnlySet<string> _alpha3;

    /// <summary>Initializes converters for one document.</summary>
    /// <param name="currency">The currency whose exponent sets minor units.</param>
    /// <param name="alpha3">Canonical pointers whose source sent an alpha-3 country code.</param>
    public SourceConverters(Currency currency, IReadOnlySet<string> alpha3)
    {
        _currency = currency;
        _alpha3 = alpha3;
    }

    /// <summary>Gets the converter for a route, or <see langword="null"/> when values are written unchanged.</summary>
    public SourceValueConverter? For(Route route, string canonicalPointer) => route.Shape switch
    {
        Shape.MinorUnits => value => ToMinorUnits(value, asText: false),
        Shape.MinorUnitsText => value => ToMinorUnits(value, asText: true),
        Shape.DecimalText or Shape.WholeNumberText => static value => TryNumber(value, out var number) ? JsonValue.Create(number.ToString(CultureInfo.InvariantCulture)) : value,
        Shape.FlagText => static value => TryFlag(value, out var flag) ? JsonValue.Create(flag ? "true" : "false") : value,
        Shape.InverseFlag => static value => TryFlag(value, out var flag) ? JsonValue.Create(!flag) : value,
        Shape.DateDayMonthYear => static value => TryText(value, out var text)
            && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? JsonValue.Create(date.ToString("ddMMyy", CultureInfo.InvariantCulture))
                : value,
        Shape.Country when _alpha3.Contains(canonicalPointer) => static value => TryText(value, out var text) && CountryCodes.TryGetAlpha3(text, out var alpha3) ? JsonValue.Create(alpha3) : value,
        Shape.LowercaseCurrency => static value => TryText(value, out var text) ? JsonValue.Create(Lowercase(text)) : value,
        Shape.TaxExempt => static value => Indicator(value) is { } exempt ? JsonValue.Create(exempt) : null,
        Shape.TaxExemptText => static value => Indicator(value) is { } exempt ? JsonValue.Create(exempt ? "true" : "false") : null,
        Shape.Taxable => static value => Indicator(value) is { } exempt ? JsonValue.Create(!exempt) : null,
        _ => null,
    };

    /// <summary>Stripe writes currencies in lowercase; the canonical model uses ISO 4217 uppercase.</summary>
#pragma warning disable CA1308 // Stripe's API defines currency codes in lowercase, so lowercase is the required output.
    private static string Lowercase(string text) => text.ToLowerInvariant();
#pragma warning restore CA1308

    // "exempt" maps to true, "taxable" to false; "notProvided" has no source representation.
    private static bool? Indicator(JsonNode? value) => TryText(value, out var text)
        ? text switch
        {
            "exempt" => true,
            "taxable" => false,
            _ => null,
        }
        : null;

    private static bool TryText(JsonNode? value, out string text)
    {
        text = string.Empty;
        if (value is JsonValue node && node.GetValueKind() == JsonValueKind.String)
        {
            text = node.GetValue<string>();
            return true;
        }

        return false;
    }

    private static bool TryNumber(JsonNode? value, out decimal number)
    {
        number = 0m;
        return value is JsonValue node && node.GetValueKind() == JsonValueKind.Number
            && decimal.TryParse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryFlag(JsonNode? value, out bool flag)
    {
        flag = false;
        if (value is JsonValue node && node.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            flag = node.GetValueKind() == JsonValueKind.True;
            return true;
        }

        return false;
    }

    private JsonNode? ToMinorUnits(JsonNode? value, bool asText)
    {
        if (!TryNumber(value, out var major))
        {
            return value;
        }

        var minor = decimal.Round(new Money(major, _currency).ToMinorUnits(), 0, MidpointRounding.AwayFromZero);
        if (asText)
        {
            return JsonValue.Create(minor.ToString(CultureInfo.InvariantCulture));
        }

        return minor is >= long.MinValue and <= long.MaxValue ? JsonValue.Create(decimal.ToInt64(minor)) : JsonValue.Create(minor);
    }
}
