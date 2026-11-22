using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Tests;

/// <summary>
/// Adapters never throw for payload content (ADR-0036): values of the wrong type become Invalid notes and stay absent,
/// and ignored fields are reported only with verbose output.
/// </summary>
public sealed class TolerantMappingTests
{
    public static TheoryData<string> JsonFixtures() =>
        [.. Fixture.AllIds().Where(id => !id.StartsWith("csv/", StringComparison.Ordinal) && !Fixture.Load(id).IsTie)];

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void Replacing_every_value_with_the_wrong_type_gives_notes_not_exceptions(string id)
    {
        var fixture = Fixture.Load(id);
        var adapter = Linters.Adapter(fixture.Format);
        var original = JsonNode.Parse(File.ReadAllText(fixture.PathOf(".json")))!;

        foreach (var replacement in new Func<JsonNode?>[] { () => new JsonObject(), () => new JsonArray(), () => JsonValue.Create("x"), () => JsonValue.Create(12.5m), () => JsonValue.Create(true), () => null })
        {
            var mutated = Replace(original.DeepClone(), replacement);
            var document = RawDocument.ParseJson(mutated!.ToJsonString(), id);

            var results = adapter.Map(document, fixture.AdapterOptions());

            results.Count.ShouldBe(1);
            foreach (var pointer in results[0].InvalidFields)
            {
                results[0].Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Invalid && n.CanonicalPointer != null, pointer);
            }
        }
    }

    [Theory]
    [InlineData("stripe", """{ "amount": "12.50", "currency": 840, "amount_details": "lines", "payment_details": [] }""")]
    [InlineData("adyen", """{ "merchantAccount": "M", "amount": 12, "enhancedSchemeData": { "levelTwoThree": { "itemDetailLines": {} } } }""")]
    [InlineData("braintree", """{ "amount": true, "taxAmount": {}, "lineItems": [ 1, "two", null ], "purchaseOrderNumber": 5 }""")]
    [InlineData("cybersource", """{ "orderInformation": { "amountDetails": [], "lineItems": [ { "quantity": "x" } ] } }""")]
    [InlineData("authorizenet", """{ "createTransactionRequest": { "transactionRequest": { "amount": "abc", "lineItems": { "lineItem": 7 } } } }""")]
    [InlineData("checkoutcom", """{ "amount": "1000", "currency": "USD", "processing": "none", "items": [ { "quantity": 1.5 } ] }""")]
    public void Hostile_payloads_map_with_invalid_notes(string adapter, string json)
    {
        var result = Mapped.Json(adapter, json);

        result.InvalidFields.ShouldNotBeEmpty();
        result.Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Invalid);
        result.Notes.Where(n => n.Kind == AdapterNoteKind.Invalid).ShouldAllBe(n => n.SourcePointer != null && n.Message.Length > 0);
    }

    [Fact]
    public void A_line_that_is_not_an_object_keeps_its_place_and_is_invalid_as_a_whole()
    {
        var result = Mapped.Json("braintree", """{ "amount": "1.00", "taxAmount": "0", "purchaseOrderNumber": "P", "lineItems": [ "oops", { "name": "Second", "kind": "debit" } ] }""");

        result.Transaction.LineItems.Count.ShouldBe(2);
        result.Transaction.LineItems.Select(l => l.Sequence).ShouldBe([1, 2]);
        result.InvalidFields.ShouldContain("/lineItems/0");
        result.Transaction.LineItems[1].Description.ShouldBe("Second");
    }

    [Fact]
    public void One_wrong_container_is_one_note_even_when_it_holds_several_fields()
    {
        var result = Mapped.Json("checkoutcom", """{ "amount": 100, "currency": "USD", "processing": {}, "shipping": "nowhere" }""");

        result.Notes.Count(n => n.Kind == AdapterNoteKind.Invalid).ShouldBe(1);
        result.InvalidFields.ShouldBe(["/shipping/fromPostalCode", "/shipping/toPostalCode", "/shipping/toCountry"], ignoreOrder: true);
    }

    [Fact]
    public void Invalid_note_messages_name_the_source_field_and_never_echo_the_value()
    {
        var result = Mapped.Json("stripe", """{ "amount": "secret-value-123", "currency": "usd", "amount_details": {} }""");

        var note = result.Notes.Single(n => n.Kind == AdapterNoteKind.Invalid);
        note.SourcePointer.ShouldBe("/amount");
        note.CanonicalPointer.ShouldBe("/amount");
        note.Message.ShouldContain("/amount");
        note.Message.ShouldNotContain("secret-value-123");
    }

    [Fact]
    public void Ignored_fields_are_reported_only_with_verbose()
    {
        var quiet = Mapped.Fixture("stripe/01-complete-level3");
        var verbose = Mapped.Fixture("stripe/01-complete-level3", verbose: true);

        quiet.Notes.ShouldNotContain(n => n.Kind == AdapterNoteKind.Ignored);
        verbose.Notes.Where(n => n.Kind == AdapterNoteKind.Ignored).Select(n => n.SourcePointer)
            .ShouldBe(["/automatic_payment_methods/enabled", "/expand/0"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("adyen/01-structured-level3", "/merchantAccount")]
    [InlineData("braintree/01-complete-level3", "/paymentMethodNonce")]
    [InlineData("cybersource/01-complete-level3", "/processingInformation/purchaseLevel")]
    [InlineData("authorizenet/01-complete-level3", "/createTransactionRequest/refId")]
    [InlineData("checkoutcom/01-complete-level3", "/processing_channel_id")]
    public void Verbose_mapping_lists_gateway_fields_without_a_canonical_equivalent(string id, string ignored) =>
        Mapped.Fixture(id, verbose: true).Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ignored && n.SourcePointer == ignored);

    [Theory]
    [InlineData("stripe")]
    [InlineData("adyen")]
    [InlineData("braintree")]
    [InlineData("cybersource")]
    [InlineData("authorizenet")]
    [InlineData("checkoutcom")]
    public void Json_adapters_refuse_csv_documents_with_an_input_error(string adapter)
    {
        var document = RawDocument.FromCsv("a,b\n1,2\n"u8.ToArray(), "x.csv");

        var error = Should.Throw<InputException>(() => Linters.Adapter(adapter).Map(document, AdapterOptions.Default));

        error.Kind.ShouldBe(InputErrorKind.Unsupported);
        Linters.Adapter(adapter).Detect(document).ShouldBe(0.0);
    }

    private static JsonNode? Replace(JsonNode? node, Func<JsonNode?> replacement)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    var child = obj[key];
                    var replaced = Replace(child, replacement);
                    if (!ReferenceEquals(child, replaced))
                    {
                        obj[key] = replaced;
                    }
                }

                return obj;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    var child = array[i];
                    var replaced = Replace(child, replacement);
                    if (!ReferenceEquals(child, replaced))
                    {
                        array[i] = replaced;
                    }
                }

                return array;
            case JsonValue value when value.GetValueKind() is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False:
                return replacement();
            default:
                return node;
        }
    }
}
