namespace L3Lint.Core.Fields;

/// <summary>
/// A JSON Pointer pattern where a <c>*</c> segment matches any single segment, such as /lineItems/*/productCode.
/// </summary>
internal sealed class PointerPattern
{
    private readonly string[] _segments;

    public PointerPattern(string pattern)
    {
        Pattern = pattern;
        _segments = pattern.Length == 0 ? [] : pattern[1..].Split('/');
    }

    public string Pattern { get; }

    public bool Matches(string pointer)
    {
        var parts = pointer.Length == 0 ? [] : pointer[1..].Split('/');
        if (parts.Length != _segments.Length)
        {
            return false;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (_segments[i] != "*" && !string.Equals(_segments[i], parts[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns true when any pattern in the list matches the pointer.</summary>
    public static bool AnyMatch(IReadOnlyList<PointerPattern> patterns, string pointer)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.Matches(pointer))
            {
                return true;
            }
        }

        return false;
    }
}
