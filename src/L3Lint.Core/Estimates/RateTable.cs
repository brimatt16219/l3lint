using System.Globalization;
using System.Text.Json;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Core.Estimates;

/// <summary>
/// A user-maintained interchange rate table for cost estimates (FR-028, US-10). l3lint ships no rates.
/// </summary>
/// <remarks>Percent values are in percent: 1.75 means 1.75%. Estimates are always labeled as estimates.</remarks>
public sealed record RateTable
{
    /// <summary>Gets the table's name, printed with every estimate.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the date the rates took effect, printed with every estimate.</summary>
    public required DateOnly Effective { get; init; }

    /// <summary>Gets the ISO 4217 currency of fixed fees and estimates.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets program fees such as Visa's CEDP participation fee.</summary>
    public IReadOnlyList<ProgramFee> ProgramFees { get; init; } = [];

    /// <summary>Gets the interchange categories.</summary>
    public IReadOnlyList<RateCategory> Categories { get; init; } = [];

    /// <summary>
    /// Parses a rate table file.
    /// </summary>
    /// <param name="yaml">The YAML text.</param>
    /// <param name="sourceName">The file name, used in errors.</param>
    /// <returns>The rate table.</returns>
    /// <exception cref="RulePackException">The file is not valid YAML or does not match <c>rates.schema.json</c>.</exception>
    public static RateTable Parse(string yaml, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        JsonElement root;
        try
        {
            root = JsonElementFromYaml.Convert(YamlJson.Parse(yaml));
        }
        catch (YamlFormatException ex)
        {
            throw new RulePackException(ex.Message, sourceName, sourceName, null, ex);
        }

        var errors = SchemaValidator.For("rates").Validate(root);
        if (errors.Count > 0)
        {
            throw new RulePackException(
                string.Format(CultureInfo.InvariantCulture, Resources.Strings.RatesInvalid, sourceName, errors[0].Pointer, SchemaMessages.Describe(errors[0])),
                sourceName,
                sourceName,
                errors[0].Pointer);
        }

        return new RateTable
        {
            Name = root.GetProperty("name").GetString()!,
            Effective = DateOnly.ParseExact(root.GetProperty("effective").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Currency = root.GetProperty("currency").GetString()!,
            ProgramFees = root.TryGetProperty("programFees", out var fees)
                ? [.. fees.EnumerateArray().Select(f => new ProgramFee(Brand(f), f.GetProperty("when").GetString()!, f.GetProperty("percent").GetDecimal(), Optional(f, "fixed")))]
                : [],
            Categories = [.. root.GetProperty("categories").EnumerateArray().Select(c => new RateCategory(
                Brand(c),
                c.TryGetProperty("products", out var products) ? [.. products.EnumerateArray().Select(Product)] : [],
                c.GetProperty("tier").GetString()!,
                c.GetProperty("percent").GetDecimal(),
                Optional(c, "fixed"),
                c.TryGetProperty("name", out var name) ? name.GetString() : null))],
        };
    }

    private static CardBrand Brand(JsonElement element)
    {
        return CanonicalNames.TryParse(element.GetProperty("brand").GetString(), out CardBrand brand) ? brand : CardBrand.Unknown;
    }

    private static CardProduct Product(JsonElement element)
    {
        return CanonicalNames.TryParse(element.GetString(), out CardProduct product) ? product : CardProduct.Unknown;
    }

    private static decimal Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.GetDecimal() : 0m;
}
