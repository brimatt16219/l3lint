namespace L3Lint.Core.Rules;

/// <summary>
/// Thrown when a rule pack, profile or lock file is invalid: schema error, checksum mismatch, unknown field,
/// unknown check, bad regex or version mismatch (FR-019).
/// </summary>
/// <remarks>The CLI maps this exception to exit code 3 and prints the file and path.</remarks>
public sealed class RulePackException : Exception
{
    /// <summary>Initializes a new instance with no details.</summary>
    public RulePackException()
    {
    }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">What is wrong.</param>
    public RulePackException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and an inner exception.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The underlying error.</param>
    public RulePackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance with the pack, file and location.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="pack">The pack ID or folder.</param>
    /// <param name="file">The file within the pack, if known.</param>
    /// <param name="pointer">A JSON Pointer within the file, if known.</param>
    /// <param name="innerException">The underlying error, if any.</param>
    public RulePackException(string message, string? pack, string? file, string? pointer, Exception? innerException = null)
        : base(message, innerException)
    {
        Pack = pack;
        File = file;
        Pointer = pointer;
    }

    /// <summary>Gets the pack ID or folder.</summary>
    public string? Pack { get; }

    /// <summary>Gets the file within the pack.</summary>
    public string? File { get; }

    /// <summary>Gets the JSON Pointer within the file.</summary>
    public string? Pointer { get; }
}
