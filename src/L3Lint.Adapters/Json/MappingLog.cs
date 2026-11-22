using L3Lint.Core.Adapters;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Collects notes, invalid pointers and derived templates while one transaction is mapped.
/// </summary>
/// <remarks>Not thread-safe; one log per transaction. Adapters create it inside <c>Map</c>, so they stay stateless.</remarks>
internal sealed class MappingLog
{
    private readonly List<AdapterNote> _notes = [];
    private readonly HashSet<string> _invalid = new(StringComparer.Ordinal);
    private readonly HashSet<string> _derived = new(StringComparer.Ordinal);

    /// <summary>Gets the notes in the order they were added.</summary>
    public IReadOnlyList<AdapterNote> Notes => _notes;

    /// <summary>Gets the concrete canonical pointers whose source values could not be converted (ADR-0036).</summary>
    public IReadOnlySet<string> Invalid => _invalid;

    /// <summary>Gets the canonical pointer templates the adapter computed instead of reading.</summary>
    public IReadOnlySet<string> Derived => _derived;

    /// <summary>Records a value that could not be converted; the engine reports it as GEN-FMT-001.</summary>
    public void AddInvalid(string? sourcePointer, string canonicalPointer, string message)
    {
        _notes.Add(new AdapterNote(AdapterNoteKind.Invalid, sourcePointer, canonicalPointer, message));
        _invalid.Add(canonicalPointer);
    }

    /// <summary>Marks a canonical pointer invalid without another note, when one note already covers it.</summary>
    public void MarkInvalid(string canonicalPointer) => _invalid.Add(canonicalPointer);

    /// <summary>Tells whether a canonical pointer, or an object or array that contains it, was marked invalid.</summary>
    public bool IsInvalid(string canonicalPointer)
    {
        var current = canonicalPointer;
        while (current.Length > 0)
        {
            if (_invalid.Contains(current))
            {
                return true;
            }

            var slash = current.LastIndexOf('/');
            current = slash <= 0 ? string.Empty : current[..slash];
        }

        return false;
    }

    /// <summary>Records a computed value and its template, for example "/lineItems/*/total".</summary>
    public void AddDerived(string template, string? sourcePointer, string message)
    {
        if (_derived.Add(template))
        {
            _notes.Add(new AdapterNote(AdapterNoteKind.Derived, sourcePointer, template, message));
        }
    }

    /// <summary>Adds a note of any kind other than <see cref="AdapterNoteKind.Invalid"/>.</summary>
    public void Add(AdapterNoteKind kind, string? sourcePointer, string? canonicalPointer, string message) =>
        _notes.Add(new AdapterNote(kind, sourcePointer, canonicalPointer, message));
}
