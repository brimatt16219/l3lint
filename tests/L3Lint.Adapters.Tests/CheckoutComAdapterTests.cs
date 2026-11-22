using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Checkout.com payments adapter, including the corrected paths (FR-004, FR-007, FR-008).</summary>
public sealed class CheckoutComAdapterTests
{
    private const string Complete = "checkoutcom/01-complete-level3";

    [Theory]
    [InlineData("/amount", "/amount")]
    [InlineData("/currency", "/currency")]
    [InlineData("/tax/amount", "/processing/tax_amount")]
    [InlineData("/customer/reference", "/processing/order_id")]
    [InlineData("/order/invoiceNumber", "/reference")]
    [InlineData("/amounts/freight", "/processing/shipping_amount")]
    [InlineData("/amounts/duty", "/processing/duty_amount")]
    [InlineData("/amounts/discount", "/processing/discount_amount")]
    [InlineData("/amounts/shippingTax", "/processing/shipping_tax_amount")]
    [InlineData("/shipping/fromPostalCode", "/shipping/from_address_zip")]
    [InlineData("/shipping/toPostalCode", "/shipping/address/zip")]
    [InlineData("/shipping/toCountry", "/shipping/address/country")]
    [InlineData("/lineItems/0/productCode", "/items/0/reference")]
    [InlineData("/lineItems/1/unitCost", "/items/1/unit_price")]
    [InlineData("/lineItems/1/total", "/items/1/total_amount")]
    [InlineData("/lineItems/0/taxable", "/items/0/tax_exempt")]
    public void Path_map_points_at_Checkout_fields(string canonical, string source) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/tax/indicator")]
    [InlineData("/order/orderDate")]
    [InlineData("/lineItems/0/debitCreditIndicator")]
    [InlineData("/lineItems/0/amountIncludesTax")]
    public void Fields_Checkout_cannot_carry_resolve_to_null(string canonical) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBeNull();

    [Fact]
    public void Tax_exempt_is_the_opposite_of_the_line_taxable_flag_both_ways()
    {
        var result = Mapped.Fixture("checkoutcom/06-line-tax-exempt");

        result.Transaction.LineItems.Select(l => l.Taxable).ShouldBe([false, false]);
        result.ToSource("/lineItems/0/taxable", JsonValue.Create(true))!.ToJsonString().ShouldBe("false");
    }

    [Fact]
    public void Zero_tax_never_becomes_an_exemption()
    {
        var result = Mapped.Fixture("checkoutcom/06-line-tax-exempt");

        result.Transaction.Tax.Amount.ShouldBe(0m);
        result.Transaction.Tax.Indicator.ShouldBeNull();
    }

    [Fact]
    public void Amounts_are_written_back_in_minor_units_of_the_currency()
    {
        Mapped.Fixture(Complete).ToSource("/amounts/shippingTax", JsonValue.Create(1.25m))!.ToJsonString().ShouldBe("125");
        Mapped.Fixture("checkoutcom/05-bhd-three-decimals").ToSource("/amount", JsonValue.Create(19.19m))!.ToJsonString().ShouldBe("19190");
        Mapped.Fixture("checkoutcom/04-jpy-zero-decimals").ToSource("/amount", JsonValue.Create(27500m))!.ToJsonString().ShouldBe("27500");
    }

    [Fact]
    public void Duty_and_shipping_tax_above_zero_add_ambiguous_notes()
    {
        var result = Mapped.Json("checkoutcom", """{ "amount": 1000, "currency": "USD", "processing": { "duty_amount": 100, "shipping_tax_amount": 50 } }""");

        result.Notes.Count(n => n.Kind == AdapterNoteKind.Ambiguous).ShouldBe(2);
        result.Transaction.Amounts.Duty.ShouldBe(1.00m);
        result.Transaction.Amounts.ShippingTax.ShouldBe(0.50m);
    }

    [Fact]
    public void Tax_above_zero_marks_the_sale_taxable_as_a_derived_value()
    {
        var result = Mapped.Fixture(Complete);

        result.Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Taxable);
        result.DerivedFields.ShouldBe(["/tax/indicator"]);
    }
}
