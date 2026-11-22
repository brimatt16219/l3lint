using System.Globalization;

namespace L3Lint.Core.Fields;

/// <summary>The state of one canonical field value as rules see it.</summary>
internal enum FieldValueKind
{
    /// <summary>Not sent (or JSON null).</summary>
    Absent,

    /// <summary>Sent with the wrong type; a schema finding already covers it, so checks skip it (ADR-0036).</summary>
    Invalid,

    /// <summary>A string, enumeration name or ISO date.</summary>
    Text,

    /// <summary>A decimal or integer.</summary>
    Number,

    /// <summary>A boolean.</summary>
    Boolean,

    /// <summary>A collection; <see cref="FieldValue.Count"/> holds its size.</summary>
    List,
}

/// <summary>
/// A canonical field value without boxing: text, number, boolean or collection size.
/// </summary>
internal readonly record struct FieldValue(FieldValueKind Kind, string? Text, decimal Number, bool Flag, int Count)
{
    public static FieldValue Absent { get; } = new(FieldValueKind.Absent, null, 0m, false, 0);

    public static FieldValue Invalid { get; } = new(FieldValueKind.Invalid, null, 0m, false, 0);

    public bool IsPresent => Kind is not FieldValueKind.Absent and not FieldValueKind.Invalid;

    public static FieldValue Of(string? text) => text is null ? Absent : new(FieldValueKind.Text, text, 0m, false, 0);

    public static FieldValue Of(decimal? number) => number is { } n ? new(FieldValueKind.Number, null, n, false, 0) : Absent;

    public static FieldValue Of(int? number) => number is { } n ? new(FieldValueKind.Number, null, n, false, 0) : Absent;

    public static FieldValue Of(bool? flag) => flag is { } f ? new(FieldValueKind.Boolean, null, 0m, f, 0) : Absent;

    public static FieldValue Of(DateOnly? date) => date is { } d ? Of(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : Absent;

    public static FieldValue OfList(int? count) => count is { } c ? new(FieldValueKind.List, null, 0m, false, c) : Absent;

    /// <summary>Formats the value for messages: text as is, numbers in invariant culture, booleans lowercase.</summary>
    public string? Display() => Kind switch
    {
        FieldValueKind.Text => Text,
        FieldValueKind.Number => Number.ToString(CultureInfo.InvariantCulture),
        FieldValueKind.Boolean => Flag ? "true" : "false",
        FieldValueKind.List => Count.ToString(CultureInfo.InvariantCulture),
        _ => null,
    };
}
