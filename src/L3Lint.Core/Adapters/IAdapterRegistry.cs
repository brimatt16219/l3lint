using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// The adapters known to a linter, and format detection over them (FR-005).
/// </summary>
public interface IAdapterRegistry
{
    /// <summary>Gets the adapters, including the built-in canonical adapter.</summary>
    IReadOnlyList<IPayloadAdapter> Adapters { get; }

    /// <summary>
    /// Scores every adapter that reads the document's kind, best first.
    /// </summary>
    /// <param name="document">The input.</param>
    /// <returns>The scores, ordered by score then name.</returns>
    IReadOnlyList<DetectionScore> Score(RawDocument document);

    /// <summary>
    /// Picks the adapter for a document.
    /// </summary>
    /// <param name="format">A format name, or <see langword="null"/> or "auto" to detect.</param>
    /// <param name="document">The input.</param>
    /// <returns>The adapter.</returns>
    /// <exception cref="FormatDetectionException">The name is unknown, or detection found no winner (none at least 0.8, or two within 0.2).</exception>
    IPayloadAdapter Resolve(string? format, RawDocument document);
}
