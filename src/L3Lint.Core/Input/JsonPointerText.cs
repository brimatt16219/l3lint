using System.Globalization;
using System.Text;

namespace L3Lint.Core.Input;

/// <summary>Builds and splits RFC 6901 JSON Pointers.</summary>
internal static class JsonPointerText
{
    /// <summary>Escapes one reference token: "~" becomes "~0" and "/" becomes "~1".</summary>
    public static string Escape(string segment) =>
        segment.Contains('~', StringComparison.Ordinal) || segment.Contains('/', StringComparison.Ordinal)
            ? segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)
            : segment;

    /// <summary>Reverses <see cref="Escape"/>.</summary>
    public static string Unescape(string segment) =>
        segment.Contains('~', StringComparison.Ordinal)
            ? segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal)
            : segment;

    /// <summary>Appends an object property to a pointer.</summary>
    public static string Append(string pointer, string property) => pointer + "/" + Escape(property);

    /// <summary>Appends an array index to a pointer.</summary>
    public static string Append(string pointer, int index) => pointer + "/" + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>Splits a pointer into unescaped segments. The root pointer "" has none.</summary>
    public static string[] Split(string pointer)
    {
        if (pointer.Length == 0)
        {
            return [];
        }

        var parts = pointer[1..].Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Unescape(parts[i]);
        }

        return parts;
    }

    /// <summary>Joins unescaped segments into a pointer.</summary>
    public static string Join(IEnumerable<string> segments)
    {
        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            builder.Append('/').Append(Escape(segment));
        }

        return builder.ToString();
    }
}
