using System.Text.Json.Nodes;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Pci;

namespace L3Lint.Adapters.Tests.Support;

/// <summary>Maps fixtures and ad hoc payloads through an adapter for contract tests.</summary>
internal static class Mapped
{
    /// <summary>Maps a fixture with its declared adapter and options, after the pipeline's card-number scrub.</summary>
    public static AdapterResult Fixture(string id, bool verbose = false)
    {
        var fixture = Support.Fixture.Load(id);
        var adapter = Linters.Adapter(fixture.Format);
        var document = fixture.Document();
        if (!fixture.IsCsv)
        {
            document = CardNumberScrubber.ScrubJson(document, adapter, []).Document;
        }

        return adapter.Map(document, fixture.AdapterOptions() with { Verbose = verbose })[0];
    }

    /// <summary>Maps a JSON payload given as text.</summary>
    public static AdapterResult Json(string adapter, string json, AdapterOptions? options = null) =>
        Linters.Adapter(adapter).Map(RawDocument.ParseJson(json, "test.json"), options ?? AdapterOptions.Default)[0];

    /// <summary>Gets the source pointer a canonical pointer resolves to, or null.</summary>
    public static string? Source(this AdapterResult result, string canonical) => result.Paths.Resolve(canonical)?.Pointer;

    /// <summary>Converts a canonical value back to the source representation for a canonical pointer.</summary>
    public static JsonNode? ToSource(this AdapterResult result, string canonical, JsonNode? value)
    {
        var field = result.Paths.Resolve(canonical);
        field.ShouldNotBeNull(canonical);
        return field.Value.ToSource is { } convert ? convert(value) : value;
    }
}
