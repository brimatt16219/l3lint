namespace L3Lint.Core.Model;

/// <summary>
/// Header-level tax data.
/// </summary>
/// <remarks>Canonical object <c>tax</c>. l3lint checks tax for presence, range and consistency only; it never computes tax (NG1).</remarks>
public sealed record TaxInfo
{
    /// <summary>Total sales tax in major units (0 or more).</summary>
    public decimal? Amount { get; init; }

    /// <summary>Whether the sale is taxable or explicitly exempt.</summary>
    public TaxIndicator? Indicator { get; init; }

    /// <summary>Tax rate from 0 to 1 (0.07 means 7%).</summary>
    public decimal? Rate { get; init; }

    /// <summary>Tax registration ID for an exempt sale. Required with an exemption under Amex profiles.</summary>
    public string? ExemptionId { get; init; }
}
