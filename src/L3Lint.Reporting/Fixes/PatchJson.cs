using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core.Fixes;

namespace L3Lint.Reporting.Fixes;

/// <summary>
/// Writes RFC 6902 JSON Patch documents, for <c>fix --dry-run</c>, the MCP <c>suggest_fixes</c> tool and the
/// <c>patch</c> member of JSON findings.
/// </summary>
public static class PatchJson
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
    /// Renders a patch document.
    /// </summary>
    /// <param name="operations">The operations, in order.</param>
    /// <param name="indented">Whether to indent (two spaces, LF) or write one line.</param>
    /// <returns>A JSON array.</returns>
    public static string Render(IEnumerable<PatchOperation> operations, bool indented)
    {
        ArgumentNullException.ThrowIfNull(operations);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, indented ? Indented : Compact))
        {
            json.WriteStartArray();
            WriteOperations(json, operations);
            json.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Writes operations as array items; the caller writes the enclosing array.
    /// </summary>
    /// <param name="json">The writer.</param>
    /// <param name="operations">The operations.</param>
    public static void WriteOperations(Utf8JsonWriter json, IEnumerable<PatchOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(operations);
        foreach (var operation in operations)
        {
            json.WriteStartObject();
            json.WriteString("op", operation.Op);
            json.WriteString("path", operation.Path);
            if (operation.Op != "remove")
            {
                json.WritePropertyName("value");
                if (operation.Value is null)
                {
                    json.WriteNullValue();
                }
                else
                {
                    operation.Value.WriteTo(json);
                }
            }

            json.WriteEndObject();
        }
    }
}
