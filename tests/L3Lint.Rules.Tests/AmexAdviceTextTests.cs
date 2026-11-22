using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Input;

namespace L3Lint.Rules.Tests;

/// <summary>
/// AMEX-FMT-001 beyond its two examples: a line over 40 characters is reported per line with a trim patch
/// (J.P. Morgan: 4 lines of 40 characters; ADR-0022 lists trimming as a mechanical fix).
/// </summary>
public sealed class AmexAdviceTextTests
{
    // The rule's own pass example: an Amex corporate payload with two short advice lines.
    private static readonly string Pass = Linters.Packs.Explain("AMEX-FMT-001").Examples!.PassJson;

    [Fact]
    public void A_line_over_40_characters_is_reported_with_a_trim_patch()
    {
        var long45 = new string('A', 45);
        var findings = Lint(p => p["amex"]!["adviceText"] = new JsonArray("Gloves and gowns", long45));

        var finding = findings.Single(f => f.RuleId == "AMEX-FMT-001");
        finding.Path.Canonical.ShouldBe("/amex/adviceText/1");
        finding.Message.ShouldBe("Advice text line 2 is 45 characters; Amex accepts at most 40.");
        var operation = finding.Patch.ShouldNotBeNull().Operations.ShouldHaveSingleItem();
        operation.Path.ShouldBe("/amex/adviceText/1");
        operation.Value!.GetValue<string>().ShouldBe(new string('A', 40));
    }

    [Fact]
    public void Exactly_40_characters_on_4_lines_passes()
    {
        var line40 = new string('B', 40);
        var findings = Lint(p => p["amex"]!["adviceText"] = new JsonArray(line40, line40, line40, line40));

        findings.ShouldNotContain(f => f.RuleId == "AMEX-FMT-001");
    }

    [Fact]
    public void Too_many_lines_and_a_long_line_give_two_findings()
    {
        var findings = Lint(p => p["amex"]!["adviceText"] = new JsonArray("a", "b", "c", "d", new string('C', 41)));

        findings.Count(f => f.RuleId == "AMEX-FMT-001").ShouldBe(2);
        findings.ShouldContain(f => f.RuleId == "AMEX-FMT-001" && f.Path.Canonical == "/amex/adviceText");
    }

    private static List<Finding> Lint(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(Pass)!.AsObject();
        root["amex"] ??= new JsonObject();
        mutation(root);
        return [.. Linters.Packs.Lint(RawDocument.ParseJson(root.ToJsonString(), "amex.json")).Findings];
    }
}
