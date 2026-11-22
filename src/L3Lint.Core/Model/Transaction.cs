namespace L3Lint.Core.Model;

/// <summary>
/// One commercial card transaction in l3lint's canonical, gateway-neutral shape.
/// </summary>
/// <remarks>
/// <para>
/// The record mirrors the canonical JSON one to one (ADR-0011). Every optional value is nullable
/// because absent is not zero: "freight not sent" and "freight sent as 0" lead to different findings
/// under Visa. Amounts inside a transaction share its <see cref="Currency"/> and are plain decimals in
/// major units.
/// </para>
/// <para>Instances are immutable and safe to share across threads.</para>
/// </remarks>
public sealed record Transaction
{
    /// <summary>Canonical schema version; "1.0" for this release.</summary>
    public string? SchemaVersion { get; init; } = "1.0";

    /// <summary>The merchant's own reference for the transaction (1 to 64 characters).</summary>
    public string? Id { get; init; }

    /// <summary>Transaction amount in major units. Must be greater than 0.</summary>
    public decimal? Amount { get; init; }

    /// <summary>ISO 4217 alpha-3 currency code, for example USD.</summary>
    public string? Currency { get; init; }

    /// <summary>Date of the transaction. Drives rule effective dates when no as-of date is given.</summary>
    public DateOnly? TransactionDate { get; init; }

    /// <summary>Whether the card was present.</summary>
    public Channel? Channel { get; init; }

    /// <summary>Card brand and product context.</summary>
    public CardInfo Card { get; init; } = new();

    /// <summary>Merchant context.</summary>
    public MerchantInfo Merchant { get; init; } = new();

    /// <summary>Customer (buyer) reference.</summary>
    public CustomerInfo Customer { get; init; } = new();

    /// <summary>Order and invoice references.</summary>
    public OrderInfo Order { get; init; } = new();

    /// <summary>Header-level tax data.</summary>
    public TaxInfo Tax { get; init; } = new();

    /// <summary>Header-level summary amounts: discount, freight, duty and similar.</summary>
    public SummaryAmounts Amounts { get; init; } = new();

    /// <summary>Shipping postal codes and destination country.</summary>
    public ShippingInfo Shipping { get; init; } = new();

    /// <summary>Billing address data used for address verification.</summary>
    public BillingInfo Billing { get; init; } = new();

    /// <summary>American Express specific fields.</summary>
    public AmexInfo Amex { get; init; } = new();

    /// <summary>Invoice line items in order. Empty when the payload has none.</summary>
    public IReadOnlyList<LineItem> LineItems { get; init; } = [];
}
