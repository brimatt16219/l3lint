using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Lists source values that map to no canonical field, as <see cref="AdapterNoteKind.Ignored"/> notes. Runs only
/// with <c>--verbose</c>, so normal runs pay nothing for it.
/// </summary>
internal static class IgnoredFields
{
    /// <summary>Adds an Ignored note for every leaf value whose pointer has no canonical equivalent.</summary>
    public static void Report(JsonElement root, Func<string, string?> toCanonical, MappingLog log) =>
        Visit(root, string.Empty, toCanonical, log);

    private static void Visit(JsonElement element, string pointer, Func<string, string?> toCanonical, MappingLog log)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Visit(property.Value, JsonPointerText.Append(pointer, property.Name), toCanonical, log);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Visit(item, pointer + "/" + index.ToString(CultureInfo.InvariantCulture), toCanonical, log);
                    index++;
                }

                break;
            default:
                if (toCanonical(pointer) is null)
                {
                    log.Add(AdapterNoteKind.Ignored, pointer, null, Messages.Format(Resources.Strings.NoteIgnoredField, pointer));
                }

                break;
        }
    }
}
