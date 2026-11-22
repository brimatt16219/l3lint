using System.Text.Json;
using L3Lint.Core.Fields;
using L3Lint.Core.Schemas;

namespace L3Lint.Core.Tests;

public sealed class CanonicalFieldsTests
{
    [Fact]
    public void Dictionary_has_the_52_spec_fields()
    {
        CanonicalFields.All.Count.ShouldBe(52);
        CanonicalFields.All.Count(f => f.IsLineField).ShouldBe(18);
        CanonicalFields.All.Select(f => f.Path).ShouldBeUnique();
    }

    [Fact]
    public void Every_field_is_declared_in_the_canonical_schema()
    {
        using var schema = JsonDocument.Parse(SchemaCatalog.GetText("canonical"));
        var root = schema.RootElement.GetProperty("properties");
        var line = schema.RootElement.GetProperty("$defs").GetProperty("lineItem").GetProperty("properties");

        foreach (var field in CanonicalFields.All)
        {
            var segments = field.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (field.IsLineField)
            {
                line.TryGetProperty(segments[^1], out _).ShouldBeTrue(field.Path);
            }
            else if (segments.Length == 1)
            {
                root.TryGetProperty(segments[0], out _).ShouldBeTrue(field.Path);
            }
            else
            {
                root.GetProperty(segments[0]).GetProperty("properties").TryGetProperty(segments[1], out _).ShouldBeTrue(field.Path);
            }
        }
    }

    [Theory]
    [InlineData("/lineItems/12/total", "/lineItems/*/total")]
    [InlineData("/tax/amount", "/tax/amount")]
    [InlineData("/amex/adviceText/3", "/amex/adviceText/*")]
    [InlineData("/lineItems", "/lineItems")]
    public void Concrete_pointers_resolve_to_fields(string pointer, string expected)
    {
        CanonicalFields.FindByPointer(pointer)!.Path.ShouldBe(expected);
    }

    [Fact]
    public void Unknown_pointers_resolve_to_nothing()
    {
        CanonicalFields.FindByPointer("/lineItems/0/commodity_code").ShouldBeNull();
        CanonicalFields.FindByPointer("/nope").ShouldBeNull();
    }

    [Fact]
    public void Code_fields_are_product_commodity_and_sequence()
    {
        CanonicalFields.All.Where(f => f.IsCode).Select(f => f.Name).ShouldBe(["sequence", "productCode", "commodityCode"]);
    }
}
