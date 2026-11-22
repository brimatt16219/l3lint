namespace L3Lint.FixtureGen;

/// <summary>
/// Writes one passing Level 3 payload per brand for the MCP server's <c>l3lint://examples/{brand}/{name}</c>
/// resource. Each sample is the brand's base payload, which meets every documented requirement as of 2026-10-19.
/// </summary>
internal static class SampleWriter
{
    public static readonly string[] Brands = ["visa", "mastercard", "amex", "discover"];

    public static void Write(FileSetWriter writer)
    {
        foreach (var brand in Brands)
        {
            writer.WriteJson($"src/L3Lint.Mcp/Examples/{brand}/level3.json", Bases.ForBrand(brand));
        }
    }
}
