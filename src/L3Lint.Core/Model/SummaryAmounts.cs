namespace L3Lint.Core.Model;

/// <summary>
/// Header-level summary amounts in major units.
/// </summary>
/// <remarks>Canonical object <c>amounts</c>. Visa Product 3 needs discount, freight and duty, sent as 0 when they do not apply (VISA-L3-011).</remarks>
public sealed record SummaryAmounts
{
    /// <summary>Header-level discount (0 or more).</summary>
    public decimal? Discount { get; init; }

    /// <summary>Freight or shipping amount (0 or more).</summary>
    public decimal? Freight { get; init; }

    /// <summary>Duty amount (0 or more).</summary>
    public decimal? Duty { get; init; }

    /// <summary>Tax on shipping (0 or more).</summary>
    public decimal? ShippingTax { get; init; }

    /// <summary>Handling amount (0 or more). Not part of the transaction total formula (ADR-0014).</summary>
    public decimal? Handling { get; init; }
}
