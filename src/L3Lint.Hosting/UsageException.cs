namespace L3Lint.Hosting;

/// <summary>
/// Thrown for a usage error: an unknown option value, an unreadable file, a missing column map. The CLI prints one
/// line plus a hint and exits 2.
/// </summary>
public sealed class UsageException : Exception
{
    /// <summary>Initializes a new instance with no details.</summary>
    public UsageException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What is wrong and how to fix it.</param>
    public UsageException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What is wrong and how to fix it.</param>
    /// <param name="innerException">The underlying error.</param>
    public UsageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
