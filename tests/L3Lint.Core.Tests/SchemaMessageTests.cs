using L3Lint.Core.Schemas;

namespace L3Lint.Core.Tests;

/// <summary>
/// Schema violations reach users as plain messages from resources, never as the validator's own text (NFR-011).
/// Each JSON Schema keyword maps to one message; a canonical payload error names the field's expectation.
/// </summary>
public sealed class SchemaMessageTests
{
    [Theory]
    [InlineData("type", "/rules/only has the wrong type.")]
    [InlineData("enum", "/rules/only is not one of the allowed values.")]
    [InlineData("const", "/rules/only is not one of the allowed values.")]
    [InlineData("pattern", "/rules/only does not have the expected format.")]
    [InlineData("minLength", "/rules/only has the wrong length.")]
    [InlineData("maxLength", "/rules/only has the wrong length.")]
    [InlineData("minimum", "/rules/only is out of range.")]
    [InlineData("maximum", "/rules/only is out of range.")]
    [InlineData("exclusiveMinimum", "/rules/only is out of range.")]
    [InlineData("exclusiveMaximum", "/rules/only is out of range.")]
    [InlineData("multipleOf", "/rules/only has too many decimal places.")]
    [InlineData("required", "A required key is missing at /rules/only.")]
    [InlineData("false", "/rules/only is not a known key. Check the spelling.")]
    [InlineData("", "/rules/only is not a known key. Check the spelling.")]
    [InlineData("propertyNames", "/rules/only is not a known key. Check the spelling.")]
    [InlineData("minItems", "/rules/only has the wrong number of items.")]
    [InlineData("maxItems", "/rules/only has the wrong number of items.")]
    [InlineData("minProperties", "/rules/only has the wrong number of items.")]
    [InlineData("maxProperties", "/rules/only has the wrong number of items.")]
    [InlineData("uniqueItems", "/rules/only does not match the schema.")]
    public void Each_keyword_has_its_own_plain_message(string keyword, string message)
    {
        SchemaMessages.Describe(new SchemaError("/rules/only", keyword, "validator text")).ShouldBe(message);
    }

    [Fact]
    public void The_document_root_is_shown_as_a_slash()
    {
        SchemaMessages.Describe(new SchemaError(string.Empty, "type", "validator text")).ShouldBe("/ has the wrong type.");
        SchemaMessages.DescribeCanonical(new SchemaError(string.Empty, "false", "validator text"))
            .ShouldBe("/ is not a canonical field. Check the spelling against the canonical field dictionary.");
    }

    [Theory]
    [InlineData("/lineItems/0/commodity_code", "false", "/lineItems/0/commodity_code is not a canonical field. Check the spelling against the canonical field dictionary.")]
    [InlineData("/amount", "required", "schemaVersion is required. Send \"1.0\".")]
    [InlineData("/amount", "type", "/amount must be a number.")]
    [InlineData("/lineItems/2/sequence", "type", "/lineItems/2/sequence must be a whole number.")]
    [InlineData("/card/isFederalGovernment", "type", "/card/isFederalGovernment must be true or false.")]
    [InlineData("/transactionDate", "type", "/transactionDate must be a date as YYYY-MM-DD.")]
    [InlineData("/amex/adviceText", "type", "/amex/adviceText must be a list of text lines.")]
    [InlineData("/lineItems", "type", "/lineItems must be a list.")]
    [InlineData("/currency", "type", "/currency must be text.")]
    [InlineData("/card/brand", "enum", "/card/brand must be one of: visa, mastercard, amex, discover, unknown.")]
    [InlineData("/lineItems/0/commodityScheme", "type", "/lineItems/0/commodityScheme must be one of: unspsc, nigp, hs, naics, other, unknown.")]
    [InlineData("/merchant/mcc", "pattern", "/merchant/mcc must be 4 digits.")]
    [InlineData("/merchant/mcc", "type", "/merchant/mcc must be text.")]
    [InlineData("/currency", "minLength", "/currency must be text.")]
    public void Canonical_errors_name_the_field_expectation(string pointer, string keyword, string message)
    {
        SchemaMessages.DescribeCanonical(new SchemaError(pointer, keyword, "validator text")).ShouldBe(message);
    }

    [Fact]
    public void A_canonical_error_outside_the_field_dictionary_uses_the_general_message()
    {
        SchemaMessages.DescribeCanonical(new SchemaError("/merchant", "type", "validator text")).ShouldBe("/merchant has the wrong type.");
    }
}
