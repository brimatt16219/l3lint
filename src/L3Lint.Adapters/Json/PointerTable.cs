using System.Globalization;

namespace L3Lint.Adapters.Json;

/// <summary>
/// The inverse of a format's routes: turns a concrete source pointer into the canonical pointer it maps to.
/// </summary>
/// <remarks>
/// The card-number scrub calls this for every matched token to tell code fields from free text and to honor
/// <c>pci.allowFields</c> (ADR-0023). Immutable and thread-safe.
/// </remarks>
internal sealed class PointerTable
{
    private const string Lines = "/lineItems";

    private readonly List<(SourceTemplate Source, string Canonical, bool IsLine)> _entries = [];

    /// <summary>Initializes a table from every shape the format accepts; earlier shapes win on overlap.</summary>
    public PointerTable(params RouteSet[] shapes)
    {
        foreach (var shape in shapes)
        {
            foreach (var route in shape.HeaderRoutes)
            {
                _entries.Add((route.Source, route.Canonical, false));
            }

            foreach (var route in shape.LineRoutes)
            {
                _entries.Add((route.Source, route.Canonical, true));
            }

            if (shape.Item is { } item)
            {
                _entries.Add((item, string.Empty, true));
            }

            if (shape.Collection is { } collection)
            {
                _entries.Add((new SourceTemplate(collection), Lines, false));
            }
        }
    }

    /// <summary>Translates a source pointer to its canonical pointer.</summary>
    /// <param name="sourcePointer">A concrete RFC 6901 pointer into the source document.</param>
    /// <returns>The canonical pointer, or <see langword="null"/> when the value maps to no canonical field.</returns>
    public string? ToCanonical(string sourcePointer)
    {
        ArgumentNullException.ThrowIfNull(sourcePointer);
        if (sourcePointer.Length == 0 || sourcePointer[0] != '/')
        {
            return null;
        }

        var segments = SourceTemplate.Split(sourcePointer);
        foreach (var (source, canonical, isLine) in _entries)
        {
            if (!source.TryMatch(segments, out var index))
            {
                continue;
            }

            if (!isLine)
            {
                return canonical;
            }

            var line = Lines + "/" + Math.Max(index, 0).ToString(CultureInfo.InvariantCulture);
            return canonical.Length == 0 ? line : line + "/" + canonical;
        }

        return null;
    }
}
