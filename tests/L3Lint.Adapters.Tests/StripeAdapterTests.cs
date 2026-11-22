using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Stripe PaymentIntent adapter (FR-003, FR-007, FR-008).</summary>
public sealed class StripeAdapterTests
{
    private const string Complete = "stripe/01-complete-level3";

    [Theory]
    [InlineData("/amount", "/amount")]
    [InlineData("/currency", "/currency")]
    [InlineData("/tax/amount", "/amount_details/tax/total_tax_amount")]
    [InlineData("/customer/reference", "/payment_details/customer_reference")]
    [InlineData("/order/invoiceNumber", "/payment_details/order_reference")]
    [InlineData("/amounts/freight", "/amount_details/shipping/amount")]
    [InlineData("/amounts/discount", "/amount_details/discount_amount")]
    [InlineData("/shipping/fromPostalCode", "/amount_details/shipping/from_postal_code")]
    [InlineData("/shipping/toPostalCode", "/amount_details/shipping/to_postal_code")]
    [InlineData("/lineItems/0/description", "/amount_details/line_items/0/product_name")]
    [InlineData("/lineItems/1/productCode", "/amount_details/line_items/1/product_code")]
    [InlineData("/lineItems/1/commodityCode", "/amount_details/line_items/1/payment_method_options/card/commodity_code")]
    [InlineData("/lineItems/0/quantity", "/amount_details/line_items/0/quantity")]
    [InlineData("/lineItems/0/unitOfMeasure", "/amount_details/line_items/0/unit_of_measure")]
    [InlineData("/lineItems/0/unitCost", "/amount_details/line_items/0/unit_cost")]
    [InlineData("/lineItems/1/discountAmount", "/amount_details/line_items/1/discount_amount")]
    [InlineData("/lineItems/1/taxAmount", "/amount_details/line_items/1/tax/total_tax_amount")]
    [InlineData("/lineItems", "/amount_details/line_items")]
    [InlineData("/lineItems/1", "/amount_details/line_items/1")]
    public void Path_map_points_at_Stripe_fields(string canonical, string source) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/amounts/duty")]
    [InlineData("/amounts/shippingTax")]
    [InlineData("/shipping/toCountry")]
    [InlineData("/order/orderDate")]
    [InlineData("/tax/indicator")]
    [InlineData("/lineItems/0/total")]
    [InlineData("/lineItems/0/sequence")]
    [InlineData("/lineItems/0/debitCreditIndicator")]
    [InlineData("/card/brand")]
    [InlineData("/merchant/mcc")]
    [InlineData("/amex/adviceText")]
    public void Fields_Stripe_cannot_carry_resolve_to_null(string canonical) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBeNull();

    [Fact]
    public void A_field_the_payload_did_not_send_still_points_where_Stripe_would_carry_it()
    {
        var result = Mapped.Fixture("stripe/03-missing-line-fields");

        result.Transaction.LineItems[1].CommodityCode.ShouldBeNull();
        result.Source("/lineItems/1/commodityCode").ShouldBe("/amount_details/line_items/1/payment_method_options/card/commodity_code");
        result.Source("/lineItems/7/unitOfMeasure").ShouldBe("/amount_details/line_items/7/unit_of_measure");
    }

    [Fact]
    public void Line_totals_and_order_tax_are_derived_and_listed()
    {
        var result = Mapped.Fixture(Complete);

        result.DerivedFields.ShouldBe(["/lineItems/*/total", "/tax/amount", "/tax/indicator"], ignoreOrder: true);
        result.Transaction.LineItems.Select(l => l.Total).ShouldBe([120.00m, 128.00m]);
        result.Transaction.Tax.Amount.ShouldBe(17.36m);
        result.Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Taxable);
        result.Notes.Count(n => n.Kind == AdapterNoteKind.Derived).ShouldBe(3);
    }

    [Fact]
    public void Order_level_tax_is_read_not_derived()
    {
        var result = Mapped.Fixture("stripe/06-order-level-discount-and-tax");

        result.DerivedFields.ShouldNotContain("/tax/amount");
        result.DerivedFields.ShouldContain("/tax/indicator");
        result.Transaction.Tax.Amount.ShouldBe(16.45m);
        result.Transaction.Amounts.Discount.ShouldBe(20.00m);
    }

    [Fact]
    public void Zero_tax_leaves_the_tax_indicator_absent()
    {
        var result = Mapped.Json("stripe", """{ "amount": 1000, "currency": "usd", "amount_details": { "tax": { "total_tax_amount": 0 } } }""");

        result.Transaction.Tax.Amount.ShouldBe(0m);
        result.Transaction.Tax.Indicator.ShouldBeNull();
        result.DerivedFields.ShouldNotContain("/tax/indicator");
    }

    [Fact]
    public void Lowercase_currency_becomes_uppercase_and_writes_back_lowercase()
    {
        var result = Mapped.Fixture(Complete);

        result.Transaction.Currency.ShouldBe("USD");
        result.ToSource("/currency", JsonValue.Create("EUR"))!.GetValue<string>().ShouldBe("eur");
    }

    [Fact]
    public void Amount_patches_are_written_back_as_minor_unit_integers()
    {
        var result = Mapped.Fixture(Complete);

        result.ToSource("/amount", JsonValue.Create(290.36m))!.ToJsonString().ShouldBe("29036");
        result.ToSource("/lineItems/1/discountAmount", JsonValue.Create(0.00m))!.ToJsonString().ShouldBe("0");
        result.ToSource("/lineItems/0/quantity", JsonValue.Create(3)).ShouldNotBeNull().ToJsonString().ShouldBe("3");
    }

    [Fact]
    public void A_line_total_is_not_derived_from_an_invalid_quantity()
    {
        var result = Mapped.Fixture("stripe/08-invalid-types");

        result.InvalidFields.ShouldContain("/lineItems/0/quantity");
        result.InvalidFields.ShouldContain("/lineItems/0/total");
        result.InvalidFields.ShouldContain("/lineItems/1/total");
        result.Notes.Count(n => n.Kind == AdapterNoteKind.Invalid).ShouldBe(3);
    }
}
