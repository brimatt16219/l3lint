namespace L3Lint.DocsGen;

/// <summary>Small helpers for Markdown tables and YAML table-of-contents files.</summary>
internal static class Markdown
{
    /// <summary>Makes text safe inside a table cell: one line, pipes escaped, angle brackets as entities.</summary>
    public static string Cell(string? text) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : text.ReplaceLineEndings(" ")
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// A code span that survives a table cell. Markdown keeps a backslash before a pipe inside a code span, so text
    /// with a pipe becomes an HTML <c>code</c> element with the pipe as a character reference.
    /// </summary>
    public static string Code(string text)
    {
        if (text.Contains('|', StringComparison.Ordinal))
        {
            return "<code>" + text
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("|", "&#124;", StringComparison.Ordinal) + "</code>";
        }

        var fence = text.Contains('`', StringComparison.Ordinal) ? "``" : "`";
        var padded = text.StartsWith('`') || text.EndsWith('`') ? " " + text + " " : text;
        return fence + padded + fence;
    }

    /// <summary>A double-quoted YAML scalar.</summary>
    public static string YamlString(string text) =>
        "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
