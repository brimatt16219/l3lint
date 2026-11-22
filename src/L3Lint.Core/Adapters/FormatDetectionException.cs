using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Thrown when no adapter scores at least 0.8, two adapters score within 0.2 of each other, or a named format
/// is unknown (pipeline stage 2, FR-005). The CLI exits 2 and lists the candidates.
/// </summary>
public sealed class FormatDetectionException : InputException
{
    /// <summary>Initializes a new instance with no details.</summary>
    public FormatDetectionException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What went wrong.</param>
    public FormatDetectionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The underlying error.</param>
    public FormatDetectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with the candidates.</summary>
    /// <param name="message">What went wrong and what to do.</param>
    /// <param name="sourceName">The input.</param>
    /// <param name="candidates">The adapters that scored too close to choose, best first, as "name (score)".</param>
    public FormatDetectionException(string message, string? sourceName, IReadOnlyList<string> candidates)
        : base(InputErrorKind.UnknownFormat, message, sourceName)
    {
        Candidates = candidates;
    }

    /// <summary>Gets the candidate adapters.</summary>
    public IReadOnlyList<string> Candidates { get; } = [];
}
