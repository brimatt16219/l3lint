using L3Lint.Core.Model;

namespace L3Lint.Core.Pci;

/// <summary>
/// Recognizes card numbers with the Architecture tab's algorithm (ADR-0023) and masks them.
/// </summary>
/// <remarks>
/// Steps: strip spaces and hyphens; require 13 to 19 digits; require a passing Luhn check; require a major
/// brand's prefix and length. Values with any other character are not card numbers, so a number inside a
/// sentence is not detected (a documented limitation).
/// </remarks>
internal static class CardNumberDetector
{
    /// <summary>Checks a value and returns the digits and brand when it is a card number.</summary>
    public static bool TryDetect(ReadOnlySpan<char> value, out string digits, out CardBrand brand)
    {
        digits = string.Empty;
        brand = CardBrand.Unknown;
        if (value.Length < 13)
        {
            return false;
        }

        Span<char> buffer = stackalloc char[19];
        var count = 0;
        foreach (var c in value)
        {
            if (c is ' ' or '-')
            {
                continue;
            }

            if (c is < '0' or > '9' || count == buffer.Length)
            {
                return false;
            }

            buffer[count++] = c;
        }

        var candidate = buffer[..count];
        if (count < 13 || !PassesLuhn(candidate) || !TryMatchBrand(candidate, out brand))
        {
            return false;
        }

        digits = candidate.ToString();
        return true;
    }

    /// <summary>Masks digits as asterisks plus the last four digits.</summary>
    public static string Mask(string digits) => new string('*', digits.Length - 4) + digits[^4..];

    /// <summary>The Luhn (mod 10) check from ISO/IEC 7812.</summary>
    public static bool PassesLuhn(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleIt)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    private static bool TryMatchBrand(ReadOnlySpan<char> d, out CardBrand brand)
    {
        var length = d.Length;
        var two = Prefix(d, 2);
        var three = Prefix(d, 3);
        var four = Prefix(d, 4);
        brand = CardBrand.Unknown;
        if (d[0] == '4' && length is 13 or 16 or 19)
        {
            brand = CardBrand.Visa;
        }
        else if (((two >= 51 && two <= 55) || (four >= 2221 && four <= 2720)) && length == 16)
        {
            brand = CardBrand.Mastercard;
        }
        else if (two is 34 or 37 && length == 15)
        {
            brand = CardBrand.Amex;
        }
        else if ((four == 6011 || (three >= 644 && three <= 649) || two == 65) && length is >= 16 and <= 19)
        {
            brand = CardBrand.Discover;
        }

        return brand != CardBrand.Unknown;
    }

    private static int Prefix(ReadOnlySpan<char> digits, int length)
    {
        var value = 0;
        for (var i = 0; i < length; i++)
        {
            value = (value * 10) + (digits[i] - '0');
        }

        return value;
    }
}
