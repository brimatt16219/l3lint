namespace L3Lint.Core.Adapters;

/// <summary>
/// Builds an immutable <see cref="PathMap"/>.
/// </summary>
/// <remarks>Not thread-safe; build one map per document.</remarks>
public sealed class PathMapBuilder
{
    private readonly Dictionary<string, SourceField?> _fields = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Func<int, SourceField?>> _lineFields = new(StringComparer.Ordinal);
    private SourceField? _lineItems;
    private Func<int, SourceField?>? _lineItem;
    private SourceField? _adviceText;
    private Func<int, SourceField?>? _adviceLine;

    /// <summary>
    /// Maps a transaction-level canonical pointer.
    /// </summary>
    /// <param name="canonicalPointer">For example "/tax/amount".</param>
    /// <param name="source">The source field, or <see langword="null"/> when the format cannot carry this field.</param>
    /// <returns>This builder.</returns>
    public PathMapBuilder Field(string canonicalPointer, SourceField? source)
    {
        ArgumentNullException.ThrowIfNull(canonicalPointer);
        _fields[canonicalPointer] = source;
        return this;
    }

    /// <summary>
    /// Maps the line-items collection and each line object.
    /// </summary>
    /// <param name="collection">The source collection, or <see langword="null"/> when the format has none.</param>
    /// <param name="item">Gives the source location of the line at a 0-based index.</param>
    /// <returns>This builder.</returns>
    public PathMapBuilder LineItems(SourceField? collection, Func<int, SourceField?> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _lineItems = collection;
        _lineItem = item;
        return this;
    }

    /// <summary>
    /// Maps one line-level field.
    /// </summary>
    /// <param name="fieldName">The canonical line field name, for example "unitCost".</param>
    /// <param name="source">Gives the source location for the line at a 0-based index, or <see langword="null"/> when unsupported.</param>
    /// <returns>This builder.</returns>
    public PathMapBuilder LineField(string fieldName, Func<int, SourceField?> source)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(source);
        _lineFields[fieldName] = source;
        return this;
    }

    /// <summary>
    /// Maps the Amex advice-text array and its lines.
    /// </summary>
    /// <param name="collection">The source array.</param>
    /// <param name="line">Gives the source location of the line at a 0-based index.</param>
    /// <returns>This builder.</returns>
    public PathMapBuilder AdviceText(SourceField? collection, Func<int, SourceField?> line)
    {
        ArgumentNullException.ThrowIfNull(line);
        _adviceText = collection;
        _adviceLine = line;
        return this;
    }

    /// <summary>Builds the immutable map.</summary>
    /// <returns>The path map.</returns>
    public PathMap Build() => new(_fields, _lineFields, _lineItems, _lineItem, _adviceText, _adviceLine, identity: false);
}
