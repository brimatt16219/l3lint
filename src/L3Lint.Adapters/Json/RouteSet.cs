using System.Collections.Frozen;

namespace L3Lint.Adapters.Json;

/// <summary>
/// The routes of one shape of a gateway format: transaction fields, the line collection and line fields.
/// </summary>
/// <remarks>Immutable and shared by all documents of that shape.</remarks>
internal sealed class RouteSet
{
    private readonly FrozenDictionary<string, Route> _header;
    private readonly FrozenDictionary<string, Route> _lines;

    /// <summary>Initializes a new route set.</summary>
    /// <param name="collection">Pointer to the line array, or <see langword="null"/> when lines are not an array.</param>
    /// <param name="item">Template of one line object, or <see langword="null"/> when lines are not objects.</param>
    /// <param name="header">Transaction-level routes.</param>
    /// <param name="lines">Line-level routes keyed by canonical line field name.</param>
    public RouteSet(string? collection, SourceTemplate? item, IEnumerable<Route> header, IEnumerable<Route> lines)
    {
        Collection = collection;
        Item = item;
        _header = header.ToFrozenDictionary(r => r.Canonical, StringComparer.Ordinal);
        _lines = lines.ToFrozenDictionary(r => r.Canonical, StringComparer.Ordinal);
    }

    /// <summary>Gets the pointer of the line array, or <see langword="null"/>.</summary>
    public string? Collection { get; }

    /// <summary>Gets the template of one line object, or <see langword="null"/>.</summary>
    public SourceTemplate? Item { get; }

    /// <summary>Gets every transaction-level route.</summary>
    public IEnumerable<Route> HeaderRoutes => _header.Values;

    /// <summary>Gets every line-level route.</summary>
    public IEnumerable<Route> LineRoutes => _lines.Values;

    /// <summary>Finds the route of a transaction-level canonical pointer.</summary>
    public Route? Header(string canonical) => _header.GetValueOrDefault(canonical);

    /// <summary>Finds the route of a canonical line field name.</summary>
    public Route? Line(string field) => _lines.GetValueOrDefault(field);
}
