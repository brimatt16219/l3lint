namespace L3Lint.Core.Input;

/// <summary>
/// Hard limits that protect the linter from hostile or runaway input (FR-034, NFR-006).
/// </summary>
/// <remarks>Exceeding a limit is an input error (exit code 2), not a finding (ADR-0037).</remarks>
public sealed record InputLimits
{
    /// <summary>The default file size limit: 25 MB.</summary>
    public const long DefaultMaxFileBytes = 25L * 1024 * 1024;

    /// <summary>The default line-item limit per transaction: 1,000.</summary>
    public const int DefaultMaxLineItems = 1000;

    /// <summary>The default JSON nesting limit: 64.</summary>
    public const int DefaultMaxJsonDepth = 64;

    /// <summary>Gets the default limits.</summary>
    public static InputLimits Default { get; } = new();

    /// <summary>Gets the largest accepted document or file, in bytes.</summary>
    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;

    /// <summary>Gets the most line items accepted in one transaction.</summary>
    public int MaxLineItems { get; init; } = DefaultMaxLineItems;

    /// <summary>Gets the deepest accepted JSON nesting.</summary>
    public int MaxJsonDepth { get; init; } = DefaultMaxJsonDepth;
}
