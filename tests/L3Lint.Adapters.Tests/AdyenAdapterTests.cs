using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;

namespace L3Lint.Adapters.Tests;

/// <summary>Contract tests for the Adyen /payments adapter in both request forms (FR-003, FR-007, FR-008).</summary>
public sealed class AdyenAdapterTests
{
    private const string Structured = "adyen/01-structured-level3";
    private const string Legacy = "adyen/02-legacy-additional-data";
    private const string Esd = "/enhancedSchemeData/levelTwoThree";

    [Theory]
    [InlineData("/amount", "/amount/value")]
    [InlineData("/currency", "/amount/currency")]
    [InlineData("/order/invoiceNumber", "/reference")]
    [InlineData("/customer/reference", Esd + "/customerReferenceNumber")]
    [InlineData("/tax/amount", Esd + "/totalTaxAmount")]
    [InlineData("/amounts/freight", Esd + "/freightAmount")]
    [InlineData("/amounts/duty", Esd + "/dutyAmount")]
    [InlineData("/shipping/fromPostalCode", Esd + "/shipFromPostalCode")]
    [InlineData("/shipping/toPostalCode", Esd + "/destination/postalCode")]
    [InlineData("/shipping/toCountry", Esd + "/destination/countryCode")]
    [InlineData("/order/orderDate", Esd + "/orderDate")]
    [InlineData("/lineItems/1/unitCost", Esd + "/itemDetailLines/1/unitPrice")]
    [InlineData("/lineItems/1/total", Esd + "/itemDetailLines/1/totalAmount")]
    [InlineData("/lineItems/0/discountAmount", Esd + "/itemDetailLines/0/discountAmount")]
    public void Structured_form_path_map_points_at_levelTwoThree(string canonical, string source) =>
        Mapped.Fixture(Structured).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/customer/reference", "/additionalData/enhancedSchemeData.customerReference")]
    [InlineData("/tax/amount", "/additionalData/enhancedSchemeData.totalTaxAmount")]
    [InlineData("/shipping/toPostalCode", "/additionalData/enhancedSchemeData.destinationPostalCode")]
    [InlineData("/lineItems/0/description", "/additionalData/enhancedSchemeData.itemDetailLine1.description")]
    [InlineData("/lineItems/1/total", "/additionalData/enhancedSchemeData.itemDetailLine2.totalAmount")]
    [InlineData("/lineItems/4/quantity", "/additionalData/enhancedSchemeData.itemDetailLine5.quantity")]
    public void Legacy_form_path_map_uses_one_based_item_detail_line_keys(string canonical, string source) =>
        Mapped.Fixture(Legacy).Source(canonical).ShouldBe(source);

    [Theory]
    [InlineData("/amounts/discount")]
    [InlineData("/amounts/shippingTax")]
    [InlineData("/lineItems/0/taxAmount")]
    [InlineData("/tax/indicator")]
    [InlineData("/lineItems/0/debitCreditIndicator")]
    public void Fields_Adyen_cannot_carry_resolve_to_null(string canonical)
    {
        Mapped.Fixture(Structured).Source(canonical).ShouldBeNull();
        Mapped.Fixture(Legacy).Source(canonical).ShouldBeNull();
    }

    [Fact]
    public void Legacy_values_are_written_back_as_strings_in_minor_units_and_ddMMyy()
    {
        var result = Mapped.Fixture(Legacy);

        result.ToSource("/lineItems/1/total", JsonValue.Create(128.00m))!.GetValue<string>().ShouldBe("12800");
        result.ToSource("/order/orderDate", JsonValue.Create("2026-10-15"))!.GetValue<string>().ShouldBe("151026");
        result.ToSource("/lineItems/0/quantity", JsonValue.Create(10m))!.GetValue<string>().ShouldBe("10");
    }

    [Fact]
    public void An_alpha3_destination_maps_to_alpha2_and_writes_back_as_alpha3()
    {
        var result = Mapped.Fixture(Legacy);

        result.Transaction.Shipping.ToCountry.ShouldBe("US");
        result.ToSource("/shipping/toCountry", JsonValue.Create("CA"))!.GetValue<string>().ShouldBe("CAN");
        Mapped.Fixture(Structured).ToSource("/shipping/toCountry", JsonValue.Create("CA"))!.GetValue<string>().ShouldBe("CA");
    }

    [Fact]
    public void Structured_amounts_are_written_back_as_minor_unit_integers()
    {
        Mapped.Fixture(Structured).ToSource("/tax/amount", JsonValue.Create(17.36m))!.ToJsonString().ShouldBe("1736");
        Mapped.Fixture("adyen/04-jpy-zero-decimals").ToSource("/amount", JsonValue.Create(27500m))!.ToJsonString().ShouldBe("27500");
    }

    [Fact]
    public void Both_forms_present_uses_the_structured_object_with_an_ambiguous_note()
    {
        var result = Mapped.Fixture("adyen/10-both-forms");

        result.Transaction.Customer.Reference.ShouldBe("PO-48820");
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.SourcePointer == "/additionalData");
    }

    [Fact]
    public void A_gap_in_legacy_line_numbers_is_noted_and_lines_follow_number_order()
    {
        var result = Mapped.Fixture("adyen/06-legacy-line-gap");

        result.Transaction.LineItems.Select(l => l.Sequence).ShouldBe([1, 2]);
        result.Source("/lineItems/1/total").ShouldBe("/additionalData/enhancedSchemeData.itemDetailLine3.totalAmount");
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer == "/lineItems");
    }

    [Fact]
    public void Duty_above_zero_adds_a_note_because_Adyen_leaves_it_out_of_the_amount()
    {
        var result = Mapped.Json("adyen", """{ "merchantAccount": "M", "amount": { "currency": "USD", "value": 1000 }, "enhancedSchemeData": { "levelTwoThree": { "dutyAmount": 150 } } }""");

        result.Transaction.Amounts.Duty.ShouldBe(1.50m);
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ambiguous && n.CanonicalPointer == "/amounts/duty");
    }

    [Fact]
    public void Tax_indicator_is_derived_from_tax_above_zero()
    {
        var result = Mapped.Fixture(Structured);

        result.DerivedFields.ShouldBe(["/tax/indicator"]);
    }
}
