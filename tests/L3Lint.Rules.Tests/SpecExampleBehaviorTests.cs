using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Tiers;

namespace L3Lint.Rules.Tests;

/// <summary>
/// Phase 2 exit: the spec's example payload behaves exactly as the Card brand rules tab says, and the worked
/// arithmetic example reproduces.
/// </summary>
public sealed class SpecExampleBehaviorTests
{
    private static RawDocument Example() =>
        RawDocument.ParseJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json")), "canonical-example.json");

    [Fact]
    public void Spec_example_raises_exactly_one_visa_warning_VISA_L3_010()
    {
        var result = Linters.Packs.Lint(Example());

        var visaFindings = result.Findings.Where(f => f.Brand == CardBrand.Visa).ToList();
        visaFindings.ShouldHaveSingleItem().RuleId.ShouldBe("VISA-L3-010");
        visaFindings[0].Severity.ShouldBe(Severity.Warning);
        result.Findings.ShouldNotContain(f => f.Severity == Severity.Error);
        result.Findings.ShouldNotContain(f => f.Brand == null && f.Severity >= Severity.Warning);
    }

    [Fact]
    public void Spec_example_meets_product3_under_visa()
    {
        var tier = Linters.Packs.Lint(Example()).Transactions.ShouldHaveSingleItem().PrimaryTier!;

        tier.Best.ShouldBe("product3");
        tier.Likely.ShouldBe("product3");
        tier.Status.ShouldBe(TierStatus.Meets);
    }

    [Fact]
    public void Spec_example_fails_MC_L3_005_under_mastercard()
    {
        var result = Linters.Packs.Lint(Example(), new LintOptions { Brand = CardBrand.Mastercard });

        var units = result.Findings.Where(f => f.RuleId == "MC-L3-005").ToList();
        units.Select(f => f.Path.Canonical).ShouldBe(["/lineItems/0/unitOfMeasure", "/lineItems/1/unitOfMeasure"]);
        units.ShouldAllBe(f => f.Severity == Severity.Error);
        units.Select(f => f.Actual).ShouldBe(["BX", "CA"]);
    }

    [Fact]
    public void Mastercard_fix_proposes_PCE_for_BX_and_CA_and_asks_a_person()
    {
        var result = Linters.Packs.Lint(Example(), new LintOptions { Brand = CardBrand.Mastercard });

        foreach (var finding in result.Findings.Where(f => f.RuleId == "MC-L3-005"))
        {
            finding.NeedsHuman.ShouldBeTrue();
            finding.Patch.ShouldNotBeNull();
            finding.Patch.Operations.ShouldHaveSingleItem().Value!.GetValue<string>().ShouldBe("PCE");
        }
    }

    [Fact]
    public void Worked_example_line_and_total_arithmetic_pass()
    {
        var result = Linters.Packs.Lint(Example(), new LintOptions { OnlyRules = new HashSet<string> { "GEN-ARITH-*" } });

        result.Findings.ShouldBeEmpty();
    }

    [Fact]
    public void Worked_example_forgotten_discount_reports_expected_reported_and_difference()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"))
            .Replace("\"total\": 120.00", "\"total\": 125.00", StringComparison.Ordinal);

        var result = Linters.Packs.Lint(RawDocument.ParseJson(text, "forgotten-discount"), new LintOptions { OnlyRules = new HashSet<string> { "GEN-ARITH-*" } });

        var line = result.Findings.Where(f => f.RuleId == "GEN-ARITH-001").ShouldHaveSingleItem();
        line.Path.Canonical.ShouldBe("/lineItems/0/total");
        line.Expected.ShouldBe("120.00");
        line.Actual.ShouldBe("125.00");
        line.Message.ShouldContain("difference 5.00");

        var total = result.Findings.Where(f => f.RuleId == "GEN-ARITH-002").ShouldHaveSingleItem();
        total.Message.ShouldContain("A line total is likely wrong");
    }
}
