namespace L3Lint.Adapters.Csv;

/// <summary>
/// A parsed CSV column map (FR-006, US-14): which column groups rows into transactions and where each canonical
/// field comes from.
/// </summary>
/// <param name="Delimiter">The field delimiter, a comma unless the map says otherwise.</param>
/// <param name="GroupBy">The column whose value identifies the transaction, for example the invoice number.</param>
/// <param name="MinorUnits">Whether amount columns hold minor units (1250) instead of major units (12.50).</param>
/// <param name="Transaction">Transaction-level fields in map order.</param>
/// <param name="Lines">Line-level fields in map order.</param>
internal sealed record CsvMap(char Delimiter, string GroupBy, bool MinorUnits, IReadOnlyList<CsvFieldMap> Transaction, IReadOnlyList<CsvFieldMap> Lines);

/// <summary>One canonical field and its source in a CSV map.</summary>
/// <param name="Target">A canonical pointer ("/tax/amount") for transaction fields, or a line field name ("unitCost").</param>
/// <param name="MapPointer">The JSON Pointer of this entry inside the map file, for messages.</param>
/// <param name="Source">Where the value comes from.</param>
internal sealed record CsvFieldMap(string Target, string MapPointer, CsvSource Source);

/// <summary>
/// The source of one mapped value: a column, a constant, or a column with a value map and a default. Constants,
/// map values and defaults are kept as text and converted like cell text.
/// </summary>
/// <param name="Column">The column name, or <see langword="null"/> for a constant.</param>
/// <param name="Constant">The constant value as text, when the entry is <c>{ const: ... }</c>.</param>
/// <param name="Values">The value map (<c>map:</c>), or <see langword="null"/>.</param>
/// <param name="Default">The value used when the cell is empty, or <see langword="null"/>.</param>
internal sealed record CsvSource(string? Column, string? Constant, IReadOnlyDictionary<string, string>? Values, string? Default);
