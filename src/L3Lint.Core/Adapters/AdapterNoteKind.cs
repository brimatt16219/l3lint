namespace L3Lint.Core.Adapters;

/// <summary>
/// The kind of an <see cref="AdapterNote"/>.
/// </summary>
public enum AdapterNoteKind
{
    /// <summary>A source field has no canonical equivalent and was ignored. Shown with <c>--verbose</c>.</summary>
    Ignored,

    /// <summary>A source field's meaning is ambiguous; it was mapped as described.</summary>
    Ambiguous,

    /// <summary>A value was computed or filled by the adapter, not read from the source.</summary>
    Derived,

    /// <summary>A source value could not be converted; it becomes a GEN-FMT-001 finding.</summary>
    Invalid,

    /// <summary>A value came from options or configuration because the format does not carry it.</summary>
    Defaulted,
}
