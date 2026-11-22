using L3Lint.Core;

namespace L3Lint.Hosting.Runs;

/// <summary>
/// What to lint and how, for <see cref="LintRunner"/>.
/// </summary>
public sealed record LintRunRequest
{
    /// <summary>Gets the inputs: files, folders, globs, or "-" for standard input.</summary>
    public required IReadOnlyList<string> Inputs { get; init; }

    /// <summary>Gets the lint options.</summary>
    public LintOptions Options { get; init; } = LintOptions.Default;

    /// <summary>Gets the folder relative inputs resolve from.</summary>
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Gets a function that opens standard input, for the "-" input.</summary>
    public Func<Stream>? StandardInput { get; init; }

    /// <summary>Gets the most findings to keep in the report; the summary still counts all (US-04).</summary>
    public int? MaxFindings { get; init; }

    /// <summary>Gets the number of worker threads; default <see cref="Environment.ProcessorCount"/>.</summary>
    public int Parallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>Gets the configuration hash for the SARIF category.</summary>
    public string ConfigHash { get; init; } = "default";

    /// <summary>Gets run-level notes to include in the report.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Gets the processor profile name for the report header.</summary>
    public string Profile { get; init; } = "generic";

    /// <summary>Gets the number of results per compressed block of the report's transaction store; tests lower it.</summary>
    internal int ResultBlockSize { get; init; } = TransactionResultStore.DefaultBlockSize;
}
