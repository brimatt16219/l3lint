using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace L3Lint.Cli.Tests;

/// <summary>The remaining lint options and the local rule pack option on every command that takes it.</summary>
public sealed class MoreOptionTests : IDisposable
{
    private const string PackRules = """
        pack:
          id: acme.rules.general
          version: 2.1.0
          brand: general
          publishedOn: 2026-10-20
          title: Acme house rules
        rules:
          - id: GEN-ACME-001
            title: Invoice number must start with INV-
            level: general
            severity: warning
            confidence: policy
            check: { kind: pattern, field: /order/invoiceNumber, regex: "^INV-" }
            message: "Invoice number {actual} does not start with INV-."
            fix: Use the INV- prefix.
            impact: hygiene
            sources: [https://example.org/acme-invoice-policy]
        """;

    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Merchant_country_flag_overrides_the_payload_with_a_note()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--merchant-country", "CA");

        run.Out.ShouldContain("Note: Merchant country set to CA by option; the payload said US.");
        (await _cli.RunAsync("lint", "p.json", "--merchant-country", "Canada")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Strict_profiles_runs_processor_rules_at_their_own_severity()
    {
        // AMEX-L2-001 is an error tied to the mastercard-gateway-amex profile, so under the generic profile it is
        // capped at warning (ADR-0016) unless --strict-profiles is set.
        _cli.Write("p.json", Payloads.Mutate(p =>
        {
            p["card"]!["brand"] = "amex";
            p["shipping"]!.AsObject().Remove("toPostalCode");
        }));

        var capped = await _cli.RunAsync("lint", "p.json", "--output", "json", "--fail-on", "none");
        var strict = await _cli.RunAsync("lint", "p.json", "--output", "json", "--fail-on", "none", "--strict-profiles");

        Severity(capped.Out, "AMEX-L2-001").ShouldBe("warning");
        Severity(strict.Out, "AMEX-L2-001").ShouldBe("error");
    }

    [Fact]
    public async Task Processor_flag_selects_the_profile_that_owns_a_rule()
    {
        // ADR-0016: with the rule's own profile selected, the catalog severity applies without --strict-profiles.
        _cli.Write("p.json", Payloads.Mutate(p =>
        {
            p["card"]!["brand"] = "amex";
            p["shipping"]!.AsObject().Remove("toPostalCode");
        }));

        var run = await _cli.RunAsync("lint", "p.json", "--output", "json", "--fail-on", "none", "--processor", "mastercard-gateway-amex");

        Severity(run.Out, "AMEX-L2-001").ShouldBe("error");
    }

    [Fact]
    public async Task Fix_dry_run_flag_prints_the_default_patch()
    {
        _cli.Write("p.json", Payloads.WrongLineTotal);

        var dryRun = await _cli.RunAsync("fix", "p.json", "--dry-run");
        var plain = await _cli.RunAsync("fix", "p.json");

        dryRun.ExitCode.ShouldBe(plain.ExitCode);
        dryRun.Out.ShouldBe(plain.Out);
        dryRun.Out.ShouldContain("\"path\": \"/lineItems/0/total\"");
    }

    [Fact]
    public async Task Verbose_prints_where_settings_came_from()
    {
        _cli.Write(".l3lint.yaml", "version: 1\nrules:\n  disable: [GEN-DQ-008]\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var verbose = await _cli.RunAsync("lint", "p.json", "--verbose");
        var normal = await _cli.RunAsync("lint", "p.json");

        verbose.Out.Length.ShouldBeGreaterThan(normal.Out.Length);
        verbose.Out.ShouldContain(".l3lint.yaml");
    }

    [Fact]
    public async Task A_local_rule_pack_works_with_lint_explain_version_and_rules()
    {
        WritePack("acme-pack");
        _cli.Write("p.json", Payloads.Mutate(p => p["order"]!["invoiceNumber"] = "20417"));

        var lint = await _cli.RunAsync("lint", "p.json", "--rule-pack", "acme-pack", "--output", "json");
        var explain = await _cli.RunAsync("explain", "GEN-ACME-001", "--rule-pack", "acme-pack");
        var version = await _cli.RunAsync("version", "--rule-pack", "acme-pack");
        var list = await _cli.RunAsync("rules", "list", "--rule-pack", "acme-pack", "--query", "ACME", "--output", "json");
        var test = await _cli.RunAsync("rules", "test", "--rule-pack", "acme-pack", "--only", "GEN-ACME-*");
        var show = await _cli.RunAsync("rules", "show", "GEN-ACME-001", "--rule-pack", "acme-pack");

        Severity(lint.Out, "GEN-ACME-001").ShouldBe("warning");
        explain.ExitCode.ShouldBe(0);
        explain.Out.ShouldStartWith("# GEN-ACME-001: Invoice number must start with INV-");
        show.ExitCode.ShouldBe(0);
        show.Out.ShouldStartWith("# GEN-ACME-001: Invoice number must start with INV-");
        version.Out.ShouldContain("acme.rules.general 2.1.0");
        using var rules = JsonDocument.Parse(list.Out);
        rules.RootElement.EnumerateArray().Select(r => r.GetProperty("id").GetString()).ShouldBe(["GEN-ACME-001"]);

        // The pack has no examples, so rules test reports the rule as failing.
        test.ExitCode.ShouldBe(1);
        test.Out.ShouldContain("FAIL  GEN-ACME-001");
    }

    [Fact]
    public async Task Short_aliases_match_their_long_options()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json", "-f", "canonical", "-o", "json")).Out.ShouldBe((await _cli.RunAsync("lint", "p.json", "--format", "canonical", "--output", "json")).Out);
        (await _cli.RunAsync("lint", "p.json", "-v")).Out.ShouldBe((await _cli.RunAsync("lint", "p.json", "--verbose")).Out);
        (await _cli.RunAsync("lint", "p.json", "--quiet")).Out.ShouldBe((await _cli.RunAsync("lint", "p.json", "-q")).Out);
        (await _cli.RunAsync("lint", "-h")).Out.ShouldBe((await _cli.RunAsync("lint", "--help")).Out);

        (await _cli.RunAsync("init", "-d", "team")).ExitCode.ShouldBe(0);
        _cli.Exists("team/.l3lint.yaml").ShouldBeTrue();
    }

    [Fact]
    public async Task A_tampered_local_pack_is_refused_with_exit_three()
    {
        WritePack("acme-pack");
        File.AppendAllText(Path.Combine(_cli.WorkingDirectory, "acme-pack", "acme.rules.yaml"), "\n# edited after hashing\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--rule-pack", "acme-pack");

        run.ExitCode.ShouldBe(3);
        run.Error.ShouldContain("acme.rules.yaml");

        // fix and convert load the same packs, so the tampered pack stops them too.
        (await _cli.RunAsync("fix", "p.json", "--rule-pack", "acme-pack")).ExitCode.ShouldBe(3);
        (await _cli.RunAsync("convert", "p.json", "--to", "canonical", "--rule-pack", "acme-pack")).ExitCode.ShouldBe(3);
    }

    private void WritePack(string folder)
    {
        var rules = Encoding.UTF8.GetBytes(PackRules.ReplaceLineEndings("\n"));
        _cli.Write(folder + "/acme.rules.yaml", PackRules.ReplaceLineEndings("\n"));
        var hash = Convert.ToHexStringLower(SHA256.HashData(rules));
        _cli.Write(folder + "/manifest.json", "{\n  \"pack\": \"acme.rules.general\",\n  \"version\": \"2.1.0\",\n  \"algorithm\": \"sha256\",\n  \"files\": { \"acme.rules.yaml\": \"" + hash + "\" }\n}\n");
    }

    private static string? Severity(string json, string ruleId)
    {
        using var report = JsonDocument.Parse(json);
        return report.RootElement.GetProperty("transactions").EnumerateArray()
            .SelectMany(t => t.GetProperty("findings").EnumerateArray())
            .Where(f => f.GetProperty("ruleId").GetString() == ruleId)
            .Select(f => f.GetProperty("severity").GetString())
            .FirstOrDefault();
    }
}
