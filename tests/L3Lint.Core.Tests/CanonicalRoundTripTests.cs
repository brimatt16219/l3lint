using System.Text.Json.Nodes;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Tests;

/// <summary>Phase 1 exit: the model round-trips the spec's example payload (T-06).</summary>
public sealed class CanonicalRoundTripTests
{
    [Fact]
    public void Spec_example_round_trips_through_the_model()
    {
        var text = SpecExamples.Text("canonical-example.json");
        var document = RawDocument.ParseJson(text, "canonical-example.json");

        var result = new CanonicalAdapter().Map(document, AdapterOptions.Default).ShouldHaveSingleItem();
        var written = CanonicalJson.Serialize(result.Transaction);

        result.InvalidFields.ShouldBeEmpty();
        JsonNode.DeepEquals(JsonNode.Parse(text), JsonNode.Parse(written)).ShouldBeTrue(written);
    }

    [Fact]
    public void Spec_example_maps_every_value()
    {
        var document = RawDocument.ParseJson(SpecExamples.Text("canonical-example.json"), "example");

        var transaction = new CanonicalAdapter().Map(document, AdapterOptions.Default)[0].Transaction;

        transaction.Amount.ShouldBe(280.36m);
        transaction.Currency.ShouldBe("USD");
        transaction.TransactionDate.ShouldBe(new DateOnly(2026, 10, 19));
        transaction.Channel.ShouldBe(Channel.CardNotPresent);
        transaction.Card.Brand.ShouldBe(CardBrand.Visa);
        transaction.Card.Product.ShouldBe(CardProduct.Purchasing);
        transaction.Merchant.Mcc.ShouldBe("5047");
        transaction.Customer.Reference.ShouldBe("PO-77812");
        transaction.Tax.Indicator.ShouldBe(TaxIndicator.Taxable);
        transaction.Amounts.Freight.ShouldBe(15.00m);
        transaction.LineItems.Count.ShouldBe(2);
        transaction.LineItems[0].DiscountAmount.ShouldBe(5.00m);
        transaction.LineItems[1].UnitOfMeasure.ShouldBe("CA");
        transaction.LineItems[1].CommodityScheme.ShouldBe(CommodityScheme.Unspsc);
    }

    [Fact]
    public void Absent_is_not_zero()
    {
        var document = RawDocument.ParseJson("""{ "schemaVersion": "1.0", "amounts": { "freight": 0 } }""", "absent");

        var transaction = new CanonicalAdapter().Map(document, AdapterOptions.Default)[0].Transaction;

        transaction.Amounts.Freight.ShouldBe(0m);
        transaction.Amounts.Duty.ShouldBeNull();
    }

    [Fact]
    public void Wrong_types_are_recorded_not_thrown()
    {
        var document = RawDocument.ParseJson(
            """{ "schemaVersion": "1.0", "amount": "ten", "card": { "brand": "VISA" }, "lineItems": [ { "quantity": true }, 5 ] }""",
            "types");

        var result = new CanonicalAdapter().Map(document, AdapterOptions.Default)[0];

        result.InvalidFields.ShouldBe(["/amount", "/card/brand", "/lineItems/0/quantity", "/lineItems/1"], ignoreOrder: true);
        result.Transaction.Amount.ShouldBeNull();
        result.Transaction.LineItems.Count.ShouldBe(2);
    }

    [Fact]
    public void Decimal_scale_is_preserved()
    {
        var transaction = new Transaction { Amount = 12.50m, Currency = "USD" };

        CanonicalJson.Serialize(transaction).ShouldContain("12.50");
    }
}
