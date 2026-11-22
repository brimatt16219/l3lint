using System.Text.Json;

namespace L3Lint.Adapters.Json;

/// <summary>
/// How many line items a document has and how a 0-based line index becomes a key in source pointers.
/// </summary>
/// <param name="Count">The number of lines.</param>
/// <param name="Key">Turns a 0-based index into the key that replaces "#" (the index itself for arrays).</param>
internal sealed record LinePlan(int Count, Func<int, int> Key)
{
    /// <summary>Gets a plan with no lines whose keys are array indexes.</summary>
    public static LinePlan None { get; } = new(0, static i => i);

    /// <summary>
    /// Counts the lines of an array collection. A collection of the wrong type is an Invalid note on
    /// <c>/lineItems</c>, so line rules skip instead of reporting every line as missing (ADR-0036).
    /// </summary>
    public static LinePlan FromArray(PayloadReader reader, RouteSet routes)
    {
        if (routes.Collection is not { } collection)
        {
            return None;
        }

        switch (reader.Find(collection, out var value, out var brokenAt))
        {
            case PayloadReader.Lookup.Found when value.ValueKind == JsonValueKind.Array:
                return new LinePlan(value.GetArrayLength(), static i => i);
            case PayloadReader.Lookup.Found:
                reader.Log.AddInvalid(collection, LineItemsPointer, Messages.Format(Resources.Strings.InvalidValue, collection, Resources.Strings.ExpectArray));
                return None;
            case PayloadReader.Lookup.Broken:
                reader.ReportBroken(brokenAt!, LineItemsPointer);
                return None;
            default:
                return None;
        }
    }

    /// <summary>The canonical pointer of the line collection.</summary>
    public const string LineItemsPointer = "/lineItems";
}
