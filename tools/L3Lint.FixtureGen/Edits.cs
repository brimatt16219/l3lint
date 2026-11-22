using System.Globalization;
using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen;

/// <summary>One change to a canonical payload, addressed by JSON Pointer.</summary>
internal delegate void Edit(JsonObject payload);

/// <summary>Small, readable payload edits used by example and fixture definitions.</summary>
internal static class Edits
{
    public static JsonObject With(JsonObject payload, params Edit[] edits)
    {
        foreach (var edit in edits)
        {
            edit(payload);
        }

        return payload;
    }

    /// <summary>Sets a value, creating missing parent objects.</summary>
    public static Edit Set(string pointer, JsonNode? value) => payload =>
    {
        var (parent, last) = Parent(payload, pointer, create: true);
        if (parent is JsonArray array)
        {
            array[int.Parse(last, CultureInfo.InvariantCulture)] = value;
        }
        else
        {
            parent![last] = value;
        }
    };

    public static Edit Set(string pointer, string value) => Set(pointer, JsonValue.Create(value));

    public static Edit Set(string pointer, decimal value) => Set(pointer, JsonValue.Create(value));

    public static Edit Set(string pointer, bool value) => Set(pointer, JsonValue.Create(value));

    /// <summary>Removes a value when present.</summary>
    public static Edit Remove(string pointer) => payload =>
    {
        var (parent, last) = Parent(payload, pointer, create: false);
        if (parent is JsonObject obj)
        {
            obj.Remove(last);
        }
        else if (parent is JsonArray array)
        {
            array.RemoveAt(int.Parse(last, CultureInfo.InvariantCulture));
        }
    };

    /// <summary>Replaces the line items with copies built by a factory.</summary>
    public static Edit Lines(int count, Func<int, JsonObject> line) => payload =>
    {
        var lines = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            lines.Add(line(i));
        }

        payload["lineItems"] = lines;
    };

    /// <summary>A minimal line: enough for counting rules, nothing else.</summary>
    public static JsonObject MinimalLine(int index) => new()
    {
        ["sequence"] = index + 1,
        ["description"] = "Gauze pad 4x4",
        ["quantity"] = 1,
        ["unitCost"] = 0.10m,
        ["total"] = 0.10m,
    };

    /// <summary>Copies line <paramref name="from"/> over line <paramref name="to"/>, keeping the target's sequence.</summary>
    public static Edit CopyLine(int from, int to) => payload =>
    {
        var lines = (JsonArray)payload["lineItems"]!;
        var copy = (JsonObject)lines[from]!.DeepClone();
        copy["sequence"] = to + 1;
        lines[to] = copy;
    };

    private static (JsonNode? Parent, string Last) Parent(JsonObject payload, string pointer, bool create)
    {
        var parts = pointer.TrimStart('/').Split('/');
        JsonNode? current = payload;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var next = current is JsonArray array ? array[int.Parse(parts[i], CultureInfo.InvariantCulture)] : current?[parts[i]];
            if (next is null && create && current is JsonObject obj)
            {
                next = new JsonObject();
                obj[parts[i]] = next;
            }

            current = next;
        }

        return (current, parts[^1]);
    }
}
