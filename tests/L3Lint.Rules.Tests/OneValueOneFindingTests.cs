using System.Text.Json.Nodes;
using L3Lint.Core.Input;

namespace L3Lint.Rules.Tests;

/// <summary>
/// One bad value gives one finding (ADR-0036): when a value has the wrong type, the schema finding reports it and
/// the arithmetic rules that would need it stay quiet instead of adding a second, misleading finding.
/// </summary>
public sealed class OneValueOneFindingTests
{
    private static readonly string SpecExample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    [Fact]
    public void Invalid_header_tax_gives_only_the_schema_finding()
    {
        var result = Lint(p => p["tax"]!["amount"] = "seventeen");

        result.ShouldContain("GEN-FMT-001");
        result.ShouldNotContain("GEN-ARITH-002");
        result.ShouldNotContain("GEN-ARITH-003");
    }

    [Fact]
    public void Invalid_line_tax_gives_only_the_schema_finding()
    {
        var result = Lint(p => p["lineItems"]![1]!["taxAmount"] = "8,96");

        result.ShouldContain("GEN-FMT-001");
        result.ShouldNotContain("GEN-ARITH-002");
        result.ShouldNotContain("GEN-ARITH-003");
    }

    [Fact]
    public void Invalid_freight_gives_only_the_schema_finding()
    {
        var result = Lint(p => p["amounts"]!["freight"] = true);

        result.ShouldContain("GEN-FMT-001");
        result.ShouldNotContain("GEN-ARITH-002");
    }

    [Fact]
    public void A_wrong_but_valid_tax_amount_still_fails_the_arithmetic()
    {
        // 17.36 is the sum of the line taxes (8.40 + 8.96); 20.00 is a valid number that does not match.
        var result = Lint(p => p["tax"]!["amount"] = 20.00m);

        result.ShouldContain("GEN-ARITH-003");
        result.ShouldContain("GEN-ARITH-002");
        result.ShouldNotContain("GEN-FMT-001");
    }

    private static List<string> Lint(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        mutation(root);
        return [.. Linters.Packs.Lint(RawDocument.ParseJson(root.ToJsonString(), "p.json")).Findings.Select(f => f.RuleId)];
    }
}
