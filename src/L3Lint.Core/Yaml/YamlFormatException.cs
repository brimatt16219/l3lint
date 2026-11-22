namespace L3Lint.Core.Yaml;

/// <summary>
/// Thrown when a YAML file cannot be read: bad syntax, several documents, a type tag or too many nodes.
/// </summary>
/// <remarks>Line and column are 1-based positions in the YAML text.</remarks>
public sealed class YamlFormatException : FormatException
{
    /// <summary>Initializes a new instance with no details.</summary>
    public YamlFormatException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What is wrong.</param>
    public YamlFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The parser error.</param>
    public YamlFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with a message and a position.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="line">1-based line.</param>
    /// <param name="column">1-based column.</param>
    /// <param name="innerException">The parser error, if any.</param>
    public YamlFormatException(string message, int line, int column, Exception? innerException)
        : base(message, innerException)
    {
        Line = line;
        Column = column;
    }

    /// <summary>Gets the 1-based line of the problem, or 0 when unknown.</summary>
    public int Line { get; }

    /// <summary>Gets the 1-based column of the problem, or 0 when unknown.</summary>
    public int Column { get; }
}
