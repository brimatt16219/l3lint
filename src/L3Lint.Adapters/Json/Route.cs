namespace L3Lint.Adapters.Json;

/// <summary>
/// Where one canonical field lives in a gateway format and how it is written there.
/// </summary>
/// <param name="Canonical">A header pointer such as "/tax/amount", or a line field name such as "unitCost".</param>
/// <param name="Source">The source pointer template; line templates hold a "#" for the line key.</param>
/// <param name="Shape">The JSON type and units the gateway uses.</param>
internal sealed record Route(string Canonical, SourceTemplate Source, Shape Shape)
{
    /// <summary>Creates a transaction-level route.</summary>
    public static Route Header(string canonical, string source, Shape shape) => new(canonical, new SourceTemplate(source), shape);

    /// <summary>Creates a line-level route.</summary>
    public static Route Line(string field, string source, Shape shape, int keyBase = 0) => new(field, new SourceTemplate(source, keyBase), shape);
}
