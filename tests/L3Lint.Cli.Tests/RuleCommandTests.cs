using System.Text.Json;

namespace L3Lint.Cli.Tests;

/// <summary><c>explain</c>, <c>rules</c>, <c>schema</c> and <c>version</c> (US-05, US-08, US-11, FR-020, FR-026).</summary>
public sealed class RuleCommandTests : IDisposable
{
    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Explain_shows_requirement_sources_confidence_dates_and_examples()
    {
        var run = await _cli.RunAsync("explain", "VISA-L3-004");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldStartWith("# VISA-L3-004: ");
        run.Out.ShouldContain("processor-doc");
        run.Out.ShouldContain("2025-10-17");
        run.Out.ShouldContain("https://");
        run.Out.ShouldContain("commodityCode");
    }

    [Fact]
    public async Task Explain_json_is_machine_readable()
    {
        var run = await _cli.RunAsync("explain", "visa-l3-004", "--output", "json");

        run.ExitCode.ShouldBe(0);
        using var rule = JsonDocument.Parse(run.Out);
        rule.RootElement.GetProperty("id").GetString().ShouldBe("VISA-L3-004");
        rule.RootElement.GetProperty("severity").GetString().ShouldBe("error");
    }

    [Fact]
    public async Task Explain_unknown_rule_is_a_usage_error()
    {
        var run = await _cli.RunAsync("explain", "VISA-L3-999");

        run.ExitCode.ShouldBe(2);
        run.Error.ShouldContain("VISA-L3-999");
        (await _cli.RunAsync("explain", "VISA-L3-004", "--output", "yaml")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Rules_list_shows_every_pack_with_version_dates_and_source_count()
    {
        var run = await _cli.RunAsync("rules", "list");

        run.ExitCode.ShouldBe(0);
        foreach (var pack in new[] { "l3lint.rules.general", "l3lint.rules.visa", "l3lint.rules.mastercard", "l3lint.rules.amex", "l3lint.rules.discover" })
        {
            run.Out.ShouldContain(pack);
        }

        // 23 general, 26 Visa, 17 Mastercard, 9 Amex and 3 Discover rules (Card brand rules tab).
        run.Out.ShouldContain("78 rule(s).");
        run.Out.ShouldNotContain("\r");
    }

    [Fact]
    public async Task Rules_list_filters()
    {
        var discover = await _cli.RunAsync("rules", "list", "--brand", "discover", "--output", "json");
        using var list = JsonDocument.Parse(discover.Out);
        list.RootElement.EnumerateArray().Select(r => r.GetProperty("id").GetString()).ShouldBe(["DISC-ELIG-001", "DISC-FMT-001", "DISC-L2-001"]);

        var general = await _cli.RunAsync("rules", "list", "--brand", "general", "--output", "json");
        using var generalList = JsonDocument.Parse(general.Out);
        generalList.RootElement.GetArrayLength().ShouldBe(23);

        var query = await _cli.RunAsync("rules", "list", "--query", "commodity", "--level", "L3", "--output", "json");
        using var queried = JsonDocument.Parse(query.Out);
        queried.RootElement.EnumerateArray().ShouldAllBe(r => r.GetProperty("level").GetString() == "L3");
        queried.RootElement.GetArrayLength().ShouldBeGreaterThan(0);

        (await _cli.RunAsync("rules", "list", "--brand", "jcb")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("rules", "list", "--level", "L4")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Rules_show_prints_the_reference_page()
    {
        var run = await _cli.RunAsync("rules", "show", "GEN-PCI-001");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldStartWith("# GEN-PCI-001: Card number in the payload");
        (await _cli.RunAsync("rules", "show", "NOPE")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Rules_test_runs_every_example_and_passes()
    {
        var run = await _cli.RunAsync("rules", "test");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldContain("78 rule(s) tested, 78 passed, 0 failed.");
        run.Out.ShouldContain("pass  VISA-L3-004");
    }

    [Fact]
    public async Task Rules_test_can_select_rules_and_be_quiet()
    {
        var run = await _cli.RunAsync("rules", "test", "--only", "MC-*", "-q");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldBe("17 rule(s) tested, 17 passed, 0 failed.\n");
    }

    [Fact]
    public async Task Rules_test_fails_for_a_broken_local_pack()
    {
        var pack = Path.Combine(_cli.WorkingDirectory, "pack");
        Directory.CreateDirectory(pack);
        _cli.Write("pack/manifest.json", "{ \"broken\": true }");

        var run = await _cli.RunAsync("rules", "test", "--rule-pack", "pack");

        run.ExitCode.ShouldBe(3);
    }

    [Theory]
    [InlineData("canonical")]
    [InlineData("results")]
    [InlineData("config")]
    [InlineData("rulepack")]
    [InlineData("csvmap")]
    [InlineData("rates")]
    [InlineData("profile")]
    [InlineData("lock")]
    public async Task Schema_prints_each_schema_byte_for_byte(string name)
    {
        var run = await _cli.RunAsync("schema", name);

        run.ExitCode.ShouldBe(0);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", name + ".schema.json")).ReplaceLineEndings("\n");
        run.Out.ShouldBe(expected.EndsWith('\n') ? expected : expected + "\n");
    }

    [Fact]
    public async Task Schema_unknown_name_lists_the_choices()
    {
        var run = await _cli.RunAsync("schema", "sarif");

        run.ExitCode.ShouldBe(2);
        run.Error.ShouldContain("canonical, results, config, rulepack");
    }

    [Fact]
    public async Task Version_prints_engine_and_pack_versions()
    {
        var run = await _cli.RunAsync("version");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldStartWith("l3lint 1.0.0\n");
        run.Out.ShouldContain("  l3lint.rules.visa 1.0.0\n");
    }

    [Fact]
    public async Task Help_lists_the_eight_commands()
    {
        var run = await _cli.RunAsync("--help");

        run.ExitCode.ShouldBe(0);
        foreach (var command in new[] { "lint", "explain", "fix", "convert", "rules", "schema", "init", "version" })
        {
            run.Out.ShouldContain(command);
        }
    }
}
