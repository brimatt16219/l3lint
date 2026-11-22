using System.Globalization;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Json;

/// <summary>
/// A source JSON Pointer with an optional "#" placeholder for a line key, for example
/// "/amount_details/line_items/#/unit_cost". The placeholder may also sit inside one segment, as in Adyen's
/// legacy "/additionalData/enhancedSchemeData.itemDetailLine#.unitPrice".
/// </summary>
/// <remarks>Immutable. Template segments are literal property names that need no RFC 6901 escaping.</remarks>
internal sealed class SourceTemplate
{
    private const char Placeholder = '#';

    private readonly string[] _segments;
    private readonly int _keyBase;

    /// <summary>Initializes a new template.</summary>
    /// <param name="template">The pointer text, with at most one "#".</param>
    /// <param name="keyBase">The key of the first line: 0 for arrays, 1 for formats that number lines from 1.</param>
    public SourceTemplate(string template, int keyBase = 0)
    {
        Text = template;
        _keyBase = keyBase;
        _segments = template.Length == 0 ? [] : template[1..].Split('/');
        HasKey = template.Contains(Placeholder, StringComparison.Ordinal);
    }

    /// <summary>Gets the template text.</summary>
    public string Text { get; }

    /// <summary>Gets a value indicating whether the template holds a line key placeholder.</summary>
    public bool HasKey { get; }

    /// <summary>Builds the concrete pointer for a line key (an array index, or a line number in a key).</summary>
    public string Format(int key) =>
        HasKey ? Text.Replace("#", key.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) : Text;

    /// <summary>
    /// Matches a concrete pointer split into unescaped segments.
    /// </summary>
    /// <param name="segments">The pointer segments.</param>
    /// <param name="lineIndex">The 0-based line index taken from the placeholder, or -1 when the template has none.</param>
    /// <returns><see langword="true"/> when the pointer fits the template.</returns>
    public bool TryMatch(string[] segments, out int lineIndex)
    {
        lineIndex = -1;
        if (segments.Length != _segments.Length)
        {
            return false;
        }

        for (var i = 0; i < segments.Length; i++)
        {
            var expected = _segments[i];
            var placeholder = expected.IndexOf(Placeholder, StringComparison.Ordinal);
            if (placeholder < 0)
            {
                if (!string.Equals(expected, segments[i], StringComparison.Ordinal))
                {
                    return false;
                }

                continue;
            }

            var prefix = expected[..placeholder];
            var suffix = expected[(placeholder + 1)..];
            var actual = segments[i];
            if (actual.Length <= prefix.Length + suffix.Length
                || !actual.StartsWith(prefix, StringComparison.Ordinal)
                || !actual.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            var digits = actual.AsSpan(prefix.Length, actual.Length - prefix.Length - suffix.Length);
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var key) || key < _keyBase)
            {
                return false;
            }

            lineIndex = key - _keyBase;
        }

        return true;
    }

    /// <summary>Splits a concrete pointer into unescaped segments for <see cref="TryMatch"/>.</summary>
    public static string[] Split(string pointer) => JsonPointerText.Split(pointer);
}
