namespace L3Lint.Core.Input;

/// <summary>
/// The physical shape of an input document.
/// </summary>
public enum InputKind
{
    /// <summary>One JSON object (a file, stdin or one NDJSON line).</summary>
    Json,

    /// <summary>Comma-separated values with a header row, read through a column map.</summary>
    Csv,
}
