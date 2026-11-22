using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Authorize.net createTransactionRequest adapter (FR-004, FR-007).</summary>
public sealed class AuthorizeNetAdapterTests
{
    private const string Complete = "authorizenet/01-complete-level3";
    private const string Request = "/createTransactionRequest/transactionRequest";

    [Theory]
    [InlineData("/amount", Request + "/amount")]
    [InlineData("/currency", Request + "/currencyCode")]
    [InlineData("/tax/amount", Request + "/tax/amount")]
    [InlineData("/tax/indicator", Request + "/taxExempt")]
    [InlineData("/customer/reference", Request + "/poNumber")]
    [InlineData("/order/invoiceNumber", Request + "/order/invoiceNumber")]
    [InlineData("/amounts/freight", Request + "/shipping/amount")]
    [InlineData("/amounts/duty", Request + "/duty/amount")]
    [InlineData("/amounts/discount", Request + "/discount/amount")]
    [InlineData("/shipping/fromPostalCode", Request + "/shipFrom/zip")]
    [InlineData("/shipping/toPostalCode", Request + "/shipTo/zip")]
    [InlineData("/shipping/toCountry", Request + "/shipTo/country")]
    [InlineData("/billing/postalCode", Request + "/billTo/zip")]
    [InlineData("/lineItems", Request + "/lineItems/lineItem")]
    [InlineData("/lineItems/1/unitCost", Request + "/lineItems/lineItem/1/unitPrice")]
    [InlineData("/lineItems/1/taxable", Request + "/lineItems/lineItem/1/taxable")]
    [InlineData("/lineItems/0/amountIncludesTax", Request + "/lineItems/lineItem/0/taxIncludedInTotal")]
    public void Path_map_points_at_wrapped_request_fields(string canonical, string source) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBe(source);

    [Fact]
    public void Bare_transaction_request_paths_have_no_wrapper()
    {
        var result = Mapped.Fixture("authorizenet/02-bare-level2-numbers");

        result.Source("/amounts/freight").ShouldBe("/transactionRequest/shipping/amount");
        result.Source("/lineItems/0/total").ShouldBe("/transactionRequest/lineItems/lineItem/0/totalAmount");
        result.Transaction.Amount.ShouldBe(612.45m);
    }

    [Fact]
    public void A_single_line_item_object_maps_to_one_line_with_object_paths()
    {
        var result = Mapped.Fixture("authorizenet/03-single-line-object");

        result.Transaction.LineItems.Count.ShouldBe(1);
        result.Source("/lineItems/0/total").ShouldBe(Request + "/lineItems/lineItem/totalAmount");
        result.Source("/lineItems/0").ShouldBe(Request + "/lineItems/lineItem");
        result.Source("/lineItems/1/total").ShouldBe(Request + "/lineItems/lineItem/1/totalAmount");
    }

    [Theory]
    [InlineData("exempt", "\"true\"")]
    [InlineData("taxable", "\"false\"")]
    public void Tax_indicator_writes_back_as_quoted_taxExempt(string indicator, string expected) =>
        Mapped.Fixture(Complete).ToSource("/tax/indicator", JsonValue.Create(indicator))!.ToJsonString().ShouldBe(expected);

    [Fact]
    public void Line_flags_and_amounts_are_written_back_as_strings()
    {
        var result = Mapped.Fixture(Complete);

        result.ToSource("/lineItems/0/taxable", JsonValue.Create(true))!.ToJsonString().ShouldBe("\"true\"");
        result.ToSource("/lineItems/0/total", JsonValue.Create(120.00m))!.ToJsonString().ShouldBe("\"120.00\"");
    }

    [Fact]
    public void Default_currency_is_used_only_when_currencyCode_is_missing()
    {
        var result = Mapped.Fixture("authorizenet/04-jpy-default-currency");

        result.Transaction.Currency.ShouldBe("JPY");
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Defaulted && n.CanonicalPointer == "/currency");
        Mapped.Fixture(Complete).Notes.ShouldNotContain(n => n.Kind == AdapterNoteKind.Defaulted);
    }

    [Fact]
    public void Description_and_sku_fallbacks_add_ambiguous_notes_and_move_the_path()
    {
        var result = Mapped.Fixture("authorizenet/05-description-and-sku-fallback");

        result.Source("/lineItems/0/description").ShouldBe(Request + "/lineItems/lineItem/0/description");
        result.Source("/lineItems/1/productCode").ShouldBe(Request + "/lineItems/lineItem/1/productSKU");
        result.Source("/lineItems/1/description").ShouldBe(Request + "/lineItems/lineItem/1/name");
        result.Notes.Count(n => n.Kind == AdapterNoteKind.Ambiguous).ShouldBe(2);
    }

    [Fact]
    public void Alpha3_ship_to_country_maps_to_alpha2()
    {
        var result = Mapped.Fixture("authorizenet/10-alpha3-country-and-po-date");

        result.Transaction.Shipping.ToCountry.ShouldBe("US");
        result.ToSource("/shipping/toCountry", JsonValue.Create("US"))!.GetValue<string>().ShouldBe("USA");
    }

    [Fact]
    public void Free_text_country_is_copied_with_an_ambiguous_note()
    {
        var result = Mapped.Json("authorizenet", """{ "transactionRequest": { "amount": "1.00", "shipTo": { "country": "United States" } } }""");

        result.Transaction.Shipping.ToCountry.ShouldBe("United States");
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer == "/shipping/toCountry");
    }

    [Fact]
    public void Line_items_sent_as_a_plain_array_are_reported_once_as_invalid()
    {
        var result = Mapped.Json("authorizenet", """{ "transactionRequest": { "amount": "1.00", "lineItems": [ { "name": "x" } ] } }""");

        result.Transaction.LineItems.ShouldBeEmpty();
        result.InvalidFields.ShouldContain("/lineItems");
        result.Notes.Count(n => n.Kind == AdapterNoteKind.Invalid).ShouldBe(1);
    }

    [Fact]
    public void Tax_included_in_total_maps_to_amount_includes_tax()
    {
        var result = Mapped.Fixture("authorizenet/06-tax-included-in-total");

        result.Transaction.LineItems.Select(l => l.AmountIncludesTax).ShouldBe([true, true]);
        result.Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Taxable);
    }
}
