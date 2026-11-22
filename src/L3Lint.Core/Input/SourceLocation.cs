namespace L3Lint.Core.Input;

/// <summary>
/// A 1-based line and column in the original input file.
/// </summary>
/// <param name="Line">1-based line number in the file (for NDJSON, counted from the start of the file).</param>
/// <param name="Column">1-based column in UTF-16 code units, as SARIF expects by default.</param>
public readonly record struct SourceLocation(int Line, int Column);
