using System.Globalization;
using System.Text.Json.Nodes;
using L3Lint.Core.Input;

namespace L3Lint.Core.Fixes;

/// <summary>
/// Applies RFC 6902 <c>add</c>, <c>replace</c> and <c>remove</c> operations to a JSON tree (FR-027).
/// </summary>
public static class JsonPatch
{
    /// <summary>
    /// Applies operations to a copy of a document.
    /// </summary>
    /// <param name="document">The document; it is not changed.</param>
    /// <param name="operations">The operations, applied in order.</param>
    /// <returns>The patched copy.</returns>
    /// <exception cref="ArgumentException">An operation is not add, replace or remove, or its path does not exist where RFC 6902 requires it to.</exception>
    public static JsonNode Apply(JsonNode document, IEnumerable<PatchOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(operations);
        var root = document.DeepClone();
        foreach (var operation in operations)
        {
            root = ApplyOne(root, operation);
        }

        return root;
    }

    private static JsonNode ApplyOne(JsonNode root, PatchOperation operation)
    {
        var segments = JsonPointerText.Split(operation.Path);
        if (segments.Length == 0)
        {
            return operation.Op is "add" or "replace" && operation.Value is not null
                ? operation.Value.DeepClone()
                : throw new ArgumentException(Message(operation), nameof(operation));
        }

        var parent = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            parent = Child(parent, segments[i]) ?? throw new ArgumentException(Message(operation), nameof(operation));
        }

        var last = segments[^1];
        switch (operation.Op, parent)
        {
            case ("add" or "replace", JsonObject obj):
                if (operation.Op == "replace" && !obj.ContainsKey(last))
                {
                    throw new ArgumentException(Message(operation), nameof(operation));
                }

                obj[last] = operation.Value?.DeepClone();
                break;
            case ("add", JsonArray array):
                var index = last == "-" ? array.Count : Index(last, array.Count, operation);
                array.Insert(index, operation.Value?.DeepClone());
                break;
            case ("replace", JsonArray array):
                array[Index(last, array.Count - 1, operation)] = operation.Value?.DeepClone();
                break;
            case ("remove", JsonObject obj):
                if (!obj.Remove(last))
                {
                    throw new ArgumentException(Message(operation), nameof(operation));
                }

                break;
            case ("remove", JsonArray array):
                array.RemoveAt(Index(last, array.Count - 1, operation));
                break;
            default:
                throw new ArgumentException(Message(operation), nameof(operation));
        }

        return root;
    }

    private static JsonNode? Child(JsonNode node, string segment) => node switch
    {
        JsonObject obj => obj[segment],
        JsonArray array when int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var i) && i < array.Count => array[i],
        _ => null,
    };

    private static int Index(string segment, int max, PatchOperation operation) =>
        int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index <= max
            ? index
            : throw new ArgumentException(Message(operation), nameof(operation));

    private static string Message(PatchOperation operation) =>
        string.Format(CultureInfo.InvariantCulture, Resources.Strings.PatchCannotApply, operation.Op, operation.Path);
}
