using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core.Adapters;
using L3Lint.Core.Fixes;
using L3Lint.Core.Input;

namespace L3Lint.Core.Engine;

/// <summary>
/// Turns a canonical fix intent into RFC 6902 operations against the source document (ADR-0022): pointers go
/// through the path map, values through the source converter (for example to minor units), and missing parent
/// objects are added so the patch applies cleanly.
/// </summary>
internal static class PatchTranslator
{
    public static FixPatch? Translate(FixIntent intent, PathMap paths, JsonElement root, bool targetsCanonical)
    {
        var operations = new List<PatchOperation>();
        foreach (var operation in intent.Operations)
        {
            string path;
            JsonNode? value;
            if (targetsCanonical)
            {
                path = operation.Path;
                value = operation.Value?.DeepClone();
            }
            else
            {
                if (paths.Resolve(operation.Path) is not { } source || !source.Pointer.StartsWith('/'))
                {
                    return null;
                }

                path = source.Pointer;
                value = source.ToSource is { } convert ? convert(operation.Value?.DeepClone()) : operation.Value?.DeepClone();
            }

            var placed = Place(root, operation.Op, path, value);
            if (placed is null)
            {
                return null;
            }

            operations.Add(placed);
        }

        return operations.Count == 0 ? null : new FixPatch(operations, targetsCanonical);
    }

    // For "add", walk to the deepest existing ancestor; missing object levels are created in one nested add.
    private static PatchOperation? Place(JsonElement root, string op, string path, JsonNode? value)
    {
        if (op != "add")
        {
            return new PatchOperation(op, path, value);
        }

        var segments = JsonPointerText.Split(path);
        var current = root;
        var depth = 0;
        for (; depth < segments.Length - 1; depth++)
        {
            if (!TryStep(current, segments[depth], out var next))
            {
                break;
            }

            current = next;
        }

        if (depth == segments.Length - 1)
        {
            return current.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? new PatchOperation("add", path, value) : null;
        }

        if (current.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // segments[depth] is the first missing level. Missing levels must be object members: array indexes
        // cannot be invented, so such a fix is left to a person.
        for (var i = depth; i < segments.Length - 1; i++)
        {
            if (segments[i].Length > 0 && segments[i].All(char.IsAsciiDigit))
            {
                return null;
            }
        }

        var nested = value;
        for (var i = segments.Length - 1; i > depth; i--)
        {
            nested = new JsonObject { [segments[i]] = nested };
        }

        return new PatchOperation("add", JsonPointerText.Join(segments.Take(depth + 1)), nested);
    }

    private static bool TryStep(JsonElement element, string segment, out JsonElement next)
    {
        next = default;
        if (element.ValueKind == JsonValueKind.Object)
        {
            return element.TryGetProperty(segment, out next);
        }

        if (element.ValueKind == JsonValueKind.Array && int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < element.GetArrayLength())
        {
            next = element[index];
            return true;
        }

        return false;
    }
}
