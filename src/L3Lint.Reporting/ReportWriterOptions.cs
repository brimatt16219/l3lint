namespace L3Lint.Reporting;

/// <summary>
/// Options shared by the output writers.
/// </summary>
public sealed record ReportWriterOptions
{
    /// <summary>Gets the default options: no color.</summary>
    public static ReportWriterOptions Default { get; } = new();

    /// <summary>Gets a value indicating whether the text writer may use ANSI color. Severity words always print (NFR-010).</summary>
    public bool Color { get; init; }

    /// <summary>Gets a value indicating whether to print notes about context overrides and adapters.</summary>
    public bool Verbose { get; init; }

    /// <summary>Gets a value indicating whether to print only the summary line in text output.</summary>
    public bool Quiet { get; init; }

    /// <summary>Gets the folder input paths are made relative to in SARIF (the repository root); <see langword="null"/> keeps them as given.</summary>
    public string? BaseDirectory { get; init; }
}
