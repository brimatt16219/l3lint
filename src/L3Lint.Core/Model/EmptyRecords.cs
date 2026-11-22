namespace L3Lint.Core.Model;

/// <summary>Shared empty instances used to detect objects that carry no values.</summary>
internal static class EmptyRecords
{
    public static readonly CardInfo Card = new();
    public static readonly MerchantInfo Merchant = new();
    public static readonly CustomerInfo Customer = new();
    public static readonly OrderInfo Order = new();
    public static readonly TaxInfo Tax = new();
    public static readonly SummaryAmounts Amounts = new();
    public static readonly ShippingInfo Shipping = new();
    public static readonly BillingInfo Billing = new();
}
