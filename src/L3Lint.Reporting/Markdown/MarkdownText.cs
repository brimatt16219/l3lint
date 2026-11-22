using System.Text;

namespace L3Lint.Reporting.Markdown;

/// <summary>
/// Escaping for GitHub-flavored Markdown: inline text, table cells, code spans in tables and text inside HTML
/// elements such as <c>&lt;summary&gt;</c>. Payload text is data, so none of its characters may start Markdown or
/// HTML syntax.
/// </summary>
internal static class MarkdownText
{
    private const char Ellipsis = '…';
    private const char Replacement = '�';

    /// <summary>
    /// Escapes text so it prints literally: every character that can start inline Markdown or HTML gets a backslash
    /// (GFM lets any ASCII punctuation be escaped), and line breaks become <c>&lt;br&gt;</c> so table rows stay on one line.
    /// </summary>
    public static string Escape(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var builder = new StringBuilder(text.Length + 16);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '&' or '|' or '~' or '#' or '!' or '$':
                    builder.Append('\\').Append(c);
                    break;
                case '\r':
                    if (i + 1 >= text.Length || text[i + 1] != '\n')
                    {
                        builder.Append("<br>");
                    }

                    break;
                case '\n':
                    builder.Append("<br>");
                    break;
                case '\t':
                    builder.Append(' ');
                    break;
                default:
                    builder.Append(char.IsControl(c) ? Replacement : c);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes a code span for a table cell. The fence is one backtick longer than any backtick run in the text, and
    /// pipes are escaped because GFM splits table cells before it parses code spans.
    /// </summary>
    public static string TableCode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var flat = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            flat.Append(c is '\r' or '\n' or '\t' ? ' ' : char.IsControl(c) ? Replacement : c);
        }

        var value = flat.ToString();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var longest = 0;
        var run = 0;
        foreach (var c in value)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        var fence = new string('`', longest + 1);
        var pad = value[0] == '`' || value[^1] == '`' || (value[0] == ' ' && value[^1] == ' ') ? " " : string.Empty;
        return fence + pad + value.Replace("|", "\\|", StringComparison.Ordinal) + pad + fence;
    }

    /// <summary>Encodes text for use inside an HTML element, where Markdown is not parsed.</summary>
    public static string Html(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var builder = new StringBuilder(text.Length + 16);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&#39;",
                '\r' or '\n' or '\t' => " ",
                _ when char.IsControl(c) => Replacement.ToString(),
                _ => c.ToString(),
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// Shortens text to at most <paramref name="max"/> characters, the last of which is an ellipsis when text was
    /// cut, never splitting a surrogate pair.
    /// </summary>
    public static string Clip(string text, int max)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length <= max)
        {
            return text;
        }

        var cut = max - 1;
        if (cut > 0 && char.IsHighSurrogate(text[cut - 1]))
        {
            cut--;
        }

        return string.Concat(text.AsSpan(0, cut), Ellipsis.ToString());
    }
}
