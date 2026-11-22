using L3Lint.Core;

namespace L3Lint.Hosting;

/// <summary>
/// Everything a run needs after config, flags and referenced files are combined.
/// </summary>
public sealed record ResolvedSettings
{
    /// <summary>Gets the lint options.</summary>
    public required LintOptions Options { get; init; }

    /// <summary>Gets the requested outputs; text to standard output when none were requested.</summary>
    public IReadOnlyList<OutputTarget> Outputs { get; init; } = [new OutputTarget("text", null)];

    /// <summary>Gets the most findings to print.</summary>
    public int? MaxFindings { get; init; }

    /// <summary>Gets the processor profile ID.</summary>
    public string ProfileName { get; init; } = "generic";

    /// <summary>Gets the configuration hash for the SARIF category.</summary>
    public string ConfigHash { get; init; } = "default";

    /// <summary>Gets local rule pack folders to load.</summary>
    public IReadOnlyList<string> LocalPacks { get; init; } = [];

    /// <summary>Gets the lock file to enforce, when one exists.</summary>
    public string? LockFile { get; init; }

    /// <summary>Gets notes about where settings came from.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];
}
