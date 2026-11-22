namespace L3Lint.Core.Input;

/// <summary>
/// Thrown when an input cannot be linted at all: malformed, too large, too deep, ambiguous or unsupported.
/// </summary>
/// <remarks>
/// Problems inside a well-formed payload are findings, not exceptions. This exception covers the cases
/// where there is no payload to point into (ADR-0037). The CLI maps it to exit code 2 and the MCP server
/// returns it as a tool error the model can correct.
/// </remarks>
public class InputException : Exception
{
    /// <summary>Initializes a new instance with no details.</summary>
    public InputException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What is wrong with the input.</param>
    public InputException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What is wrong with the input.</param>
    /// <param name="innerException">The underlying parser error.</param>
    public InputException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with full details.</summary>
    /// <param name="kind">The kind of problem.</param>
    /// <param name="message">What is wrong with the input.</param>
    /// <param name="sourceName">The file name, "-" for stdin, or another label.</param>
    /// <param name="line">1-based line of the problem, or 0 when unknown.</param>
    /// <param name="column">1-based column of the problem, or 0 when unknown.</param>
    /// <param name="innerException">The underlying parser error, if any.</param>
    public InputException(InputErrorKind kind, string message, string? sourceName, int line = 0, int column = 0, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        SourceName = sourceName;
        Line = line;
        Column = column;
    }

    /// <summary>Gets the kind of problem.</summary>
    public InputErrorKind Kind { get; } = InputErrorKind.Malformed;

    /// <summary>Gets the file name or label of the input.</summary>
    public string? SourceName { get; }

    /// <summary>Gets the 1-based line of the problem, or 0 when unknown.</summary>
    public int Line { get; }

    /// <summary>Gets the 1-based column of the problem, or 0 when unknown.</summary>
    public int Column { get; }
}
