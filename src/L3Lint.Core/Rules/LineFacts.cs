namespace L3Lint.Core.Rules;

/// <summary>
/// Values computed once for one line item.
/// </summary>
public sealed class LineFacts
{
    internal LineFacts()
    {
    }

    /// <summary>Gets the 0-based line index.</summary>
    public int Index { get; internal init; }

    /// <summary>
    /// Gets the line total implied by formula 1, round_c(q x u) - d + t x I, or <see langword="null"/> when
    /// quantity, unit cost or the currency is missing.
    /// </summary>
    public decimal? ExpectedTotal { get; internal init; }

    /// <summary>Gets reported total minus expected total, when both exist.</summary>
    public decimal? Difference { get; internal init; }

    /// <summary>Gets a value indicating whether the line fails formula 1 beyond the line tolerance (and its total was sent, not derived).</summary>
    public bool TotalMismatch { get; internal init; }

    /// <summary>Gets the line tax counted inside the line total (t x I in the formulas).</summary>
    public decimal IncludedTax { get; internal init; }

    /// <summary>Gets a value indicating whether the line is a credit (return), which subtracts in formula 2.</summary>
    public bool IsCredit { get; internal init; }

    /// <summary>Gets a value indicating whether the adapter computed the line total rather than reading it.</summary>
    public bool TotalIsDerived { get; internal init; }

    /// <summary>Gets the description normalized for data quality matching.</summary>
    public string? NormalizedDescription { get; internal init; }
}
