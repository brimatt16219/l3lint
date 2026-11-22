using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Fields;
using L3Lint.Core.Model;

namespace L3Lint.Reporting.Mapping;

/// <summary>
/// Writes the canonical form of a document with its path map, derived fields and adapter notes, for
/// <c>convert --to canonical</c> and the MCP <c>map_payload</c> tool (US-02, FR-007).
/// </summary>
/// <remarks>
/// The path map lists every canonical leaf that has a source equivalent, in pointer order, so output is
/// byte-identical for identical input (NFR-001). Card numbers are already masked in the canonical values.
/// </remarks>
public static class MappingJson
{
    private static readonly JsonWriterOptions Indented = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonWriterOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Renders a mapping result.
    /// </summary>
    /// <param name="result">The mapping result.</param>
    /// <param name="indented">Whether to indent (two spaces, LF) or write one line, for NDJSON.</param>
    /// <returns>The JSON text.</returns>
    public static string Render(MapResult result, bool indented)
    {
        ArgumentNullException.ThrowIfNull(result);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, indented ? Indented : Compact))
        {
            json.WriteStartObject();
            json.WriteString("format", result.Format);
            json.WriteNumber("cardNumbersMasked", result.CardNumbers.Count);
            json.WriteStartArray("transactions");
            for (var i = 0; i < result.Transactions.Count; i++)
            {
                WriteTransaction(json, i, result.Transactions[i]);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteTransaction(Utf8JsonWriter json, int index, AdapterResult mapped)
    {
        var canonical = CanonicalJson.SerializeToElement(mapped.Transaction);
        json.WriteStartObject();
        json.WriteNumber("index", index);
        if (mapped.FirstLine is { } line)
        {
            json.WriteNumber("line", line);
        }

        json.WritePropertyName("canonical");
        canonical.WriteTo(json);

        json.WriteStartObject("pathMap");
        var leaves = new List<string>();
        CollectLeaves(canonical, string.Empty, leaves);
        foreach (var pointer in leaves.Order(PointerComparer.Instance))
        {
            if (mapped.Paths.Resolve(pointer) is { } source)
            {
                json.WriteString(pointer, source.Pointer);
            }
        }

        json.WriteEndObject();

        json.WriteStartArray("derivedFields");
        foreach (var field in mapped.DerivedFields.Order(StringComparer.Ordinal))
        {
            json.WriteStringValue(field);
        }

        json.WriteEndArray();

        json.WriteStartArray("notes");
        foreach (var note in mapped.Notes)
        {
            json.WriteStartObject();
            json.WriteString("kind", WireNames.Of(note.Kind));
            WriteOptional(json, "source", note.SourcePointer);
            WriteOptional(json, "canonical", note.CanonicalPointer);
            json.WriteString("message", note.Message);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void CollectLeaves(JsonElement element, string pointer, List<string> leaves)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CollectLeaves(property.Value, pointer + "/" + property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal), leaves);
                }

                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    CollectLeaves(item, pointer + "/" + i.ToString(CultureInfo.InvariantCulture), leaves);
                    i++;
                }

                break;
            default:
                leaves.Add(pointer);
                break;
        }
    }

    private static void WriteOptional(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null)
        {
            json.WriteNull(name);
        }
        else
        {
            json.WriteString(name, value);
        }
    }
}
