using System.Text.Json;
using System.Text.Json.Nodes;

namespace L3Lint.Cli.Tests;

/// <summary><c>fix</c>, <c>convert</c> and <c>init</c> (US-02, US-06, US-07, FR-027, FR-032, ADR-0041, ADR-0047).</summary>
public sealed class FixConvertInitTests : IDisposable
{
    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Fix_dry_run_prints_the_line_total_patch()
    {
        _cli.Write("p.json", Payloads.WrongLineTotal);

        var run = await _cli.RunAsync("fix", "p.json");

        // The mutation set line 1 total to 125.00; 10 x 12.50 - 5.00 = 120.00 (Card brand rules tab, worked arithmetic).
        run.ExitCode.ShouldBe(0);
        var patch = JsonNode.Parse(run.Out)!.AsArray();
        var total = patch.Single(o => (string?)o!["path"] == "/lineItems/0/total");
        ((decimal)total!["value"]!).ShouldBe(120.00m);
        patch.ShouldNotContain(o => (string?)o!["path"] == "/amount");
    }

    [Fact]
    public async Task Fix_amount_is_only_patched_with_fix_amount()
    {
        _cli.Write("p.json", Payloads.Mutate(p => p["amount"] = 290.36m));

        var withoutFlag = await _cli.RunAsync("fix", "p.json");
        var withFlag = await _cli.RunAsync("fix", "p.json", "--fix-amount");

        JsonNode.Parse(withoutFlag.Out)!.AsArray().ShouldNotContain(o => (string?)o!["path"] == "/amount");
        withoutFlag.Error.ShouldContain("GEN-ARITH-002");
        var amount = JsonNode.Parse(withFlag.Out)!.AsArray().Single(o => (string?)o!["path"] == "/amount");

        // 280.36 is the spec example's correct total; the mutation added 10.00.
        ((decimal)amount!["value"]!).ShouldBe(280.36m);
    }

    [Fact]
    public async Task Fix_never_invents_commodity_codes()
    {
        _cli.Write("p.json", Payloads.MissingCommodityCode);

        var run = await _cli.RunAsync("fix", "p.json");

        JsonNode.Parse(run.Out)!.AsArray().ShouldNotContain(o => ((string?)o!["path"] ?? string.Empty).Contains("commodityCode", StringComparison.Ordinal));
        run.Error.ShouldContain("VISA-L3-004");
    }

    [Fact]
    public async Task Fix_write_creates_a_patched_copy_that_lints_cleaner()
    {
        _cli.Write("p.json", Payloads.WrongLineTotal);

        var before = await _cli.RunAsync("lint", "p.json", "--output", "json");
        var fix = await _cli.RunAsync("fix", "p.json", "--write", "fixed/p.json");
        var after = await _cli.RunAsync("lint", "fixed/p.json", "--output", "json");

        fix.ExitCode.ShouldBe(0);
        _cli.Read("p.json").ShouldBe(Payloads.WrongLineTotal);
        using var beforeReport = JsonDocument.Parse(before.Out);
        using var afterReport = JsonDocument.Parse(after.Out);
        Rules(beforeReport).ShouldContain("GEN-ARITH-001");
        Rules(afterReport).ShouldNotContain("GEN-ARITH-001");
        Rules(afterReport).ShouldNotContain("GEN-ARITH-002");
    }

    [Fact]
    public async Task Fix_copy_never_contains_a_card_number()
    {
        _cli.Write("p.json", Payloads.Mutate(p =>
        {
            p["lineItems"]![0]!["description"] = Payloads.TestCardNumber;
            p["lineItems"]![0]!["total"] = 125.00m;
        }));

        await _cli.RunAsync("fix", "p.json", "--write", "fixed.json");

        _cli.Read("fixed.json").ShouldNotContain(Payloads.TestCardNumber);
        _cli.Read("fixed.json").ShouldNotContain(Payloads.TestCardDigits);
        _cli.Read("fixed.json").ShouldContain(Payloads.MaskedTestCardNumber);
    }

    [Fact]
    public async Task Fix_with_strict_pci_stops()
    {
        _cli.Write("p.json", Payloads.WithCardNumber);

        (await _cli.RunAsync("fix", "p.json", "--strict-pci")).ExitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Fix_rejects_bad_targets()
    {
        _cli.Write("p.json", Payloads.WrongLineTotal);

        (await _cli.RunAsync("fix", "p.json", "--write", "p.json")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("fix", "p.json", "--write", "q.json", "--dry-run")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("fix", "missing.json")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Fix_ndjson_gives_one_patch_per_line()
    {
        _cli.Write("batch.ndjson", Payloads.OneLine(Payloads.SpecExample) + "\n" + Payloads.OneLine(Payloads.WrongLineTotal) + "\n");

        var run = await _cli.RunAsync("fix", "batch.ndjson");

        var lines = run.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2);
        JsonNode.Parse(lines[0])!.AsArray().ShouldNotContain(o => (string?)o!["path"] == "/lineItems/0/total");
        JsonNode.Parse(lines[1])!.AsArray().ShouldContain(o => (string?)o!["path"] == "/lineItems/0/total");

        var write = await _cli.RunAsync("fix", "batch.ndjson", "--write", "fixed.ndjson");
        write.ExitCode.ShouldBe(0);
        _cli.Read("fixed.ndjson").Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(2);
    }

    [Fact]
    public async Task Fix_reads_standard_input()
    {
        var run = await _cli.RunWithInputAsync(Payloads.WrongLineTotal, "fix", "-");

        run.ExitCode.ShouldBe(0);
        JsonNode.Parse(run.Out)!.AsArray().ShouldContain(o => (string?)o!["path"] == "/lineItems/0/total");
    }

    [Fact]
    public async Task Convert_canonical_has_an_identity_path_map()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("convert", "p.json", "--to", "canonical");

        run.ExitCode.ShouldBe(0);
        using var mapping = JsonDocument.Parse(run.Out);
        mapping.RootElement.GetProperty("format").GetString().ShouldBe("canonical");
        var transaction = mapping.RootElement.GetProperty("transactions")[0];
        transaction.GetProperty("canonical").GetProperty("id").GetString().ShouldBe("INV-20417");
        transaction.GetProperty("pathMap").GetProperty("/lineItems/1/commodityCode").GetString().ShouldBe("/lineItems/1/commodityCode");
        transaction.GetProperty("derivedFields").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Convert_canonical_only_round_trips_the_payload()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("convert", "p.json", "--to", "canonical", "--canonical-only");

        run.ExitCode.ShouldBe(0);
        run.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBe(1);
        JsonNode.DeepEquals(JsonNode.Parse(run.Out), JsonNode.Parse(Payloads.SpecExample)).ShouldBeTrue();
    }

    [Fact]
    public async Task Convert_masks_card_numbers_and_honors_strict_pci()
    {
        _cli.Write("p.json", Payloads.WithCardNumber);

        var run = await _cli.RunAsync("convert", "p.json", "--to", "canonical");
        var strict = await _cli.RunAsync("convert", "p.json", "--to", "canonical", "--strict-pci");

        run.Out.ShouldNotContain(Payloads.TestCardNumber);
        run.Out.ShouldNotContain(Payloads.TestCardDigits);
        using var mapping = JsonDocument.Parse(run.Out);
        mapping.RootElement.GetProperty("cardNumbersMasked").GetInt32().ShouldBe(1);
        strict.ExitCode.ShouldBe(1);
        strict.Out.ShouldBeEmpty();
    }

    [Fact]
    public async Task Convert_requires_a_known_target()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("convert", "p.json")).ExitCode.ShouldBe(2);
        (await _cli.RunAsync("convert", "p.json", "--to", "stripe")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Init_writes_four_files_that_lint_clean()
    {
        var init = await _cli.RunAsync("init");

        init.ExitCode.ShouldBe(0);
        foreach (var file in new[] { ".l3lint.yaml", ".l3lint.lock", "samples/l3lint-sample.json", ".github/workflows/l3lint.yml" })
        {
            _cli.Exists(file).ShouldBeTrue(file);
        }

        _cli.Read(".github/workflows/l3lint.yml").ShouldContain("brimatt16219/l3lint/action@v1.0.0");
        _cli.Read(".l3lint.lock").ShouldContain("l3lint.rules.visa: 1.0.0");
        var lint = await _cli.RunAsync("lint", "samples/l3lint-sample.json");
        lint.ExitCode.ShouldBe(0);
        lint.Out.ShouldContain("1 warning");
    }

    [Fact]
    public async Task Init_never_overwrites_without_force()
    {
        _cli.Write(".l3lint.yaml", "version: 1\n");

        var refused = await _cli.RunAsync("init");
        refused.ExitCode.ShouldBe(2);
        _cli.Read(".l3lint.yaml").ShouldBe("version: 1\n");
        _cli.Exists(".l3lint.lock").ShouldBeFalse();

        (await _cli.RunAsync("init", "--force", "-q")).ExitCode.ShouldBe(0);
        _cli.Read(".l3lint.yaml").ShouldNotBe("version: 1\n");
    }

    [Fact]
    public async Task Init_writes_into_another_directory()
    {
        var run = await _cli.RunAsync("init", "--directory", "team");

        run.ExitCode.ShouldBe(0);
        _cli.Exists("team/.l3lint.yaml").ShouldBeTrue();
    }

    private static List<string> Rules(JsonDocument report) =>
        [.. report.RootElement.GetProperty("transactions").EnumerateArray().SelectMany(t => t.GetProperty("findings").EnumerateArray()).Select(f => f.GetProperty("ruleId").GetString()!)];
}
