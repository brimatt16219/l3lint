using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Cybersource REST payments adapter, including the corrected paths (FR-004, FR-007).</summary>
public sealed class CybersourceAdapterTests
{
    private const string Complete = "cybersource/01-complete-level3";
    private const string Order = "/orderInformation";

    [Theory]
    [InlineData("/amount", Order + "/amountDetails/totalAmount")]
    [InlineData("/currency", Order + "/amountDetails/currency")]
    [InlineData("/tax/amount", Order + "/amountDetails/taxAmount")]
    [InlineData("/tax/indicator", Order + "/invoiceDetails/taxable")]
    [InlineData("/customer/reference", Order + "/invoiceDetails/purchaseOrderNumber")]
    [InlineData("/order/invoiceNumber", "/clientReferenceInformation/code")]
    [InlineData("/amounts/freight", Order + "/amountDetails/freightAmount")]
    [InlineData("/amounts/duty", Order + "/amountDetails/dutyAmount")]
    [InlineData("/amounts/discount", Order + "/amountDetails/discountAmount")]
    [InlineData("/shipping/fromPostalCode", Order + "/shippingDetails/shipFromPostalCode")]
    [InlineData("/shipping/toPostalCode", Order + "/shipTo/postalCode")]
    [InlineData("/shipping/toCountry", Order + "/shipTo/country")]
    [InlineData("/billing/postalCode", Order + "/billTo/postalCode")]
    [InlineData("/lineItems/0/description", Order + "/lineItems/0/productName")]
    [InlineData("/lineItems/1/productCode", Order + "/lineItems/1/productCode")]
    [InlineData("/lineItems/1/unitCost", Order + "/lineItems/1/unitPrice")]
    [InlineData("/lineItems/1/amountIncludesTax", Order + "/lineItems/1/amountIncludesTax")]
    public void Path_map_points_at_Cybersource_fields(string canonical, string source) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/amounts/shippingTax")]
    [InlineData("/lineItems/0/debitCreditIndicator")]
    public void Fields_Cybersource_cannot_carry_resolve_to_null(string canonical) =>
        Mapped.Fixture(Complete).Source(canonical).ShouldBeNull();

    [Theory]
    [InlineData("taxable", "true")]
    [InlineData("exempt", "false")]
    public void Tax_indicator_writes_back_as_the_taxable_flag(string indicator, string expected) =>
        Mapped.Fixture(Complete).ToSource("/tax/indicator", JsonValue.Create(indicator))!.ToJsonString().ShouldBe(expected);

    [Fact]
    public void Header_tax_is_the_sum_of_line_taxes_when_not_sent()
    {
        var result = Mapped.Fixture("cybersource/03-tax-from-lines");

        result.Transaction.Tax.Amount.ShouldBe(14.84m);
        result.DerivedFields.ShouldBe(["/tax/amount"]);
        result.Transaction.Tax.Indicator.ShouldBeNull();
    }

    [Fact]
    public void Product_sku_is_used_when_product_code_is_missing_and_the_path_follows_it()
    {
        var result = Mapped.Fixture("cybersource/06-product-sku");

        result.Transaction.LineItems[0].ProductCode.ShouldBe("GLV-NL-100");
        result.Source("/lineItems/0/productCode").ShouldBe(Order + "/lineItems/0/productSku");
        result.Source("/lineItems/1/productCode").ShouldBe(Order + "/lineItems/1/productCode");
        result.Notes.Count(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer is "/lineItems/0/productCode" or "/lineItems/1/productCode").ShouldBe(2);
    }

    [Fact]
    public void Taxable_false_with_tax_maps_to_exempt_with_an_ambiguous_note()
    {
        var result = Mapped.Fixture("cybersource/10-taxable-false-with-tax");

        result.Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Exempt);
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer == "/tax/indicator");
    }

    [Fact]
    public void Purchase_order_date_is_not_mapped_because_the_spec_lists_the_order_date_as_not_supported()
    {
        var result = Mapped.Fixture(Complete);

        result.Transaction.Order.OrderDate.ShouldBeNull();
        result.Transaction.Order.InvoiceNumber.ShouldBe("INV-50311");
    }

    [Fact]
    public void Amounts_are_written_back_as_decimal_strings() =>
        Mapped.Fixture(Complete).ToSource("/amounts/freight", JsonValue.Create(15.00m))!.GetValue<string>().ShouldBe("15.00");
}
