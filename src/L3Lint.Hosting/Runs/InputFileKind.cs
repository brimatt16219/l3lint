namespace L3Lint.Hosting.Runs;

/// <summary>
/// How an <see cref="InputFile"/> was split into documents.
/// </summary>
public enum InputFileKind
{
    /// <summary>One JSON document.</summary>
    Json,

    /// <summary>One JSON document per non-blank line.</summary>
    Ndjson,

    /// <summary>One CSV document holding every row.</summary>
    Csv,
}
