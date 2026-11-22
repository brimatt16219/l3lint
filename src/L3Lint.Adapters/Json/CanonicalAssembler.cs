using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Json;

/// <summary>
/// Builds a canonical transaction by reading every routed field of a gateway document. Fields without a route
/// stay absent; adapters add derived values afterwards (ADR-0008).
/// </summary>
internal static class CanonicalAssembler
{
    /// <summary>Reads the transaction.</summary>
    /// <param name="reader">The document reader.</param>
    /// <param name="routes">The routes of the document's shape.</param>
    /// <param name="lines">The line plan.</param>
    /// <param name="currency">The currency, read beforehand because minor units depend on it.</param>
    /// <param name="lineRoutes">Routes for line fields when they differ per line; <see langword="null"/> uses <paramref name="routes"/>.</param>
    /// <param name="item">Gives the pointer of line object i, or <see langword="null"/> when lines are not objects.</param>
    /// <returns>The canonical transaction.</returns>
    public static Transaction Assemble(PayloadReader reader, RouteSet routes, LinePlan lines, string? currency, Func<int, RouteSet>? lineRoutes = null, Func<int, string?>? item = null)
    {
        string? Text(string pointer) => reader.ReadText(routes.Header(pointer), 0, pointer);
        decimal? Number(string pointer) => reader.ReadNumber(routes.Header(pointer), 0, pointer);
        DateOnly? Date(string pointer) => reader.ReadDate(routes.Header(pointer), 0, pointer);

        return new Transaction
        {
            Id = Text("/id"),
            Amount = Number("/amount"),
            Currency = currency,
            TransactionDate = Date("/transactionDate"),
            Card = new CardInfo { ProductCode = Text("/card/productCode"), Last4 = Text("/card/last4") },
            Merchant = new MerchantInfo
            {
                Mcc = Text("/merchant/mcc"),
                Name = Text("/merchant/name"),
                Country = Text("/merchant/country"),
                TaxId = Text("/merchant/taxId"),
            },
            Customer = new CustomerInfo { Reference = Text("/customer/reference") },
            Order = new OrderInfo
            {
                PurchaseOrderNumber = Text("/order/purchaseOrderNumber"),
                InvoiceNumber = Text("/order/invoiceNumber"),
                OrderDate = Date("/order/orderDate"),
            },
            Tax = new TaxInfo
            {
                Amount = Number("/tax/amount"),
                Indicator = reader.ReadTaxIndicator(routes.Header("/tax/indicator"), "/tax/indicator"),
                Rate = Number("/tax/rate"),
                ExemptionId = Text("/tax/exemptionId"),
            },
            Amounts = new SummaryAmounts
            {
                Discount = Number("/amounts/discount"),
                Freight = Number("/amounts/freight"),
                Duty = Number("/amounts/duty"),
                ShippingTax = Number("/amounts/shippingTax"),
                Handling = Number("/amounts/handling"),
            },
            Shipping = new ShippingInfo
            {
                FromPostalCode = Text("/shipping/fromPostalCode"),
                ToPostalCode = Text("/shipping/toPostalCode"),
                ToCountry = Text("/shipping/toCountry"),
            },
            Billing = new BillingInfo { PostalCode = Text("/billing/postalCode") },
            Amex = new AmexInfo { RequestorName = Text("/amex/requestorName") },
            LineItems = ReadLines(reader, routes, lines, lineRoutes, item ?? (i => routes.Item?.Format(lines.Key(i)))),
        };
    }

    /// <summary>Gets the canonical pointer of a line field.</summary>
    public static string LinePointer(int index, string field) =>
        "/lineItems/" + index.ToString(CultureInfo.InvariantCulture) + "/" + field;

    private static List<LineItem> ReadLines(PayloadReader reader, RouteSet routes, LinePlan lines, Func<int, RouteSet>? lineRoutes, Func<int, string?> item)
    {
        var result = new List<LineItem>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            // Sequence always comes from array order; no gateway line number is trusted over it (ADR-0008).
            var sequence = i + 1;
            if (item(i) is { } itemPointer
                && reader.Find(itemPointer, out var element, out _) is var found
                && (found == PayloadReader.Lookup.Absent || element.ValueKind != JsonValueKind.Object))
            {
                var at = "/lineItems/" + i.ToString(CultureInfo.InvariantCulture);
                reader.Log.AddInvalid(itemPointer, at, Messages.Format(Resources.Strings.InvalidValue, itemPointer, Resources.Strings.ExpectObject));
                result.Add(new LineItem { Sequence = sequence });
                continue;
            }

            var set = lineRoutes?.Invoke(i) ?? routes;
            var key = lines.Key(i);
            string? Text(string field) => reader.ReadText(set.Line(field), key, LinePointer(i, field));
            decimal? Number(string field) => reader.ReadNumber(set.Line(field), key, LinePointer(i, field));
            bool? Flag(string field) => reader.ReadFlag(set.Line(field), key, LinePointer(i, field));

            result.Add(new LineItem
            {
                Sequence = sequence,
                Description = Text("description"),
                ProductCode = Text("productCode"),
                CommodityCode = Text("commodityCode"),
                Quantity = Number("quantity"),
                UnitOfMeasure = Text("unitOfMeasure"),
                UnitCost = Number("unitCost"),
                DiscountAmount = Number("discountAmount"),
                DiscountIndicator = Flag("discountIndicator"),
                TaxAmount = Number("taxAmount"),
                TaxRate = Number("taxRate"),
                Taxable = Flag("taxable"),
                TaxTypeCode = Text("taxTypeCode"),
                AmountIncludesTax = Flag("amountIncludesTax"),
                Total = Number("total"),
                DebitCreditIndicator = reader.ReadDebitCredit(set.Line("debitCreditIndicator"), key, LinePointer(i, "debitCreditIndicator")),
                DetailIndicator = Text("detailIndicator"),
            });
        }

        return result;
    }
}
