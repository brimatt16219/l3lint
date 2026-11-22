namespace L3Lint.Core;

/// <summary>
/// How serious a finding is. Values are ordered: information is lowest, error is highest.
/// </summary>
/// <remarks>Severity words always print in text output so color is never the only signal (NFR-010).</remarks>
public enum Severity
{
    /// <summary>Context the user should know, such as "no enhanced-data rate exists for this card".</summary>
    Information = 0,

    /// <summary>A likely problem that does not block the rate on its own, or a heuristic.</summary>
    Warning = 1,

    /// <summary>A documented requirement is not met; the transaction is likely to downgrade.</summary>
    Error = 2,
}
