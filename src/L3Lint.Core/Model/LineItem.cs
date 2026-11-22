namespace L3Lint.Core.Model;

/// <summary>
/// One invoice line in canonical form.
/// </summary>
/// <remarks>
/// Canonical items of <c>lineItems[]</c>. All amounts are in major units of the transaction currency.
/// Absent values stay <see langword="null"/>; adapters never fill a value the source did not send,
/// except <see cref="Sequence"/> from array order and values listed as derived (ADR-0008).
/// </remarks>
public sealed record LineItem
{
    /// <summary>1-based, contiguous line number.</summary>
    public int? Sequence { get; init; }

    /// <summary>Item descriptor: the real product or service name.</summary>
    public string? Description { get; init; }

    /// <summary>SKU or part number (up to 12 characters).</summary>
    public string? ProductCode { get; init; }

    /// <summary>Commodity code, usually an 8-digit UNSPSC code.</summary>
    public string? CommodityCode { get; init; }

    /// <summary>Scheme of <see cref="CommodityCode"/>.</summary>
    public CommodityScheme? CommodityScheme { get; init; }

    /// <summary>Quantity, greater than 0.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Unit of measure, for example EA, BX or EAC.</summary>
    public string? UnitOfMeasure { get; init; }

    /// <summary>Unit price before discount (0 or more, up to 6 decimal places).</summary>
    public decimal? UnitCost { get; init; }

    /// <summary>Line discount (0 or more).</summary>
    public decimal? DiscountAmount { get; init; }

    /// <summary>Set when a discount applies to the line.</summary>
    public bool? DiscountIndicator { get; init; }

    /// <summary>Line tax amount (0 or more).</summary>
    public decimal? TaxAmount { get; init; }

    /// <summary>Line tax rate from 0 to 1.</summary>
    public decimal? TaxRate { get; init; }

    /// <summary>Line tax indicator.</summary>
    public bool? Taxable { get; init; }

    /// <summary>Processor tax type code.</summary>
    public string? TaxTypeCode { get; init; }

    /// <summary>Whether <see cref="Total"/> includes the line tax. Absent means false.</summary>
    public bool? AmountIncludesTax { get; init; }

    /// <summary>Extended amount for the line.</summary>
    public decimal? Total { get; init; }

    /// <summary>Whether the line adds to or subtracts from the total. Absent means debit.</summary>
    public DebitCredit? DebitCreditIndicator { get; init; }

    /// <summary>Visa line-item detail indicator (1 character).</summary>
    public string? DetailIndicator { get; init; }
}
