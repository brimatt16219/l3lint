using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Core.Tests;

/// <summary>
/// The canonical reader is tolerant (ADR-0036): a value of the wrong type maps to nothing and its own pointer is
/// recorded as invalid, so the schema finding reports it and rules that need it stay quiet. Detection follows the
/// Architecture tab: the version marker, or only canonical keys at the top level and in every line.
/// </summary>
public sealed class CanonicalReaderTests
{
    [Fact]
    public void Every_wrongly_typed_field_is_marked_invalid_at_its_own_pointer()
    {
        const string json = """
            {
              "schemaVersion": 1, "id": 2, "amount": "x", "currency": 3, "transactionDate": 4, "channel": "pigeon",
              "card": { "brand": 1, "product": "platinum", "productCode": 3, "isFederalGovernment": "yes", "last4": 4 },
              "merchant": { "mcc": 5047, "name": 1, "country": 2, "taxId": 3 },
              "customer": { "reference": 1 },
              "order": { "purchaseOrderNumber": 1, "invoiceNumber": 2, "orderDate": "19-10-2026" },
              "tax": { "amount": "x", "indicator": 1, "rate": "x", "exemptionId": 1 },
              "amounts": { "discount": "x", "freight": "x", "duty": "x", "shippingTax": "x", "handling": "x" },
              "shipping": { "fromPostalCode": 1, "toPostalCode": 2, "toCountry": 3 },
              "billing": 7,
              "amex": { "requestorName": 1, "adviceText": ["first", 5] },
              "lineItems": [
                {
                  "sequence": 1.5, "description": 1, "productCode": 2, "commodityCode": 3, "commodityScheme": "dewey",
                  "quantity": "x", "unitOfMeasure": 1, "unitCost": "x", "discountAmount": "x", "discountIndicator": "y",
                  "taxAmount": "x", "taxRate": "x", "taxable": "y", "taxTypeCode": 1, "amountIncludesTax": "y",
                  "total": "x", "debitCreditIndicator": "both", "detailIndicator": 1
                },
                "not a line",
                { "sequence": 3000000000 }
              ]
            }
            """;

        var result = Map(json);

        result.InvalidFields.Order(StringComparer.Ordinal).ShouldBe(
        [
            "/amex/adviceText/1", "/amex/requestorName",
            "/amount",
            "/amounts/discount", "/amounts/duty", "/amounts/freight", "/amounts/handling", "/amounts/shippingTax",
            "/billing",
            "/card/brand", "/card/isFederalGovernment", "/card/last4", "/card/product", "/card/productCode",
            "/channel", "/currency",
            "/customer/reference",
            "/id",
            "/lineItems/0/amountIncludesTax", "/lineItems/0/commodityCode", "/lineItems/0/commodityScheme",
            "/lineItems/0/debitCreditIndicator", "/lineItems/0/description", "/lineItems/0/detailIndicator",
            "/lineItems/0/discountAmount", "/lineItems/0/discountIndicator", "/lineItems/0/productCode",
            "/lineItems/0/quantity", "/lineItems/0/sequence", "/lineItems/0/taxAmount", "/lineItems/0/taxRate",
            "/lineItems/0/taxTypeCode", "/lineItems/0/taxable", "/lineItems/0/total", "/lineItems/0/unitCost",
            "/lineItems/0/unitOfMeasure",
            "/lineItems/1",
            "/lineItems/2/sequence",
            "/merchant/country", "/merchant/mcc", "/merchant/name", "/merchant/taxId",
            "/order/invoiceNumber", "/order/orderDate", "/order/purchaseOrderNumber",
            "/schemaVersion",
            "/shipping/fromPostalCode", "/shipping/toCountry", "/shipping/toPostalCode",
            "/tax/amount", "/tax/exemptionId", "/tax/indicator", "/tax/rate",
            "/transactionDate",
        ]);

        // A line that is not an object keeps its place, so the next line keeps index 2; an advice text entry of
        // the wrong type keeps its place as an empty string.
        result.Transaction.LineItems.Count.ShouldBe(3);
        result.Transaction.Amex.AdviceText.ShouldBe(["first", string.Empty]);
    }

    [Fact]
    public void A_line_item_list_or_advice_text_that_is_not_an_array_is_invalid_as_a_whole()
    {
        var result = Map("""{ "schemaVersion": "1.0", "amex": { "adviceText": "one line" }, "lineItems": { "total": 1 } }""");

        result.InvalidFields.Order(StringComparer.Ordinal).ShouldBe(["/amex/adviceText", "/lineItems"]);
        result.Transaction.LineItems.ShouldBeEmpty();
        result.Transaction.Amex.AdviceText.ShouldBeNull();
    }

    [Fact]
    public void Whole_numbers_within_range_are_valid_sequences()
    {
        var result = Map("""{ "schemaVersion": "1.0", "lineItems": [ { "sequence": 2 }, { "sequence": 2.0 }, { "sequence": -1 } ] }""");

        result.InvalidFields.ShouldBeEmpty();
        result.Transaction.LineItems.Select(l => l.Sequence).ShouldBe([2, 2, -1]);
    }

    [Theory]
    [InlineData("""{ "schemaVersion": "1.0" }""", 1.0)]
    [InlineData("""{ "id": "INV-1", "amount": 10.00, "lineItems": [ { "description": "Gloves", "total": 10.00 } ] }""", 0.85)]
    [InlineData("""{ "amount": 10.00 }""", 0.85)]
    [InlineData("""{ "lineItems": [] }""", 0.85)]
    [InlineData("""{ "id": "INV-1", "merchant": { "name": "Acme" } }""", 0.0)]
    [InlineData("""{ "amount": 10.00, "order_id": "INV-1" }""", 0.0)]
    [InlineData("""{ "amount": 10.00, "lineItems": [ { "name": "Gloves", "kind": "debit" } ] }""", 0.0)]
    [InlineData("""{ "amount": 10.00, "lineItems": [ "not a line" ] }""", 0.85)]
    [InlineData("""{ }""", 0.0)]
    public void Detection_needs_the_version_marker_or_only_canonical_keys(string json, double score)
    {
        // The Braintree-like case: canonical top-level names but gateway line keys is not claimed.
        new CanonicalAdapter().Detect(RawDocument.ParseJson(json, "p.json")).ShouldBe(score);
    }

    private static AdapterResult Map(string json) =>
        new CanonicalAdapter().Map(RawDocument.ParseJson(json, "p.json"), AdapterOptions.Default).ShouldHaveSingleItem();
}
