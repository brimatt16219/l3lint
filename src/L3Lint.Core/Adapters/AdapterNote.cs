namespace L3Lint.Core.Adapters;

/// <summary>
/// Something an adapter wants the user to know about the mapping.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="SourcePointer">Where in the source format, or <see langword="null"/> when not tied to one value.</param>
/// <param name="CanonicalPointer">The canonical field involved, or <see langword="null"/>.</param>
/// <param name="Message">A plain-language explanation.</param>
public sealed record AdapterNote(AdapterNoteKind Kind, string? SourcePointer, string? CanonicalPointer, string Message);
