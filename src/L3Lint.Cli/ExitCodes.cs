namespace L3Lint.Cli;

/// <summary>Process exit codes (FR-025, Interfaces tab "Exit codes").</summary>
internal static class ExitCodes
{
    /// <summary>Clean, or findings below --fail-on.</summary>
    public const int Clean = 0;

    /// <summary>Findings at or above --fail-on.</summary>
    public const int Findings = 1;

    /// <summary>Usage or input error.</summary>
    public const int Usage = 2;

    /// <summary>Rule pack, config or internal error.</summary>
    public const int Internal = 3;
}
