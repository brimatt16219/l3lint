using System.Text.Json;
using Json.Schema;

namespace L3Lint.Cli.Tests;

/// <summary>Output selection, files, annotations and color (FR-021 to FR-025, NFR-010, NFR-012).</summary>
public sealed class OutputTests : IDisposable
{
    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Json_output_validates_against_the_results_schema()
    {
        _cli.Write("a.json", Payloads.MissingCommodityCode);
        _cli.Write("b.json", Payloads.WithCardNumber);
        _cli.Write("c.json", "not json");

        var run = await _cli.RunAsync("lint", "a.json", "b.json", "c.json", "--output", "json");

        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", "results.schema.json")), new BuildOptions { SchemaRegistry = new SchemaRegistry() });
        using var report = JsonDocument.Parse(run.Out);
        var result = schema.Evaluate(report.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        result.IsValid.ShouldBeTrue(string.Join("; ", (result.Details ?? []).Where(d => d.Errors is not null).SelectMany(d => d.Errors!.Select(e => d.InstanceLocation + " " + e.Value))));
    }

    [Fact]
    public async Task Json_output_is_identical_across_runs()
    {
        _cli.Write("a.json", Payloads.MissingCommodityCode);
        _cli.Write("b.json", Payloads.WrongLineTotal);

        var first = await _cli.RunAsync("lint", "a.json", "b.json", "--output", "json");
        var second = await _cli.RunAsync("lint", "a.json", "b.json", "--output", "json");

        second.Out.ShouldBe(first.Out);
    }

    [Fact]
    public async Task Kind_equals_path_writes_a_file_and_keeps_text_on_stdout()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);

        var run = await _cli.RunAsync("lint", "p.json", "--output", "text", "--output", "json=out/report.json");

        run.ExitCode.ShouldBe(1);
        run.Out.ShouldContain("VISA-L3-004");
        _cli.Exists("out/report.json").ShouldBeTrue();
        using var report = JsonDocument.Parse(_cli.Read("out/report.json"));
        report.RootElement.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Unwritable_output_path_exits_three()
    {
        _cli.Write("p.json", Payloads.SpecExample);
        _cli.Write("blocked", "a file where a folder is needed");

        var run = await _cli.RunAsync("lint", "p.json", "--output", "json=blocked/report.json");

        run.ExitCode.ShouldBe(3);
    }

    [Fact]
    public async Task Github_annotations_cover_findings_at_or_above_fail_on()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);

        var run = await _cli.RunAsync("lint", "p.json", "--annotations", "github");

        var annotations = run.Out.Split('\n').Where(l => l.StartsWith("::", StringComparison.Ordinal)).ToList();
        annotations.ShouldHaveSingleItem().ShouldStartWith("::error file=p.json,");
        annotations[0].ShouldContain("title=VISA-L3-004");
    }

    [Fact]
    public async Task Github_annotations_include_warnings_when_fail_on_is_warning()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--annotations", "github", "--fail-on", "warning");

        run.Out.Split('\n').Where(l => l.StartsWith("::warning ", StringComparison.Ordinal)).ShouldHaveSingleItem().ShouldContain("VISA-L3-010");
    }

    [Fact]
    public async Task Color_only_on_a_terminal_without_no_color()
    {
        _cli.Write("p.json", Payloads.SpecExample);
        const string Escape = "\u001b[";

        (await _cli.RunAsync("lint", "p.json")).Out.ShouldNotContain(Escape);

        _cli.OutputIsTerminal = true;
        (await _cli.RunAsync("lint", "p.json")).Out.ShouldContain(Escape);
        (await _cli.RunAsync("lint", "p.json", "--no-color")).Out.ShouldNotContain(Escape);

        _cli.SetVariable("NO_COLOR", "1");
        (await _cli.RunAsync("lint", "p.json")).Out.ShouldNotContain(Escape);
    }

    [Fact]
    public async Task Cost_estimates_need_a_rate_table_and_are_labeled()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "rates-example.yaml"), Path.Combine(_cli.WorkingDirectory, "rates.yaml"));

        var without = await _cli.RunAsync("lint", "p.json", "--output", "json");
        var with = await _cli.RunAsync("lint", "p.json", "--output", "json", "--rates", "rates.yaml");

        using var plain = JsonDocument.Parse(without.Out);
        plain.RootElement.GetProperty("transactions")[0].TryGetProperty("costEstimate", out _).ShouldBeFalse();
        using var estimated = JsonDocument.Parse(with.Out);
        estimated.RootElement.GetProperty("transactions")[0].TryGetProperty("costEstimate", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_rate_table_flag_is_a_usage_error()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--rates", "missing.yaml");

        run.ExitCode.ShouldBe(2);
        run.Error.ShouldContain("missing.yaml");
    }
}
