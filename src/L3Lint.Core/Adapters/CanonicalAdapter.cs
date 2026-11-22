using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Reads l3lint's own canonical JSON (schema v1) into a <see cref="Transaction"/>.
/// </summary>
/// <remarks>
/// The reader is tolerant: a value of the wrong type is recorded in <see cref="AdapterResult.InvalidFields"/>
/// and left absent, never thrown (ADR-0036). The schema stage reports it as GEN-FMT-001, so this adapter adds
/// no notes of its own. Paths map to themselves.
/// </remarks>
internal sealed class CanonicalAdapter : IPayloadAdapter
{
    private static readonly FrozenSet<string> TopLevelNames = FrozenSet.ToFrozenSet(
        ["schemaVersion", "id", "amount", "currency", "transactionDate", "channel", "card", "merchant", "customer", "order", "tax", "amounts", "shipping", "billing", "amex", "lineItems"],
        StringComparer.Ordinal);

    private const string LinePrefix = "/lineItems/*/";

    // Line field names come from the canonical field table, so the two never drift apart.
    private static readonly FrozenSet<string> LineNames = FrozenSet.ToFrozenSet(
        CanonicalFields.All.Where(f => f.Path.StartsWith(LinePrefix, StringComparison.Ordinal)).Select(f => f.Path[LinePrefix.Length..]),
        StringComparer.Ordinal);

    public string Name => "canonical";

    public InputKind Kind => InputKind.Json;

    public double Detect(RawDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = document.Json;
        if (root.TryGetProperty("schemaVersion", out _))
        {
            return 1.0;
        }

        // Without the version marker the payload can still be canonical if every key is a canonical key.
        var count = 0;
        foreach (var property in root.EnumerateObject())
        {
            if (!TopLevelNames.Contains(property.Name))
            {
                return 0.0;
            }

            count++;
        }

        // A gateway body can reuse canonical top-level names (Braintree sends amount and lineItems), but its line
        // keys are not canonical, so it is not claimed here.
        if (root.TryGetProperty("lineItems", out var lines) && lines.ValueKind == JsonValueKind.Array
            && lines.EnumerateArray().Any(l => l.ValueKind == JsonValueKind.Object && l.EnumerateObject().Any(p => !LineNames.Contains(p.Name))))
        {
            return 0.0;
        }

        return count > 0 && (root.TryGetProperty("lineItems", out _) || root.TryGetProperty("amount", out _)) ? 0.85 : 0.0;
    }

    public IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        var reader = new Reader();
        var transaction = reader.ReadTransaction(document.Json);
        return [new AdapterResult(transaction, PathMap.Identity) { InvalidFields = reader.Invalid }];
    }

    public string? ToCanonicalPointer(string sourcePointer) => sourcePointer;

    /// <summary>Reads one canonical object, collecting pointers of values with the wrong type.</summary>
    private sealed class Reader
    {
        public HashSet<string> Invalid { get; } = new(StringComparer.Ordinal);

        public Transaction ReadTransaction(JsonElement root) => new()
        {
            SchemaVersion = Text(root, "", "schemaVersion"),
            Id = Text(root, "", "id"),
            Amount = Number(root, "", "amount"),
            Currency = Text(root, "", "currency"),
            TransactionDate = Date(root, "", "transactionDate"),
            Channel = EnumValue<Channel>(root, "", "channel", CanonicalNames.TryParse),
            Card = Object(root, "", "card") is { } card ? new CardInfo
            {
                Brand = EnumValue<CardBrand>(card, "/card", "brand", CanonicalNames.TryParse),
                Product = EnumValue<CardProduct>(card, "/card", "product", CanonicalNames.TryParse),
                ProductCode = Text(card, "/card", "productCode"),
                IsFederalGovernment = Flag(card, "/card", "isFederalGovernment"),
                Last4 = Text(card, "/card", "last4"),
            } : new CardInfo(),
            Merchant = Object(root, "", "merchant") is { } merchant ? new MerchantInfo
            {
                Mcc = Text(merchant, "/merchant", "mcc"),
                Name = Text(merchant, "/merchant", "name"),
                Country = Text(merchant, "/merchant", "country"),
                TaxId = Text(merchant, "/merchant", "taxId"),
            } : new MerchantInfo(),
            Customer = Object(root, "", "customer") is { } customer
                ? new CustomerInfo { Reference = Text(customer, "/customer", "reference") }
                : new CustomerInfo(),
            Order = Object(root, "", "order") is { } order ? new OrderInfo
            {
                PurchaseOrderNumber = Text(order, "/order", "purchaseOrderNumber"),
                InvoiceNumber = Text(order, "/order", "invoiceNumber"),
                OrderDate = Date(order, "/order", "orderDate"),
            } : new OrderInfo(),
            Tax = Object(root, "", "tax") is { } tax ? new TaxInfo
            {
                Amount = Number(tax, "/tax", "amount"),
                Indicator = EnumValue<TaxIndicator>(tax, "/tax", "indicator", CanonicalNames.TryParse),
                Rate = Number(tax, "/tax", "rate"),
                ExemptionId = Text(tax, "/tax", "exemptionId"),
            } : new TaxInfo(),
            Amounts = Object(root, "", "amounts") is { } amounts ? new SummaryAmounts
            {
                Discount = Number(amounts, "/amounts", "discount"),
                Freight = Number(amounts, "/amounts", "freight"),
                Duty = Number(amounts, "/amounts", "duty"),
                ShippingTax = Number(amounts, "/amounts", "shippingTax"),
                Handling = Number(amounts, "/amounts", "handling"),
            } : new SummaryAmounts(),
            Shipping = Object(root, "", "shipping") is { } shipping ? new ShippingInfo
            {
                FromPostalCode = Text(shipping, "/shipping", "fromPostalCode"),
                ToPostalCode = Text(shipping, "/shipping", "toPostalCode"),
                ToCountry = Text(shipping, "/shipping", "toCountry"),
            } : new ShippingInfo(),
            Billing = Object(root, "", "billing") is { } billing
                ? new BillingInfo { PostalCode = Text(billing, "/billing", "postalCode") }
                : new BillingInfo(),
            Amex = Object(root, "", "amex") is { } amex ? new AmexInfo
            {
                RequestorName = Text(amex, "/amex", "requestorName"),
                AdviceText = TextList(amex, "/amex", "adviceText"),
            } : new AmexInfo(),
            LineItems = Lines(root),
        };

        private List<LineItem> Lines(JsonElement root)
        {
            if (!root.TryGetProperty("lineItems", out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return [];
            }

            if (value.ValueKind != JsonValueKind.Array)
            {
                Invalid.Add("/lineItems");
                return [];
            }

            var lines = new List<LineItem>(value.GetArrayLength());
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                var at = "/lineItems/" + index.ToString(CultureInfo.InvariantCulture);
                index++;
                if (item.ValueKind != JsonValueKind.Object)
                {
                    // Keep the index so later lines keep their pointers; the whole line is invalid.
                    Invalid.Add(at);
                    lines.Add(new LineItem());
                    continue;
                }

                lines.Add(new LineItem
                {
                    Sequence = Integer(item, at, "sequence"),
                    Description = Text(item, at, "description"),
                    ProductCode = Text(item, at, "productCode"),
                    CommodityCode = Text(item, at, "commodityCode"),
                    CommodityScheme = EnumValue<CommodityScheme>(item, at, "commodityScheme", CanonicalNames.TryParse),
                    Quantity = Number(item, at, "quantity"),
                    UnitOfMeasure = Text(item, at, "unitOfMeasure"),
                    UnitCost = Number(item, at, "unitCost"),
                    DiscountAmount = Number(item, at, "discountAmount"),
                    DiscountIndicator = Flag(item, at, "discountIndicator"),
                    TaxAmount = Number(item, at, "taxAmount"),
                    TaxRate = Number(item, at, "taxRate"),
                    Taxable = Flag(item, at, "taxable"),
                    TaxTypeCode = Text(item, at, "taxTypeCode"),
                    AmountIncludesTax = Flag(item, at, "amountIncludesTax"),
                    Total = Number(item, at, "total"),
                    DebitCreditIndicator = EnumValue<DebitCredit>(item, at, "debitCreditIndicator", CanonicalNames.TryParse),
                    DetailIndicator = Text(item, at, "detailIndicator"),
                });
            }

            return lines;
        }

        private JsonElement? Object(JsonElement parent, string at, string name)
        {
            if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                return value;
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private string? Text(JsonElement parent, string at, string name)
        {
            if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private decimal? Number(JsonElement parent, string at, string name)
        {
            if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private int? Integer(JsonElement parent, string at, string name)
        {
            var number = Number(parent, at, name);
            if (number is null)
            {
                return null;
            }

            if (number == decimal.Truncate(number.Value) && number >= int.MinValue && number <= int.MaxValue)
            {
                return (int)number.Value;
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private bool? Flag(JsonElement parent, string at, string name)
        {
            if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private DateOnly? Date(JsonElement parent, string at, string name)
        {
            var text = Text(parent, at, name);
            if (text is null)
            {
                return null;
            }

            if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private T? EnumValue<T>(JsonElement parent, string at, string name, TryParser<T> parse)
            where T : struct
        {
            var text = Text(parent, at, name);
            if (text is null)
            {
                return null;
            }

            if (parse(text, out var value))
            {
                return value;
            }

            Invalid.Add(at + "/" + name);
            return null;
        }

        private List<string>? TextList(JsonElement parent, string at, string name)
        {
            if (!parent.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind != JsonValueKind.Array)
            {
                Invalid.Add(at + "/" + name);
                return null;
            }

            var lines = new List<string>();
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    lines.Add(item.GetString()!);
                }
                else
                {
                    Invalid.Add(at + "/" + name + "/" + index.ToString(CultureInfo.InvariantCulture));
                    lines.Add(string.Empty);
                }

                index++;
            }

            return lines;
        }
    }

    private delegate bool TryParser<T>(string? text, out T value);
}
