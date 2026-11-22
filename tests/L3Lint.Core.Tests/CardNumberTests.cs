using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;

namespace L3Lint.Core.Tests;

/// <summary>
/// The card-number detection algorithm, step by step (Architecture tab "Card-number detection algorithm",
/// ADR-0023). Every number here is a well-known public test card number or a deliberate near miss.
/// </summary>
public sealed class CardNumberTests
{
    [Theory]
    [InlineData("4111111111111111", CardBrand.Visa)]
    [InlineData("4111 1111 1111 1111", CardBrand.Visa)]
    [InlineData("4111-1111-1111-1111", CardBrand.Visa)]
    [InlineData("4222222222222", CardBrand.Visa)]
    [InlineData("5555555555554444", CardBrand.Mastercard)]
    [InlineData("2223003122003222", CardBrand.Mastercard)]
    [InlineData("378282246310005", CardBrand.Amex)]
    [InlineData("371449635398431", CardBrand.Amex)]
    [InlineData("6011111111111117", CardBrand.Discover)]
    [InlineData("6011000990139424", CardBrand.Discover)]
    public void Public_test_numbers_are_detected_with_their_brand(string value, CardBrand expected)
    {
        CardNumberDetector.TryDetect(value, out var digits, out var brand).ShouldBeTrue();
        brand.ShouldBe(expected);
        digits.ShouldBe(value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("4111111111111112")] // Step 3: fails Luhn.
    [InlineData("411111111111")] // Step 2: 12 digits is too short.
    [InlineData("41111111111111111111")] // Step 2: 20 digits is too long.
    [InlineData("9111111111111111")] // Step 4: no major brand starts with 9.
    [InlineData("Gloves 4111111111111111")] // ADR-0023: numbers inside text are not scanned.
    [InlineData("4111.1111.1111.1111")] // Step 2 strips only spaces and hyphens.
    [InlineData("")]
    public void Near_misses_are_not_detected(string value)
    {
        CardNumberDetector.TryDetect(value, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void Masking_keeps_only_the_last_four_digits()
    {
        CardNumberDetector.Mask("4111111111111111").ShouldBe("************1111");
        CardNumberDetector.Mask("378282246310005").ShouldBe("***********0005");
    }

    [Fact]
    public void Scrubbed_document_never_returns_the_original_text()
    {
        const string Json = """{ "id": "A", "customer": { "reference": "4111 1111 1111 1111" }, "lineItems": [ { "productCode": "4111111111111111" } ] }""";
        var document = RawDocument.ParseJson(Json, "p.json");

        var result = CardNumberScrubber.ScrubJson(document, new CanonicalAdapter(), []);

        result.Matches.Count.ShouldBe(2);
        result.Document.IsMasked.ShouldBeTrue();
        result.Document.GetText().ShouldNotContain("4111 1111 1111 1111");
        result.Document.GetText().ShouldNotContain("4111111111111111");
        result.Document.Json.GetRawText().ShouldContain("************1111");

        // Step 5: the product code is a code field, so its match is a warning-level code match.
        result.Matches.Single(m => m.SourcePointer == "/lineItems/0/productCode").IsCodeField.ShouldBeTrue();
        result.Matches.Single(m => m.SourcePointer == "/customer/reference").IsCodeField.ShouldBeFalse();

        // Positions and the hash still come from the original bytes.
        result.Document.Sha256.ShouldBe(document.Sha256);
        document.GetText().ShouldBe(Json);
    }

    [Fact]
    public void Numbers_are_scanned_as_well_as_strings()
    {
        var document = RawDocument.ParseJson("""{ "id": "A", "order": { "invoiceNumber": 4111111111111111 } }""", "p.json");

        var result = CardNumberScrubber.ScrubJson(document, new CanonicalAdapter(), []);

        result.Matches.ShouldHaveSingleItem().SourcePointer.ShouldBe("/order/invoiceNumber");
    }

    [Fact]
    public void Allow_fields_exempt_reviewed_paths()
    {
        var document = RawDocument.ParseJson("""{ "id": "A", "customer": { "reference": "4111111111111111" } }""", "p.json");

        var result = CardNumberScrubber.ScrubJson(document, new CanonicalAdapter(), [new Fields.PointerPattern("/customer/reference")]);

        result.Matches.ShouldBeEmpty();
        result.Document.IsMasked.ShouldBeFalse();
    }

    [Fact]
    public void Clean_document_is_returned_unchanged()
    {
        var document = RawDocument.ParseJson("""{ "id": "A" }""", "p.json");

        var result = CardNumberScrubber.ScrubJson(document, new CanonicalAdapter(), []);

        result.Document.ShouldBeSameAs(document);
        result.Matches.ShouldBeEmpty();
    }
}
