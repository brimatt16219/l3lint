using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Csv;

/// <summary>
/// The parsed CSV file: the header row and each data row with the file position of every cell, so findings can
/// point at the physical line and column where a mapped cell starts (ADR-0021).
/// </summary>
/// <param name="Header">The column names from the first record.</param>
/// <param name="Rows">The data records in file order.</param>
internal sealed record CsvTable(IReadOnlyList<string> Header, IReadOnlyList<CsvRow> Rows)
{
    /// <summary>
    /// Parses CSV text with CsvHelper. Blank lines are skipped; a quote inside an unquoted field is kept as text.
    /// </summary>
    /// <param name="text">The CSV text without a byte order mark.</param>
    /// <param name="delimiter">The field delimiter.</param>
    /// <param name="sourceName">The input name for errors.</param>
    /// <returns>The table.</returns>
    /// <exception cref="InputException">The file has no header row or cannot be parsed.</exception>
    public static CsvTable Parse(string text, char delimiter, string sourceName)
    {
        var lineStarts = LineStarts(text);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = false,
            IgnoreBlankLines = true,
            BadDataFound = null,
            DetectDelimiter = false,
        };

        string[]? header = null;
        var rows = new List<CsvRow>();
        using var reader = new StringReader(text);
        using var parser = new CsvParser(reader, config);
        try
        {
            while (parser.Read())
            {
                var record = parser.Record ?? [];
                var raw = parser.RawRecord;
                var start = (int)(parser.CharCount - raw.Length);
                var positions = CellPositions(raw, start, delimiter, record.Length, lineStarts);
                if (header is null)
                {
                    header = record;
                    continue;
                }

                rows.Add(new CsvRow(positions.Length > 0 ? positions[0].Line : Locate(start, lineStarts).Line, record, positions));
            }
        }
        catch (CsvHelperException ex)
        {
            var line = rows.Count > 0 ? rows[^1].Line + 1 : 1;
            throw new InputException(InputErrorKind.Malformed, Messages.Format(Resources.Strings.CsvMalformed, line), sourceName, line, 0, ex);
        }

        if (header is null)
        {
            throw new InputException(InputErrorKind.Malformed, Resources.Strings.CsvNoHeader, sourceName, 1, 1);
        }

        return new CsvTable(header, rows);
    }

    /// <summary>Turns a 1-based column number into spreadsheet letters: 1 is A, 27 is AA.</summary>
    public static string ColumnLetters(int column)
    {
        var letters = string.Empty;
        while (column > 0)
        {
            var remainder = (column - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            column = (column - 1) / 26;
        }

        return letters;
    }

    // Each record's raw text starts at CharCount minus its length; a cell starts after each delimiter outside quotes.
    private static SourceLocation[] CellPositions(string raw, int start, char delimiter, int count, List<int> lineStarts)
    {
        var positions = new SourceLocation[count];
        if (count == 0)
        {
            return positions;
        }

        positions[0] = Locate(start, lineStarts);
        var cell = 1;
        var quoted = false;
        for (var i = 0; i < raw.Length && cell < count; i++)
        {
            var c = raw[i];
            if (c == '"')
            {
                quoted = !quoted;
            }
            else if (c == delimiter && !quoted)
            {
                positions[cell++] = Locate(start + i + 1, lineStarts);
            }
        }

        return positions;
    }

    private static SourceLocation Locate(int offset, List<int> lineStarts)
    {
        var index = lineStarts.BinarySearch(offset);
        if (index < 0)
        {
            index = ~index - 1;
        }

        return new SourceLocation(index + 1, offset - lineStarts[index] + 1);
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' || (text[i] == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
            {
                starts.Add(i + 1);
            }
        }

        return starts;
    }
}

/// <summary>One CSV data record.</summary>
/// <param name="Line">The 1-based physical line where the record starts.</param>
/// <param name="Cells">The cell values as parsed (quotes removed).</param>
/// <param name="Positions">The line and 1-based character column where each cell starts.</param>
internal sealed record CsvRow(int Line, string[] Cells, SourceLocation[] Positions);
