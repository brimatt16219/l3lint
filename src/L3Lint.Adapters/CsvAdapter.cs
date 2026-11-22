using L3Lint.Adapters.Csv;
using L3Lint.Core.Adapters;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;

namespace L3Lint.Adapters;

/// <summary>
/// Reads CSV exports (for example from an ERP) through a YAML column map, one transaction per <c>groupBy</c> value
/// (FR-006, US-14).
/// </summary>
/// <remarks>
/// <para>
/// The map comes from <see cref="AdapterOptions.CsvMap"/>; each canonical field is a column name, a constant
/// (<c>{ const: USD }</c>) or a column with a value map and a default (<c>{ from: taxable_flag, map: { Y: taxable },
/// default: 0 }</c>). Rows are grouped by the <c>groupBy</c> column in first-appearance order, one line item per
/// row. Transaction-level values must agree across a group's rows; a disagreement is an Invalid note, which the
/// engine reports as GEN-FMT-001. With <c>amountUnits: minor</c>, amounts are converted with the currency exponent.
/// </para>
/// <para>
/// Every cell is checked for card numbers before mapping and masked to its last four digits; the matches are
/// returned in <see cref="AdapterResult.CardNumberMatches"/> with the cell's line and column (ADR-0004). Source
/// paths are labels such as "row 14, column F (unit_price)". Stateless and thread-safe.
/// </para>
/// </remarks>
public sealed class CsvAdapter : IPayloadAdapter
{
    /// <inheritdoc/>
    public string Name => "csv";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Csv;

    /// <inheritdoc/>
    /// <remarks>CSV input is recognized by its document kind, so the score is 1.0 for CSV and 0.0 otherwise.</remarks>
    public double Detect(RawDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Kind == InputKind.Csv ? 1.0 : 0.0;
    }

    /// <inheritdoc/>
    /// <returns>One result per group of rows, in the order each group first appears.</returns>
    /// <exception cref="InputException">
    /// The document is not CSV, no map was given, the map is not valid (the message names the map file and the JSON
    /// Pointer), the map names a column the file lacks, or the file cannot be parsed.
    /// </exception>
    public IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        if (document.Kind != InputKind.Csv)
        {
            throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.AdapterNeedsCsv, Name), document.SourceName);
        }

        if (string.IsNullOrWhiteSpace(options.CsvMap))
        {
            throw new InputException(InputErrorKind.Unsupported, Resources.Strings.CsvMapMissing, document.SourceName);
        }

        var mapName = string.IsNullOrWhiteSpace(options.CsvMapName) ? Resources.Strings.DefaultMapName : options.CsvMapName;
        var map = CsvMapReader.Read(options.CsvMap, mapName, document.SourceName);
        var table = CsvTable.Parse(document.GetText(), map.Delimiter, document.SourceName);
        var columns = Columns(table);
        CheckColumns(map, columns, mapName, document.SourceName);
        var masked = MaskCells(table, ExemptColumns(map, columns, options.PciAllowFields));

        var groupColumn = columns[map.GroupBy];
        var groups = new Dictionary<string, List<CsvRow>>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var row in table.Rows)
        {
            var key = groupColumn < row.Cells.Length ? row.Cells[groupColumn].Trim() : string.Empty;
            if (!groups.TryGetValue(key, out var rows))
            {
                rows = [];
                groups[key] = rows;
                order.Add(key);
            }

            rows.Add(row);
        }

        var results = new List<AdapterResult>(order.Count);
        foreach (var key in order)
        {
            var mapper = new CsvGroupMapper(map, table, columns, groups[key], key);
            if (results.Count == 0 && options.Verbose)
            {
                IgnoredColumns(map, table, mapper.Log);
            }

            results.Add(mapper.Build(masked));
        }

        return results;
    }

    /// <inheritdoc/>
    /// <remarks>CSV source paths are cell labels, not JSON Pointers, and CSV cells are masked by this adapter itself, so this returns <see langword="null"/>.</remarks>
    public string? ToCanonicalPointer(string sourcePointer) => null;

    private static Dictionary<string, int> Columns(CsvTable table)
    {
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < table.Header.Count; i++)
        {
            columns.TryAdd(table.Header[i], i);
        }

        return columns;
    }

    private static void CheckColumns(CsvMap map, Dictionary<string, int> columns, string mapName, string sourceName)
    {
        void Require(string column, string pointer)
        {
            if (!columns.ContainsKey(column))
            {
                throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.CsvColumnMissing, mapName, column, pointer), sourceName);
            }
        }

        Require(map.GroupBy, "/groupBy");
        foreach (var entry in map.Transaction.Concat(map.Lines))
        {
            if (entry.Source.Column is { } column)
            {
                Require(column, entry.MapPointer);
            }
        }
    }

    // Card numbers are masked in place before any mapping reads a cell (ADR-0004, NFR-007), except in columns a
    // person exempted with pci.allowFields after review.
    private static Dictionary<(CsvRow Row, int Column), CardBrand> MaskCells(CsvTable table, HashSet<int> exempt)
    {
        var masked = new Dictionary<(CsvRow Row, int Column), CardBrand>();
        foreach (var row in table.Rows)
        {
            for (var i = 0; i < row.Cells.Length; i++)
            {
                if (!exempt.Contains(i) && CardNumberScrubber.TryMask(row.Cells[i], out var value, out var brand))
                {
                    row.Cells[i] = value;
                    masked[(row, i)] = brand;
                }
            }
        }

        return masked;
    }

    // pci.allowFields names canonical pointers; a CSV column is exempt when the field it feeds matches. Line fields
    // are matched as /lineItems/*/name, so a pattern needs * for the line index.
    private static HashSet<int> ExemptColumns(CsvMap map, Dictionary<string, int> columns, IReadOnlyList<string> allowFields)
    {
        var exempt = new HashSet<int>();
        if (allowFields.Count == 0)
        {
            return exempt;
        }

        var patterns = allowFields.Select(p => new PointerPattern(p)).ToList();
        var targets = map.Transaction.Select(f => (f.Source.Column, Pointer: f.Target))
            .Concat(map.Lines.Select(f => (f.Source.Column, Pointer: "/lineItems/*/" + f.Target)));
        foreach (var (column, pointer) in targets)
        {
            if (column is not null && columns.TryGetValue(column, out var index) && PointerPattern.AnyMatch(patterns, pointer))
            {
                exempt.Add(index);
            }
        }

        return exempt;
    }

    private static void IgnoredColumns(CsvMap map, CsvTable table, Json.MappingLog log)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { map.GroupBy };
        foreach (var entry in map.Transaction.Concat(map.Lines))
        {
            if (entry.Source.Column is { } column)
            {
                used.Add(column);
            }
        }

        for (var i = 0; i < table.Header.Count; i++)
        {
            if (!used.Contains(table.Header[i]))
            {
                log.Add(AdapterNoteKind.Ignored, null, null, Messages.Format(Resources.Strings.CsvIgnoredColumn, CsvTable.ColumnLetters(i + 1), table.Header[i]));
            }
        }
    }
}
