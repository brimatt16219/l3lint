using L3Lint.Core;
using L3Lint.Core.Model;

namespace L3Lint.Rules.Tests;

/// <summary>Every rule's pass example passes and fail example fails (FR-020, Test strategy "Rule examples").</summary>
public sealed class RuleExampleTests
{
    public static TheoryData<string> RuleIds()
    {
        var data = new TheoryData<string>();
        foreach (var rule in Linters.Packs.Rules())
        {
            data.Add(rule.Id);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void Rule_examples_behave(string ruleId)
    {
        var outcome = RuleExampleTester.Run(Linters.Packs, Linters.Packs.Explain(ruleId));

        outcome.Passed.ShouldBeTrue(outcome.Detail);
    }

    [Fact]
    public void Catalog_has_78_rules_split_as_the_spec_says()
    {
        var rules = Linters.Packs.Rules();

        rules.Count.ShouldBe(78);
        rules.Count(r => r.Brand is null).ShouldBe(23);
        rules.Count(r => r.Brand == CardBrand.Visa).ShouldBe(26);
        rules.Count(r => r.Brand == CardBrand.Mastercard).ShouldBe(17);
        rules.Count(r => r.Brand == CardBrand.Amex).ShouldBe(9);
        rules.Count(r => r.Brand == CardBrand.Discover).ShouldBe(3);
    }

    [Fact]
    public void Every_rule_has_sources_examples_and_text()
    {
        foreach (var rule in Linters.Packs.Rules())
        {
            rule.Sources.ShouldNotBeEmpty(rule.Id);
            rule.Sources.ShouldAllBe(s => s.Scheme == "https");
            rule.Examples.ShouldNotBeNull(rule.Id);
            rule.Fix.ShouldNotBeNullOrWhiteSpace(rule.Id);
            rule.Requirement.ShouldNotBeNullOrWhiteSpace(rule.Id);
            rule.Rationale.ShouldNotBeNullOrWhiteSpace(rule.Id);
        }
    }

    [Fact]
    public void Five_packs_are_loaded_at_version_1_0_0()
    {
        Linters.Packs.RulePacks.Select(p => p.Id).ShouldBe(
            ["l3lint.rules.amex", "l3lint.rules.discover", "l3lint.rules.general", "l3lint.rules.mastercard", "l3lint.rules.visa"]);
        Linters.Packs.RulePacks.ShouldAllBe(p => p.Version == "1.0.0");
    }
}
