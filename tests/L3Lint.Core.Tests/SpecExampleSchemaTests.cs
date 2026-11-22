using L3Lint.Core.Schemas;

namespace L3Lint.Core.Tests;

/// <summary>Phase 1 exit: every JSON and YAML example in the spec validates against its schema (T-05).</summary>
public sealed class SpecExampleSchemaTests
{
    [Theory]
    [InlineData("canonical", "canonical-example.json")]
    [InlineData("results", "results-example.json")]
    public void Json_examples_validate(string schema, string file)
    {
        var errors = SchemaValidator.For(schema).Validate(SpecExamples.Json(file));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Theory]
    [InlineData("config", "config-example.yaml")]
    [InlineData("rulepack", "rulepack-excerpt.yaml")]
    [InlineData("csvmap", "csvmap-example.yaml")]
    [InlineData("rates", "rates-example.yaml")]
    public void Yaml_examples_validate(string schema, string file)
    {
        var errors = SchemaValidator.For(schema).Validate(SpecExamples.YamlAsJson(file));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Config_schema_rejects_unknown_keys()
    {
        var instance = System.Text.Json.JsonDocument.Parse("""{ "version": 1, "rulez": {} }""").RootElement;

        var errors = SchemaValidator.For("config").Validate(instance);

        errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Canonical_schema_rejects_misspelled_line_field()
    {
        var instance = System.Text.Json.JsonDocument.Parse("""{ "schemaVersion": "1.0", "lineItems": [ { "commodity_code": "42132203" } ] }""").RootElement;

        var errors = SchemaValidator.For("canonical").Validate(instance);

        errors.ShouldContain(e => e.Pointer == "/lineItems/0/commodity_code");
    }

    [Fact]
    public void Every_schema_is_embedded_and_named()
    {
        foreach (var name in SchemaCatalog.Names)
        {
            SchemaCatalog.TryGetText(name, out var text).ShouldBeTrue(name);
            text.ShouldContain("https://json-schema.org/draft/2020-12/schema");
            Should.NotThrow(() => SchemaValidator.For(name));
        }
    }
}
