using System.Text.Json;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Json;

/// <summary>Cheap shape probes for format detection (FR-005). None of them reads more than two levels deep.</summary>
internal static class Probe
{
    /// <summary>
    /// Gets the root when a gateway adapter may score the document: JSON, an object, and not canonical. Canonical
    /// payloads carry <c>schemaVersion</c>, which no gateway sends, so gateways never claim them.
    /// </summary>
    public static bool TryRoot(RawDocument document, out JsonElement root)
    {
        ArgumentNullException.ThrowIfNull(document);
        root = default;
        if (document.Kind != InputKind.Json || document.Json.ValueKind != JsonValueKind.Object || document.Json.TryGetProperty("schemaVersion", out _))
        {
            return false;
        }

        root = document.Json;
        return true;
    }

    /// <summary>Tells whether an object has a property of the given kind.</summary>
    public static bool Has(JsonElement parent, string name, JsonValueKind kind) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == kind;

    /// <summary>Tells whether an object has a property of any kind other than null.</summary>
    public static bool Has(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null;

    /// <summary>Gets a child object, or <see langword="default"/> when absent or not an object.</summary>
    public static JsonElement Child(JsonElement parent, string name) =>
        parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : default;

    /// <summary>Tells whether a property is a JSON integer.</summary>
    public static bool IsInteger(JsonElement parent, string name) =>
        Has(parent, name, JsonValueKind.Number) && parent.GetProperty(name).TryGetInt64(out _);

    /// <summary>Tells whether the first element of an array property is an object with any of the given keys.</summary>
    public static bool FirstItemHasAny(JsonElement parent, string name, params string[] keys)
    {
        if (!Has(parent, name, JsonValueKind.Array))
        {
            return false;
        }

        foreach (var item in parent.GetProperty(name).EnumerateArray())
        {
            return item.ValueKind == JsonValueKind.Object && keys.Any(k => item.TryGetProperty(k, out _));
        }

        return false;
    }
}
