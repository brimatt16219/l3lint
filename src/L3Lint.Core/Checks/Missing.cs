using L3Lint.Core.Fields;

namespace L3Lint.Core.Checks;

/// <summary>The single definition of "missing" for required fields (ADR-0013).</summary>
internal static class Missing
{
    /// <summary>
    /// Null, absent, empty or whitespace text, an unknown-sentinel enum value, an empty collection, and (unless
    /// allowed) a numeric zero count as missing. Negative numbers count as missing when positiveOnly is set.
    /// </summary>
    public static bool Is(FieldValue value, CanonicalField field, bool allowZero, bool positiveOnly) => value.Kind switch
    {
        FieldValueKind.Absent => true,
        FieldValueKind.Invalid => false,
        FieldValueKind.Text => string.IsNullOrWhiteSpace(value.Text) || (field.MissingSentinel is not null && value.Text == field.MissingSentinel),
        FieldValueKind.Number => (positiveOnly && value.Number <= 0m) || (!allowZero && value.Number == 0m),
        FieldValueKind.List => value.Count == 0,
        _ => false,
    };
}
