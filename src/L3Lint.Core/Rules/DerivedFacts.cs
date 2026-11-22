using L3Lint.Core.Model;

namespace L3Lint.Core.Rules;

/// <summary>
/// Values computed once per transaction (pipeline stage 6) so rules read them instead of recomputing.
/// </summary>
/// <remarks>
/// Arithmetic follows the four formulas of the Card brand rules tab in <see cref="decimal"/> at full
/// precision; rounding happens once, at comparison, to the currency exponent (ADR-0014).
/// </remarks>
public sealed class DerivedFacts
{
    internal DerivedFacts()
    {
    }

    /// <summary>Gets the transaction currency, or <see langword="null"/> when missing or not ISO 4217 (arithmetic rules then skip).</summary>
    public Currency? Currency { get; internal init; }

    /// <summary>Gets the midpoint rounding mode for comparisons.</summary>
    public MidpointRounding Rounding { get; internal init; }

    /// <summary>Gets the line tolerance in major units.</summary>
    public decimal LineTolerance { get; internal init; }

    /// <summary>Gets the transaction total tolerance in major units, after the tolerance mode is applied.</summary>
    public decimal TotalTolerance { get; internal init; }

    /// <summary>Gets per-line facts, in line order.</summary>
    public IReadOnlyList<LineFacts> Lines { get; internal init; } = [];

    /// <summary>
    /// Gets the transaction amount implied by formula 2 from the reported line totals, or <see langword="null"/>
    /// when there are no lines, a line total is missing, or the currency is unknown.
    /// </summary>
    public decimal? ExpectedAmount { get; internal init; }

    /// <summary>Gets the sum of line tax amounts (absent counts as 0).</summary>
    public decimal SumLineTax { get; internal init; }

    /// <summary>Gets a value indicating whether any line carries a tax amount.</summary>
    public bool AnyLineTax { get; internal init; }

    /// <summary>Gets the sum of line discounts (absent counts as 0).</summary>
    public decimal SumLineDiscount { get; internal init; }

    /// <summary>Gets header tax divided by the amount (formula 4), or <see langword="null"/> when either is missing or the amount is 0.</summary>
    public decimal? TaxRatio { get; internal init; }

    /// <summary>Gets the merchant name normalized for data quality matching.</summary>
    public string? NormalizedMerchantName { get; internal init; }

    /// <summary>Gets a value indicating whether any line failed formula 1, so GEN-ARITH-002 can name it as the likely cause.</summary>
    public bool AnyLineTotalMismatch { get; internal init; }

    /// <summary>Gets the canonical pointer templates the adapter derived instead of reading.</summary>
    public IReadOnlySet<string> DerivedFields { get; internal init; } = new HashSet<string>();

    /// <summary>
    /// Tells whether a value was derived by the adapter rather than sent.
    /// </summary>
    /// <param name="template">A canonical template such as /lineItems/*/total.</param>
    /// <returns><see langword="true"/> when the adapter computed the value.</returns>
    public bool IsDerived(string template) => DerivedFields.Contains(template);
}
