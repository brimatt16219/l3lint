using L3Lint.Core.Tiers;

namespace L3Lint.Core.Rules;

/// <summary>
/// Supplies one rule pack's files and the compiled checks it may name.
/// </summary>
/// <remarks>
/// A pack is a folder with exactly one <c>*.rules.yaml</c> file, its CSV lists, its examples and a
/// <c>manifest.json</c> of SHA-256 hashes. The loader validates the YAML against
/// <c>rulepack.schema.json</c> and every hash against the manifest, and refuses the pack on any mismatch
/// (FR-019, ADR-0024).
/// </remarks>
public interface IRulePackSource
{
    /// <summary>Gets "embedded" for packs inside an assembly, or the folder of a local pack.</summary>
    string Origin { get; }

    /// <summary>Gets every pack file as a relative path with forward slashes, including manifest.json.</summary>
    IReadOnlyList<string> Files { get; }

    /// <summary>
    /// Opens one pack file.
    /// </summary>
    /// <param name="relativePath">A path from <see cref="Files"/>.</param>
    /// <returns>A readable stream the caller disposes.</returns>
    Stream Open(string relativePath);

    /// <summary>Gets the custom checks this pack's rules may name.</summary>
    IReadOnlyList<ICustomCheck> CustomChecks { get; }

    /// <summary>Gets the batch checks this pack's rules may name.</summary>
    IReadOnlyList<IBatchCheck> BatchChecks { get; }

    /// <summary>Gets the brand's tier ladder, or <see langword="null"/> for the general pack.</summary>
    ITierPolicy? TierPolicy { get; }
}
