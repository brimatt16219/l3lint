using System.Collections.Frozen;
using L3Lint.Core.Model;

namespace L3Lint.Core.Fields;

/// <summary>
/// The 52 fields of the canonical field dictionary, plus the two collections rules can count.
/// </summary>
/// <remarks>
/// Every rule reads only these fields (Card brand rules tab, "Canonical field dictionary"). Selectors in rule
/// packs are resolved against this table at load time, so a typo in a pack is a pack error (ADR-0012).
/// </remarks>
internal static class CanonicalFields
{
    private static readonly string[] Brands = ["visa", "mastercard", "amex", "discover", "unknown"];
    private static readonly string[] Products = ["consumer", "business", "corporate", "purchasing", "fleet", "commercialPrepaid", "unknown"];
    private static readonly string[] Indicators = ["taxable", "exempt", "notProvided"];
    private static readonly string[] Channels = ["cardPresent", "cardNotPresent"];
    private static readonly string[] Schemes = ["unspsc", "nigp", "hs", "naics", "other", "unknown"];
    private static readonly string[] DebitCredits = ["debit", "credit"];

    /// <summary>Gets every field in dictionary order.</summary>
    public static IReadOnlyList<CanonicalField> All { get; } =
    [
        Header("/schemaVersion", FieldKind.Text, t => FieldValue.Of(t.SchemaVersion), constraint: "ConstraintSchemaVersion"),
        Header("/id", FieldKind.Text, t => FieldValue.Of(t.Id), constraint: "ConstraintId"),
        Header("/amount", FieldKind.Decimal, t => FieldValue.Of(t.Amount), money: true),
        Header("/currency", FieldKind.Text, t => FieldValue.Of(t.Currency)),
        Header("/transactionDate", FieldKind.Date, t => FieldValue.Of(t.TransactionDate), constraint: "ConstraintDate"),
        Header("/channel", FieldKind.Enum, t => t.Channel is { } c ? FieldValue.Of(CanonicalNames.ToName(c)) : FieldValue.Absent, values: Channels),
        Header("/card/brand", FieldKind.Enum, t => t.Card.Brand is { } b ? FieldValue.Of(CanonicalNames.ToName(b)) : FieldValue.Absent, sentinel: "unknown", values: Brands),
        Header("/card/product", FieldKind.Enum, t => t.Card.Product is { } p ? FieldValue.Of(CanonicalNames.ToName(p)) : FieldValue.Absent, sentinel: "unknown", values: Products),
        Header("/card/productCode", FieldKind.Text, t => FieldValue.Of(t.Card.ProductCode)),
        Header("/card/isFederalGovernment", FieldKind.Boolean, t => FieldValue.Of(t.Card.IsFederalGovernment)),
        Header("/card/last4", FieldKind.Text, t => FieldValue.Of(t.Card.Last4), constraint: "ConstraintLast4"),
        Header("/merchant/mcc", FieldKind.Text, t => FieldValue.Of(t.Merchant.Mcc), constraint: "ConstraintMcc"),
        Header("/merchant/name", FieldKind.Text, t => FieldValue.Of(t.Merchant.Name)),
        Header("/merchant/country", FieldKind.Text, t => FieldValue.Of(t.Merchant.Country), constraint: "ConstraintCountry"),
        Header("/merchant/taxId", FieldKind.Text, t => FieldValue.Of(t.Merchant.TaxId)),
        Header("/customer/reference", FieldKind.Text, t => FieldValue.Of(t.Customer.Reference)),
        Header("/order/purchaseOrderNumber", FieldKind.Text, t => FieldValue.Of(t.Order.PurchaseOrderNumber)),
        Header("/order/invoiceNumber", FieldKind.Text, t => FieldValue.Of(t.Order.InvoiceNumber)),
        Header("/order/orderDate", FieldKind.Date, t => FieldValue.Of(t.Order.OrderDate), constraint: "ConstraintDate"),
        Header("/tax/amount", FieldKind.Decimal, t => FieldValue.Of(t.Tax.Amount), money: true, constraint: "ConstraintNonNegative"),
        Header("/tax/indicator", FieldKind.Enum, t => t.Tax.Indicator is { } i ? FieldValue.Of(CanonicalNames.ToName(i)) : FieldValue.Absent, sentinel: "notProvided", values: Indicators),
        Header("/tax/rate", FieldKind.Decimal, t => FieldValue.Of(t.Tax.Rate), constraint: "ConstraintRate"),
        Header("/tax/exemptionId", FieldKind.Text, t => FieldValue.Of(t.Tax.ExemptionId)),
        Header("/amounts/discount", FieldKind.Decimal, t => FieldValue.Of(t.Amounts.Discount), money: true, constraint: "ConstraintNonNegative"),
        Header("/amounts/freight", FieldKind.Decimal, t => FieldValue.Of(t.Amounts.Freight), money: true, constraint: "ConstraintNonNegative"),
        Header("/amounts/duty", FieldKind.Decimal, t => FieldValue.Of(t.Amounts.Duty), money: true, constraint: "ConstraintNonNegative"),
        Header("/amounts/shippingTax", FieldKind.Decimal, t => FieldValue.Of(t.Amounts.ShippingTax), money: true, constraint: "ConstraintNonNegative"),
        Header("/amounts/handling", FieldKind.Decimal, t => FieldValue.Of(t.Amounts.Handling), money: true, constraint: "ConstraintNonNegative"),
        Header("/shipping/fromPostalCode", FieldKind.Text, t => FieldValue.Of(t.Shipping.FromPostalCode)),
        Header("/shipping/toPostalCode", FieldKind.Text, t => FieldValue.Of(t.Shipping.ToPostalCode)),
        Header("/shipping/toCountry", FieldKind.Text, t => FieldValue.Of(t.Shipping.ToCountry), constraint: "ConstraintCountry"),
        Header("/billing/postalCode", FieldKind.Text, t => FieldValue.Of(t.Billing.PostalCode)),
        Header("/amex/requestorName", FieldKind.Text, t => FieldValue.Of(t.Amex.RequestorName)),
        Header("/amex/adviceText", FieldKind.TextList, t => FieldValue.OfList(t.Amex.AdviceText?.Count)),
        Line("/lineItems/*/sequence", FieldKind.Integer, l => FieldValue.Of(l.Sequence), code: true, constraint: "ConstraintSequence"),
        Line("/lineItems/*/description", FieldKind.Text, l => FieldValue.Of(l.Description)),
        Line("/lineItems/*/productCode", FieldKind.Text, l => FieldValue.Of(l.ProductCode), code: true),
        Line("/lineItems/*/commodityCode", FieldKind.Text, l => FieldValue.Of(l.CommodityCode), code: true),
        Line("/lineItems/*/commodityScheme", FieldKind.Enum, l => l.CommodityScheme is { } s ? FieldValue.Of(CanonicalNames.ToName(s)) : FieldValue.Absent, values: Schemes),
        Line("/lineItems/*/quantity", FieldKind.Decimal, l => FieldValue.Of(l.Quantity)),
        Line("/lineItems/*/unitOfMeasure", FieldKind.Text, l => FieldValue.Of(l.UnitOfMeasure)),
        Line("/lineItems/*/unitCost", FieldKind.Decimal, l => FieldValue.Of(l.UnitCost), constraint: "ConstraintUnitCost"),
        Line("/lineItems/*/discountAmount", FieldKind.Decimal, l => FieldValue.Of(l.DiscountAmount), money: true, constraint: "ConstraintNonNegative"),
        Line("/lineItems/*/discountIndicator", FieldKind.Boolean, l => FieldValue.Of(l.DiscountIndicator)),
        Line("/lineItems/*/taxAmount", FieldKind.Decimal, l => FieldValue.Of(l.TaxAmount), money: true, constraint: "ConstraintNonNegative"),
        Line("/lineItems/*/taxRate", FieldKind.Decimal, l => FieldValue.Of(l.TaxRate), constraint: "ConstraintRate"),
        Line("/lineItems/*/taxable", FieldKind.Boolean, l => FieldValue.Of(l.Taxable)),
        Line("/lineItems/*/taxTypeCode", FieldKind.Text, l => FieldValue.Of(l.TaxTypeCode)),
        Line("/lineItems/*/amountIncludesTax", FieldKind.Boolean, l => FieldValue.Of(l.AmountIncludesTax)),
        Line("/lineItems/*/total", FieldKind.Decimal, l => FieldValue.Of(l.Total), money: true),
        Line("/lineItems/*/debitCreditIndicator", FieldKind.Enum, l => l.DebitCreditIndicator is { } d ? FieldValue.Of(CanonicalNames.ToName(d)) : FieldValue.Absent, values: DebitCredits),
        Line("/lineItems/*/detailIndicator", FieldKind.Text, l => FieldValue.Of(l.DetailIndicator), constraint: "ConstraintDetailIndicator"),
    ];

    /// <summary>Gets the line-items collection pseudo-field.</summary>
    public static CanonicalField LineItems { get; } = new("/lineItems", FieldKind.Collection, t => FieldValue.OfList(t.LineItems.Count), null);

    /// <summary>Gets the advice-text element pseudo-field, addressed as /amex/adviceText/*.</summary>
    public static CanonicalField AdviceTextLine { get; } = new("/amex/adviceText/*", FieldKind.Text, null, null);

    private static readonly FrozenDictionary<string, CanonicalField> ByPath =
        All.Append(LineItems).Append(AdviceTextLine).ToFrozenDictionary(f => f.Path, StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, CanonicalField> LineByName =
        All.Where(f => f.IsLineField).ToFrozenDictionary(f => f.Name, StringComparer.Ordinal);

    /// <summary>Finds a field by its pointer template.</summary>
    public static bool TryGet(string path, out CanonicalField field) => ByPath.TryGetValue(path, out field!);

    /// <summary>Finds the field behind a concrete pointer such as /lineItems/3/total.</summary>
    public static CanonicalField? FindByPointer(string pointer)
    {
        if (ByPath.TryGetValue(pointer, out var direct))
        {
            return direct;
        }

        const string Prefix = "/lineItems/";
        if (pointer.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var rest = pointer.AsSpan(Prefix.Length);
            var slash = rest.IndexOf('/');
            if (slash > 0 && LineByName.TryGetValue(rest[(slash + 1)..].ToString(), out var line))
            {
                return line;
            }
        }

        if (pointer.StartsWith("/amex/adviceText/", StringComparison.Ordinal))
        {
            return AdviceTextLine;
        }

        return null;
    }

    private static CanonicalField Header(string path, FieldKind kind, Func<Transaction, FieldValue> read, string? sentinel = null, bool money = false, IReadOnlyList<string>? values = null, string? constraint = null) =>
        new(path, kind, read, null, sentinel, money, false, values, constraint);

    private static CanonicalField Line(string path, FieldKind kind, Func<LineItem, FieldValue> read, bool money = false, bool code = false, IReadOnlyList<string>? values = null, string? constraint = null) =>
        new(path, kind, null, read, null, money, code, values, constraint);
}
