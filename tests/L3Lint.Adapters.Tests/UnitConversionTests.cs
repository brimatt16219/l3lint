using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;

namespace L3Lint.Adapters.Tests;

/// <summary>Minor units are converted with the ISO 4217 exponent of the payload's currency (FR-008, D9).</summary>
public sealed class UnitConversionTests
{
    [Theory]
    [InlineData("stripe", """{ "amount": 1250, "currency": "jpy", "amount_details": {} }""", "JPY", "1250")]
    [InlineData("stripe", """{ "amount": 1250, "currency": "usd", "amount_details": {} }""", "USD", "12.50")]
    [InlineData("stripe", """{ "amount": 1250, "currency": "bhd", "amount_details": {} }""", "BHD", "1.250")]
    [InlineData("checkoutcom", """{ "amount": 1250, "currency": "JPY", "processing": {} }""", "JPY", "1250")]
    [InlineData("checkoutcom", """{ "amount": 1250, "currency": "KWD", "processing": {} }""", "KWD", "1.250")]
    [InlineData("adyen", """{ "merchantAccount": "M", "amount": { "value": 1250, "currency": "EUR" } }""", "EUR", "12.50")]
    [InlineData("adyen", """{ "merchantAccount": "M", "amount": { "value": 1250, "currency": "CLP" } }""", "CLP", "1250")]
    public void Minor_units_use_the_currency_exponent(string adapter, string json, string currency, string major)
    {
        var result = Mapped.Json(adapter, json);

        result.Transaction.Currency.ShouldBe(currency);
        result.Transaction.Amount.ShouldBe(decimal.Parse(major, System.Globalization.CultureInfo.InvariantCulture));
        result.Notes.ShouldNotContain(n => n.Kind == AdapterNoteKind.Ambiguous);
    }

    [Fact]
    public void Legacy_Adyen_strings_in_minor_units_use_the_exponent()
    {
        var result = Mapped.Json("adyen", """{ "merchantAccount": "M", "amount": { "value": 19204, "currency": "BHD" }, "additionalData": { "enhancedSchemeData.totalTaxAmount": "829" } }""");

        result.Transaction.Tax.Amount.ShouldBe(0.829m);
    }

    [Theory]
    [InlineData("""{ "amount": 1250, "currency": "xyz", "amount_details": {} }""", "XYZ")]
    [InlineData("""{ "amount": 1250, "amount_details": {} }""", null)]
    public void Unknown_or_missing_currency_assumes_two_decimals_with_an_ambiguous_note(string json, string? currency)
    {
        var result = Mapped.Json("stripe", json);

        result.Transaction.Currency.ShouldBe(currency);
        result.Transaction.Amount.ShouldBe(12.50m);
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer == "/currency");
    }

    [Fact]
    public void Decimal_string_formats_keep_major_units()
    {
        Mapped.Json("braintree", """{ "amount": "1250", "taxAmount": "0", "purchaseOrderNumber": "P" }""", new AdapterOptions { DefaultCurrency = "BHD" })
            .Transaction.Amount.ShouldBe(1250m);
        Mapped.Json("cybersource", """{ "orderInformation": { "amountDetails": { "totalAmount": "12.345", "currency": "BHD" } } }""")
            .Transaction.Amount.ShouldBe(12.345m);
    }

    [Fact]
    public void Derived_Stripe_line_total_rounds_to_the_currency_exponent()
    {
        var result = Mapped.Json("stripe", """{ "amount": 3000, "currency": "jpy", "amount_details": { "line_items": [ { "product_name": "A", "unit_cost": 1000, "quantity": 3, "discount_amount": 100 } ] } }""");

        result.Transaction.LineItems[0].Total.ShouldBe(2900m);
    }
}
