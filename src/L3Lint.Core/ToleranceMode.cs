namespace L3Lint.Core;

/// <summary>
/// How the transaction total tolerance is computed.
/// </summary>
public enum ToleranceMode
{
    /// <summary>The configured total tolerance (default 1 minor unit) applies as is.</summary>
    Strict,

    /// <summary>1 minor unit per line, capped at 5, for ERPs that round each line.</summary>
    Accumulated,
}
