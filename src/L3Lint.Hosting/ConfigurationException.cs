namespace L3Lint.Hosting;

/// <summary>
/// Thrown when <c>.l3lint.yaml</c>, a rate table, a profile file or a lock file is invalid. The CLI exits 3 and
/// prints the file and the JSON Pointer of the problem.
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>Initializes a new instance with no details.</summary>
    public ConfigurationException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What is wrong.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The underlying error.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with the file and location.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="file">The file.</param>
    /// <param name="pointer">The JSON Pointer of the problem, if known.</param>
    /// <param name="innerException">The underlying error, if any.</param>
    public ConfigurationException(string message, string file, string? pointer, Exception? innerException = null)
        : base(message, innerException)
    {
        File = file;
        Pointer = pointer;
    }

    /// <summary>Gets the file.</summary>
    public string? File { get; }

    /// <summary>Gets the JSON Pointer of the problem.</summary>
    public string? Pointer { get; }
}
