using System.Globalization;
using System.Text;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;

namespace L3Lint.Corpus.Tests;

/// <summary>
/// The golden corpus gates (Test and delivery tab: recall at least 95% on seeded defects, false positives at most 2%
/// on known-good payloads; Product requirements success metrics). How the two rates are counted is in ADR-0057.
/// </summary>
public sealed class CorpusTests(ITestOutputHelper output)
{
    public static TheoryData<string> RuleExamples => [.. Fixture.Load("canonical").Select(f => f.Name)];

    public static TheoryData<string> PciFixtures => [.. Fixture.Load("pci").Select(f => f.Name)];

    [Theory]
    [MemberData(nameof(RuleExamples))]
    public void Every_rule_example_fixture_meets_its_expectation(string name)
    {
        var outcome = CorpusRunner.Run(Fixture.Read(Path.Combine(Fixture.RepositoryRoot, "fixtures", name)));

        outcome.Passed.ShouldBeTrue(outcome.Explain());
    }

    [Fact]
    public void Rule_example_set_has_one_pass_and_one_fail_per_rule()
    {
        var fixtures = Fixture.Load("canonical");

        fixtures.Count.ShouldBe(156);
        fixtures.Select(f => f.Name.Split('/')[^1]).Distinct().Count().ShouldBe(78);
        CorpusRunner.Linter.Rules().Count.ShouldBe(78);
    }

    [Fact]
    public void Seeded_defects_are_found_at_least_95_percent_of_the_time()
    {
        var fixtures = Fixture.Load("seeded");
        var outcomes = fixtures.Select(CorpusRunner.Run).ToList();

        var expected = outcomes.Sum(o => o.Fixture.Findings.Count);
        var missed = outcomes.Sum(o => o.Missing.Count);
        var recall = (expected - missed) / (double)expected;
        foreach (var group in outcomes.GroupBy(o => o.Fixture.Operator).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var groupExpected = group.Sum(o => o.Fixture.Findings.Count);
            var groupMissed = group.Sum(o => o.Missing.Count);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{group.Key,-28} expected {groupExpected,3}  found {groupExpected - groupMissed,3}"));
        }

        foreach (var outcome in outcomes.Where(o => o.Missing.Count > 0))
        {
            output.WriteLine("missed: " + outcome.Explain());
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"recall {recall:P2} ({expected - missed} of {expected})"));
        fixtures.Count.ShouldBe(400);
        recall.ShouldBeGreaterThanOrEqualTo(0.95);
    }

    [Fact]
    public void Seeded_defects_do_not_trip_rules_they_must_not_trip()
    {
        var outcomes = Fixture.Load("seeded").Select(CorpusRunner.Run).ToList();

        var violations = outcomes.Where(o => o.Unexpected.Count > 0 || o.ExitMismatch is not null).ToList();
        foreach (var outcome in violations)
        {
            output.WriteLine("violation: " + outcome.Explain());
        }

        (violations.Count / (double)outcomes.Count).ShouldBeLessThanOrEqualTo(0.02);
    }

    [Fact]
    public void Known_good_payloads_give_at_most_2_percent_false_positives()
    {
        var fixtures = Fixture.Load("known-good");
        var outcomes = fixtures.Select(CorpusRunner.Run).ToList();

        // A known-good payload counts as a false positive when it gets any error or warning, the wrong exit code
        // or the wrong likely tier (ADR-0057).
        var falsePositives = outcomes.Where(o => o.Defects.Any() || !o.Passed).ToList();
        foreach (var outcome in falsePositives)
        {
            output.WriteLine("false positive: " + outcome.Explain());
        }

        var rate = falsePositives.Count / (double)outcomes.Count;
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"false positives {rate:P2} ({falsePositives.Count} of {outcomes.Count})"));
        fixtures.Count.ShouldBe(200);
        rate.ShouldBeLessThanOrEqualTo(0.02);
    }

    [Theory]
    [MemberData(nameof(PciFixtures))]
    public void Every_pci_fixture_reports_and_masks_its_card_number(string name)
    {
        var outcome = CorpusRunner.Run(Fixture.Read(Path.Combine(Fixture.RepositoryRoot, "fixtures", name)));

        outcome.Passed.ShouldBeTrue(outcome.Explain());
    }

    [Fact]
    public async Task No_output_format_ever_contains_a_test_card_number()
    {
        var report = await new LintRunner(CorpusRunner.Linter).RunAsync(
            new LintRunRequest { Inputs = ["fixtures/pci"], WorkingDirectory = Fixture.RepositoryRoot },
            TestContext.Current.CancellationToken);

        report.Summary.ByRule.ShouldContainKey("GEN-PCI-001");
        string[] numbers =
        [
            "4111111111111111", "4111 1111 1111 1111", "5555555555554444", "5555-5555-5555-4444", "378282246310005",
            "3782 822463 10005", "6011111111111117", "6011 1111 1111 1117", "6011-1111-1111-1117", "4012888888881881", "4012 8888 8888 1881",
        ];
        foreach (var writer in ReportWriters.All)
        {
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            writer.Write(report, text, ReportWriterOptions.Default with { BaseDirectory = Fixture.RepositoryRoot });
            var rendered = text.ToString();
            foreach (var number in numbers)
            {
                rendered.ShouldNotContain(number, Case.Sensitive, writer.Format);
            }
        }
    }

    [Fact]
    public async Task One_and_eight_threads_give_byte_identical_json_and_sarif()
    {
        // NFR-001 over 600 corpus payloads in many files.
        var one = await RenderAsync(parallelism: 1);
        var eight = await RenderAsync(parallelism: 8);

        eight.Json.ShouldBe(one.Json);
        eight.Sarif.ShouldBe(one.Sarif);
    }

    [Fact]
    public void Rule_example_contexts_match_the_packs()
    {
        foreach (var fixture in Fixture.Load("canonical"))
        {
            var ruleId = fixture.Name.Split('/')[^1][..^".json".Length];
            var context = CorpusRunner.Linter.Explain(ruleId).Examples!.Context;
            if (context.AsOf is { } asOf)
            {
                fixture.Context["asOf"].ShouldBe(asOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), fixture.Name);
            }

            fixture.Context.GetValueOrDefault("processor").ShouldBe(context.Processor, fixture.Name);
            fixture.Context.ContainsKey("rates").ShouldBe(context.RatesYaml is not null, fixture.Name);
        }
    }

    private static async Task<(string Json, string Sarif)> RenderAsync(int parallelism)
    {
        var report = await new LintRunner(CorpusRunner.Linter).RunAsync(
            new LintRunRequest { Inputs = ["fixtures/seeded", "fixtures/known-good"], WorkingDirectory = Fixture.RepositoryRoot, Parallelism = parallelism },
            TestContext.Current.CancellationToken);
        ReportWriters.TryGet("json", out var json);
        ReportWriters.TryGet("sarif", out var sarif);
        return (Render(json, report), Render(sarif, report));
    }

    private static string Render(IReportWriter writer, LintReport report)
    {
        var builder = new StringBuilder();
        using var text = new StringWriter(builder, CultureInfo.InvariantCulture);
        writer.Write(report, text, ReportWriterOptions.Default with { BaseDirectory = Fixture.RepositoryRoot });
        return builder.ToString();
    }
}
