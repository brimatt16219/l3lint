using System.Text.Json;

namespace L3Lint.Cli.Tests;

/// <summary>End-to-end tests of <c>l3lint lint</c> (US-01 to US-04, US-13, US-14, FR-025, Interfaces tab).</summary>
public sealed class LintCommandTests : IDisposable
{
    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Spec_example_has_one_visa_warning_and_exits_zero()
    {
        _cli.Write("INV-20417.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "INV-20417.json");

        run.ExitCode.ShouldBe(0);
        run.Out.ShouldContain("VISA-L3-010");
        run.Out.ShouldContain("1 transaction, 0 errors, 1 warning.");
        run.Out.ShouldContain("Product 3");
        run.Error.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fail_on_warning_turns_the_warning_into_exit_one()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json", "--fail-on", "warning")).ExitCode.ShouldBe(1);
        (await _cli.RunAsync("lint", "p.json", "--fail-on", "information")).ExitCode.ShouldBe(1);
        (await _cli.RunAsync("lint", "p.json", "--fail-on", "error")).ExitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Errors_exit_one_unless_fail_on_none()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);

        var failing = await _cli.RunAsync("lint", "p.json");
        failing.ExitCode.ShouldBe(1);
        failing.Out.ShouldContain("VISA-L3-004");
        failing.Out.ShouldContain("/lineItems/1/commodityCode");

        (await _cli.RunAsync("lint", "p.json", "--fail-on", "none")).ExitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Missing_file_is_a_usage_error()
    {
        var run = await _cli.RunAsync("lint", "nope.json");

        // Input errors are part of the results, so they print with the report on standard output.
        run.ExitCode.ShouldBe(2);
        run.Out.ShouldContain("nope.json");
    }

    [Fact]
    public async Task Malformed_json_is_an_input_error_with_a_location()
    {
        _cli.Write("bad.json", "{ \"id\": \"x\",\n  \"amount\": }");

        var run = await _cli.RunAsync("lint", "bad.json");

        run.ExitCode.ShouldBe(2);
        (run.Out + run.Error).ShouldContain("bad.json");
    }

    [Fact]
    public async Task Input_error_beats_findings_in_exit_code_precedence()
    {
        _cli.Write("a.json", Payloads.MissingCommodityCode);
        _cli.Write("b.json", "not json");

        var run = await _cli.RunAsync("lint", "a.json", "b.json");

        run.ExitCode.ShouldBe(2);
        run.Out.ShouldContain("VISA-L3-004");
    }

    [Fact]
    public async Task Unknown_option_is_a_usage_error_with_a_hint()
    {
        var run = await _cli.RunAsync("lint", "x.json", "--no-such-option");

        run.ExitCode.ShouldBe(2);
        run.Error.ShouldContain("--help");
    }

    [Fact]
    public async Task Standard_input_is_read_for_dash()
    {
        var run = await _cli.RunWithInputAsync(Payloads.MissingCommodityCode, "lint", "-");

        run.ExitCode.ShouldBe(1);
        run.Out.ShouldContain("VISA-L3-004");
    }

    [Fact]
    public async Task Standard_input_with_several_lines_is_ndjson()
    {
        var ndjson = Payloads.OneLine(Payloads.WithId("A")) + "\n" + Payloads.OneLine(Payloads.WithId("B")) + "\n";

        var run = await _cli.RunWithInputAsync(ndjson, "lint", "-", "--output", "json");

        run.ExitCode.ShouldBe(0);
        using var report = JsonDocument.Parse(run.Out);
        report.RootElement.GetProperty("summary").GetProperty("transactions").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Folders_and_globs_expand_in_sorted_order()
    {
        _cli.Write("fixtures/b.json", Payloads.WithId("B"));
        _cli.Write("fixtures/a.json", Payloads.WithId("A"));
        _cli.Write("fixtures/nested/c.json", Payloads.WithId("C"));
        _cli.Write("fixtures/readme.txt", "not a payload");

        var folder = await _cli.RunAsync("lint", "fixtures", "--output", "json");
        var glob = await _cli.RunAsync("lint", "fixtures/**/*.json", "--output", "json");

        folder.ExitCode.ShouldBe(0);
        using var report = JsonDocument.Parse(folder.Out);
        var ids = report.RootElement.GetProperty("transactions").EnumerateArray().Select(t => t.GetProperty("id").GetString()).ToList();
        ids.ShouldBe(["A", "B", "C"]);
        glob.Out.ShouldBe(folder.Out);
    }

    [Fact]
    public async Task Glob_without_matches_is_reported()
    {
        var run = await _cli.RunAsync("lint", "nothing/**/*.json");

        run.ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Ndjson_file_lints_each_line()
    {
        _cli.Write("batch.ndjson", Payloads.OneLine(Payloads.WithId("A")) + "\n\n" + Payloads.OneLine(Payloads.MissingCommodityCode) + "\n");

        var run = await _cli.RunAsync("lint", "batch.ndjson", "--output", "json");

        run.ExitCode.ShouldBe(1);
        using var report = JsonDocument.Parse(run.Out);
        report.RootElement.GetProperty("summary").GetProperty("transactions").GetInt32().ShouldBe(2);
        report.RootElement.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Max_findings_caps_output_but_the_summary_counts_everything()
    {
        var lines = Enumerable.Range(0, 5).Select(i => Payloads.OneLine(Payloads.MissingCommodityCode));
        _cli.Write("batch.ndjson", string.Join("\n", lines) + "\n");

        var run = await _cli.RunAsync("lint", "batch.ndjson", "--output", "json", "--max-findings", "2");

        using var report = JsonDocument.Parse(run.Out);
        var printed = report.RootElement.GetProperty("transactions").EnumerateArray().Sum(t => t.GetProperty("findings").GetArrayLength());
        printed.ShouldBe(2);
        report.RootElement.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBe(5);
    }

    [Fact]
    public async Task Card_numbers_are_masked_in_every_output()
    {
        _cli.Write("p.json", Payloads.WithCardNumber);

        var text = await _cli.RunAsync("lint", "p.json");
        var json = await _cli.RunAsync("lint", "p.json", "--output", "json", "--log-level", "trace", "--log-values");

        text.ExitCode.ShouldBe(1);
        text.Out.ShouldContain("GEN-PCI-001");
        foreach (var output in new[] { text.Out, text.Error, json.Out, json.Error })
        {
            output.ShouldNotContain(Payloads.TestCardNumber);
            output.ShouldNotContain(Payloads.TestCardDigits);
        }

        json.Out.ShouldContain(Payloads.MaskedTestCardNumber);
    }

    [Fact]
    public async Task Strict_pci_stops_with_exit_one()
    {
        _cli.Write("p.json", Payloads.WithCardNumber);

        var run = await _cli.RunAsync("lint", "p.json", "--strict-pci", "--output", "json");

        run.ExitCode.ShouldBe(1);
        using var report = JsonDocument.Parse(run.Out);
        var rules = report.RootElement.GetProperty("transactions").EnumerateArray()
            .SelectMany(t => t.GetProperty("findings").EnumerateArray()).Select(f => f.GetProperty("ruleId").GetString()).Distinct().ToList();
        rules.ShouldBe(["GEN-PCI-001"]);
    }

    [Fact]
    public async Task Card_product_flag_overrides_the_payload_and_says_so()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--card-product", "consumer", "--output", "json");
        var text = await _cli.RunAsync("lint", "p.json", "--card-product", "consumer");

        // US-03: a consumer card yields one informational finding ("no enhanced-data rate available") and
        // suppresses the rate-related findings.
        run.ExitCode.ShouldBe(0);
        using var report = JsonDocument.Parse(run.Out);
        var findings = report.RootElement.GetProperty("transactions")[0].GetProperty("findings");
        findings.GetArrayLength().ShouldBe(1);
        findings[0].GetProperty("ruleId").GetString().ShouldBe("GEN-ELIG-001");
        findings[0].GetProperty("severity").GetString().ShouldBe("information");
        text.Out.ShouldContain("Note: Card product set to consumer by option; the payload said purchasing.");
    }

    [Fact]
    public async Task Brand_all_lints_against_every_brand()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--brand", "all", "--output", "json", "--fail-on", "none");

        using var report = JsonDocument.Parse(run.Out);
        var prefixes = report.RootElement.GetProperty("transactions")[0].GetProperty("findings").EnumerateArray()
            .Select(f => f.GetProperty("ruleId").GetString()!.Split('-')[0]).ToHashSet();
        prefixes.ShouldContain("MC");
        prefixes.ShouldContain("VISA");
    }

    [Fact]
    public async Task Only_disable_and_severity_change_rule_selection()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var disabled = await _cli.RunAsync("lint", "p.json", "--disable", "VISA-L3-*");
        disabled.Out.ShouldNotContain("VISA-L3-010");
        disabled.Out.ShouldContain("0 warnings");

        var only = await _cli.RunAsync("lint", "p.json", "--only", "GEN-*");
        only.Out.ShouldNotContain("VISA-L3-010");

        var raised = await _cli.RunAsync("lint", "p.json", "--severity", "VISA-L3-010=error");
        raised.ExitCode.ShouldBe(1);
        raised.Out.ShouldContain("1 error");
    }

    [Fact]
    public async Task Bad_values_are_usage_errors()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json", "--as-of", "19/10/2026")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--fail-on", "fatal")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--output", "html")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--annotations", "gitlab")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--log-level", "loud")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--format", "paypal")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("lint", "p.json", "--severity", "VISA-L3-010")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task As_of_before_a_rule_takes_effect_skips_it()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);

        // VISA-L3-004 is effective from 2025-10-17 (Card brand rules tab), so it does not run a year earlier.
        var run = await _cli.RunAsync("lint", "p.json", "--as-of", "2024-10-19");

        run.Out.ShouldNotContain("VISA-L3-004");
    }

    [Fact]
    public async Task Quiet_and_verbose_change_what_is_printed()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var quiet = await _cli.RunAsync("lint", "p.json", "-q");
        var normal = await _cli.RunAsync("lint", "p.json");

        quiet.ExitCode.ShouldBe(0);
        quiet.Out.ShouldBe("1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.\n");
        normal.Out.ShouldEndWith(quiet.Out);
        normal.Out.ShouldContain("VISA-L3-010");
    }

    [Fact]
    public async Task Logs_go_to_standard_error_only()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var text = await _cli.RunAsync("lint", "p.json", "--log-level", "debug", "--output", "json");
        var json = await _cli.RunAsync("lint", "p.json", "--log-level", "debug", "--log-format", "json", "--output", "json");

        // Standard output stays a valid JSON report whatever the log level.
        Should.NotThrow(() => JsonDocument.Parse(text.Out).Dispose());
        Should.NotThrow(() => JsonDocument.Parse(json.Out).Dispose());
    }

    [Fact]
    public async Task Log_values_prints_a_warning()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--log-values");

        run.Error.ShouldContain("--log-values");
    }

    [Fact]
    public async Task Currency_flag_and_config_key_fill_a_payload_without_currency()
    {
        // Braintree requests never carry a currency (ADR-0056); fixtures/adapters/braintree/03 shows GEN-FMT-002 without one.
        File.Copy(Path.Combine(RepositoryRoot(), "fixtures", "adapters", "braintree", "01-complete-level3.json"), Path.Combine(_cli.WorkingDirectory, "bt.json"));

        var without = await _cli.RunAsync("lint", "bt.json", "--brand", "visa", "--card-product", "purchasing", "--mcc", "5047", "--output", "json", "--fail-on", "none");
        var flag = await _cli.RunAsync("lint", "bt.json", "--brand", "visa", "--card-product", "purchasing", "--mcc", "5047", "--currency", "usd", "--output", "json", "--fail-on", "none");
        _cli.Write(".l3lint.yaml", "version: 1\ninputs:\n  defaultCurrency: USD\n");
        var config = await _cli.RunAsync("lint", "bt.json", "--brand", "visa", "--card-product", "purchasing", "--mcc", "5047", "--output", "json", "--fail-on", "none");

        without.Out.ShouldContain("GEN-FMT-002");
        flag.Out.ShouldNotContain("GEN-FMT-002");
        flag.Out.ShouldContain("\"format\": \"braintree\"");
        config.Out.ShouldNotContain("GEN-FMT-002");
        (await _cli.RunAsync("lint", "bt.json", "--currency", "XYZ")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Csv_with_a_column_map_groups_rows_into_transactions()
    {
        // US-14. fixtures/adapters/csv/01 is an ERP export with two invoices; its expectations list VISA-L3-011 on
        // both transactions, because the map sends no duty or order discount.
        var folder = Path.Combine(RepositoryRoot(), "fixtures", "adapters", "csv");
        File.Copy(Path.Combine(folder, "01-erp-two-invoices.csv"), Path.Combine(_cli.WorkingDirectory, "invoices.csv"));
        File.Copy(Path.Combine(folder, "01-erp-two-invoices.map.yaml"), Path.Combine(_cli.WorkingDirectory, "erp-map.yaml"));

        var run = await _cli.RunAsync("lint", "invoices.csv", "--format", "csv", "--map", "erp-map.yaml", "--brand", "visa", "--card-product", "purchasing", "--mcc", "5047", "--output", "json");
        var missingMap = await _cli.RunAsync("lint", "invoices.csv", "--format", "csv");

        run.ExitCode.ShouldBe(1);
        using var report = JsonDocument.Parse(run.Out);
        var transactions = report.RootElement.GetProperty("transactions");
        transactions.GetArrayLength().ShouldBe(2);
        foreach (var transaction in transactions.EnumerateArray())
        {
            transaction.GetProperty("findings").EnumerateArray().ShouldContain(f => f.GetProperty("ruleId").GetString() == "VISA-L3-011");
        }

        missingMap.ExitCode.ShouldBe(2);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
