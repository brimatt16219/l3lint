namespace L3Lint.Core.Fields;

/// <summary>
/// Orders JSON Pointers segment by segment, comparing array indexes as numbers, so "/lineItems/2" sorts before
/// "/lineItems/10". Used wherever output lists pointers, so order never depends on thread timing (NFR-001).
/// </summary>
/// <remarks>
/// Segments made only of digits sort first, by numeric value; all other segments follow in ordinal order. Keeping
/// the two groups apart makes this a total order even when one object mixes keys such as "2", "10" and "1a".
/// </remarks>
public sealed class PointerComparer : IComparer<string>
{
    private PointerComparer()
    {
    }

    /// <summary>Gets the shared instance.</summary>
    public static PointerComparer Instance { get; } = new();

    /// <inheritdoc/>
    public int Compare(string? x, string? y)
    {
        if (x is null || y is null)
        {
            return x is null ? (y is null ? 0 : -1) : 1;
        }

        var left = x.Split('/');
        var right = y.Split('/');
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            var result = CompareSegments(left[i], right[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    private static int CompareSegments(string left, string right)
    {
        var leftNumber = IsDigits(left);
        if (leftNumber != IsDigits(right))
        {
            return leftNumber ? -1 : 1;
        }

        if (!leftNumber)
        {
            return string.CompareOrdinal(left, right);
        }

        // Compare digit strings by value without parsing, so any length works; "01" and "1" stay distinct.
        var leftValue = left.TrimStart('0');
        var rightValue = right.TrimStart('0');
        var result = leftValue.Length.CompareTo(rightValue.Length);
        if (result == 0)
        {
            result = string.CompareOrdinal(leftValue, rightValue);
        }

        return result != 0 ? result : string.CompareOrdinal(left, right);
    }

    private static bool IsDigits(string segment) => segment.Length > 0 && segment.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0;
}
