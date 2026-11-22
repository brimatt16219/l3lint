using System.Text;
using System.Text.RegularExpressions;

namespace L3Lint.Core.Detectors;

/// <summary>
/// Normalizes descriptive text before data quality matching (Card brand rules tab, "Normalization before
/// matching"): trim, collapse inner whitespace, lowercase, strip punctuation except inner hyphens, and remove a
/// trailing sequence marker ("item 1" and "item #2" become "item").
/// </summary>
internal static partial class TextNormalizer
{
    public static string? Normalize(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                builder.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                builder.Append(' ');
            }
        }

        // Hyphens survive only between word characters ("x-ray"), not at the edges of words ("--", "- item").
        var words = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('-'))
            .Where(w => w.Length > 0)
            .ToList();
        var joined = string.Join(' ', words);
        var match = TrailingSequence().Match(joined);
        return match.Success ? match.Groups[1].Value : joined;
    }

    [GeneratedRegex(@"^(.*\S)\s+[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingSequence();
}
