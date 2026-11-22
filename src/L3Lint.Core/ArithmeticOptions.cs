namespace L3Lint.Core;

/// <summary>
/// Tolerance and rounding for the arithmetic rules (Card brand rules tab, "Tolerance and rounding settings").
/// </summary>
/// <remarks>Defaults: 1 minor unit for line and total tolerance, strict mode, round half away from zero (ADR-0014).</remarks>
public sealed record ArithmeticOptions
{
    /// <summary>Gets the line tolerance in major units; <see langword="null"/> means 1 minor unit of the currency.</summary>
    public decimal? LineTolerance { get; init; }

    /// <summary>Gets the transaction total tolerance in major units; <see langword="null"/> means 1 minor unit.</summary>
    public decimal? TotalTolerance { get; init; }

    /// <summary>Gets how the total tolerance grows with line count.</summary>
    public ToleranceMode ToleranceMode { get; init; } = ToleranceMode.Strict;

    /// <summary>Gets how midpoints round: away from zero (default) or to even.</summary>
    public MidpointRounding Rounding { get; init; } = MidpointRounding.AwayFromZero;
}
