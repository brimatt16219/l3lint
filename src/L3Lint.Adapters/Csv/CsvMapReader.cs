using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core.Input;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Adapters.Csv;

/// <summary>
/// Reads a YAML CSV column map and validates it against <c>csvmap.schema.json</c>. Any problem is an input error
/// that names the map file and the JSON Pointer of the bad entry, because without a valid map no row can be read.
/// </summary>
internal static class CsvMapReader
{
    /// <summary>Parses and validates a map.</summary>
    /// <param name="yaml">The map text.</param>
    /// <param name="mapName">The map file name for messages.</param>
    /// <param name="sourceName">The CSV input name for the exception.</param>
    /// <returns>The map.</returns>
    /// <exception cref="InputException">The map is not valid YAML or does not match the schema.</exception>
    public static CsvMap Read(string yaml, string mapName, string sourceName)
    {
        JsonNode? node;
        try
        {
            node = YamlJson.Parse(yaml);
        }
        catch (YamlFormatException ex)
        {
            throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.CsvMapInvalidYaml, mapName, ex.Line, ex.Column, ex.Message), sourceName, 0, 0, ex);
        }

        if (node is not JsonObject root)
        {
            if (node is null)
            {
                throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.CsvMapEmpty, mapName), sourceName);
            }

            root = [];
        }

        var errors = SchemaValidator.For("csvmap").Validate(JsonElementFromYaml.Convert(node));
        if (errors.Count > 0)
        {
            throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.CsvMapInvalid, mapName, SchemaMessages.Describe(errors[0])), sourceName);
        }

        var delimiter = root["delimiter"]?.GetValue<string>() is { Length: 1 } text ? text[0] : ',';
        var minor = string.Equals(root["amountUnits"]?.GetValue<string>(), "minor", StringComparison.Ordinal);
        return new CsvMap(
            delimiter,
            root["groupBy"]!.GetValue<string>(),
            minor,
            Fields(root["transaction"]!.AsObject(), "/transaction", static key => "/" + key.Replace('.', '/')),
            Fields(root["lineItems"]!.AsObject(), "/lineItems", static key => key));
    }

    private static List<CsvFieldMap> Fields(JsonObject entries, string at, Func<string, string> target)
    {
        var fields = new List<CsvFieldMap>(entries.Count);
        foreach (var (key, value) in entries)
        {
            fields.Add(new CsvFieldMap(target(key), at + "/" + key, Source(value!)));
        }

        return fields;
    }

    // The schema has already checked the three allowed forms, so each branch can read without further checks.
    private static CsvSource Source(JsonNode value)
    {
        if (value is JsonValue column)
        {
            return new CsvSource(column.GetValue<string>(), null, null, null);
        }

        var entry = value.AsObject();
        if (entry.TryGetPropertyValue("const", out var constant))
        {
            return new CsvSource(null, Scalar(constant), null, null);
        }

        Dictionary<string, string>? values = null;
        if (entry["map"] is JsonObject map)
        {
            values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (from, to) in map)
            {
                values[from] = Scalar(to) ?? string.Empty;
            }
        }

        return new CsvSource(entry["from"]!.GetValue<string>(), null, values, entry.TryGetPropertyValue("default", out var fallback) ? Scalar(fallback) : null);
    }

    // YAML scalars arrive as JSON strings, numbers or booleans; all become text and convert like cell text.
    private static string? Scalar(JsonNode? node) => node switch
    {
        null => null,
        JsonValue value when value.GetValueKind() == JsonValueKind.String => value.GetValue<string>(),
        JsonValue value when value.GetValueKind() == JsonValueKind.True => "true",
        JsonValue value when value.GetValueKind() == JsonValueKind.False => "false",
        JsonValue value => decimal.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : value.ToJsonString(),
        _ => node.ToJsonString(),
    };
}
