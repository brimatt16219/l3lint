using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Braintree transaction sale adapter (FR-003, FR-007, FR-008).</summary>
public sealed class BraintreeAdapterTests
{
    private const string Complete = "braintree/01-complete-level3";

    [Theory]
    [InlineData("/amount", "/amount")]
    [InlineData("/tax/amount", "/taxAmount")]
    [InlineData("/tax/indicator", "/taxExempt")]
    [InlineData("/customer/reference", "/purchaseOrderNumber")]
    [InlineData("/order/invoiceNumber", "/orderId")]
    [InlineData("/amounts/freight", "/shippingAmount")]
    [InlineData("/amounts/discount", "/discountAmount")]
    [InlineData("/amounts/shippingTax", "/shippingTaxAmount")]
    [InlineData("/shipping/fromPostalCode", "/shipsFromPostalCode")]
    [InlineData("/shipping/toPostalCode", "/shipping/postalCode")]
    [InlineData("/shipping/toCountry", "/shipping/countryCodeAlpha3")]
    [InlineData("/lineItems/0/description", "/lineItems/0/name")]
    [InlineData("/lineItems/0/unitCost", "/lineItems/0/unitAmount")]
    [InlineData("/lineItems/1/total", "/lineItems/1/totalAmount")]
    [InlineData("/lineItems/1/debitCreditIndicator", "/lineItems/1/kind")]
    public void Path_map_points_at_Braintree_fields(string canonical, string source) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/currency")]
    [InlineData("/amounts/duty")]
    [InlineData("/order/orderDate")]
    [InlineData("/lineItems/0/amountIncludesTax")]
    public void Fields_Braintree_cannot_carry_resolve_to_null(string canonical) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBeNull();

    [Fact]
    public void The_alpha2_country_field_is_mapped_when_the_payload_uses_it() =>
        Mapped.Fixture("braintree/02-level2-only").Source("/shipping/toCountry").ShouldBe("/shipping/countryCodeAlpha2");

    [Fact]
    public void Currency_comes_from_the_default_with_a_defaulted_note()
    {
        var result = Mapped.Fixture(Complete);

        result.Transaction.Currency.ShouldBe("USD");
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Defaulted && n.CanonicalPointer == "/currency");
    }

    [Fact]
    public void Without_a_default_currency_the_currency_stays_absent()
    {
        var result = Mapped.Json("braintree", """{ "amount": "10.00", "taxAmount": "0.70", "purchaseOrderNumber": "PO-1" }""");

        result.Transaction.Currency.ShouldBeNull();
        result.Notes.ShouldNotContain(n => n.Kind == AdapterNoteKind.Defaulted);
    }

    [Theory]
    [InlineData("exempt", "true")]
    [InlineData("taxable", "false")]
    public void Tax_indicator_writes_back_as_the_taxExempt_boolean(string indicator, string expected) =>
        Mapped.Fixture(Complete).ToSource("/tax/indicator", JsonValue.Create(indicator))!.ToJsonString().ShouldBe(expected);

    [Fact]
    public void Not_provided_has_no_taxExempt_representation() =>
        Mapped.Fixture(Complete).ToSource("/tax/indicator", JsonValue.Create("notProvided")).ShouldBeNull();

    [Fact]
    public void Amounts_are_written_back_as_decimal_strings()
    {
        var result = Mapped.Fixture(Complete);

        result.ToSource("/lineItems/0/total", JsonValue.Create(120.00m))!.GetValue<string>().ShouldBe("120.00");
        result.ToSource("/lineItems/0/quantity", JsonValue.Create(10m))!.GetValue<string>().ShouldBe("10");
    }

    [Fact]
    public void Credit_kind_maps_to_a_credit_line()
    {
        var result = Mapped.Fixture("braintree/05-credit-line");

        result.Transaction.LineItems.Select(l => l.DebitCreditIndicator).ShouldBe([DebitCredit.Debit, DebitCredit.Debit, DebitCredit.Credit]);
    }

    [Fact]
    public void Exempt_flag_with_tax_above_zero_maps_to_exempt_with_an_ambiguous_note()
    {
        var result = Mapped.Json("braintree", """{ "amount": "10.70", "taxAmount": "0.70", "taxExempt": true, "purchaseOrderNumber": "PO-1" }""");

        result.Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Exempt);
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.SourcePointer == "/taxExempt");
    }

    [Fact]
    public void Missing_tax_flag_stays_absent_because_Braintree_can_send_it()
    {
        var result = Mapped.Json("braintree", """{ "amount": "10.70", "taxAmount": "0.70", "purchaseOrderNumber": "PO-1" }""");

        result.Transaction.Tax.Indicator.ShouldBeNull();
        result.DerivedFields.ShouldBeEmpty();
    }
}
