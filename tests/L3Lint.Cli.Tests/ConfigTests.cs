namespace L3Lint.Cli.Tests;

/// <summary>Config discovery, precedence, validation and the lock file (Interfaces tab "Configuration file", ADR-0024, ADR-0041).</summary>
public sealed class ConfigTests : IDisposable
{
    private readonly Cli _cli = new();

    public void Dispose() => _cli.Dispose();

    [Fact]
    public async Task Nearest_config_walking_up_is_used()
    {
        _cli.Write(".l3lint.yaml", "version: 1\nrules:\n  disable: [VISA-L3-010]\n");
        _cli.Write("deep/er/p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "deep/er/p.json");

        run.Out.ShouldNotContain("VISA-L3-010");
        run.Out.ShouldContain("0 warnings");
    }

    [Fact]
    public async Task Explicit_config_is_used_and_flags_beat_it()
    {
        _cli.Write("conf/team.yaml", "version: 1\nrules:\n  severity:\n    VISA-L3-010: error\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var fromConfig = await _cli.RunAsync("lint", "p.json", "--config", "conf/team.yaml");
        var flagWins = await _cli.RunAsync("lint", "p.json", "--config", "conf/team.yaml", "--severity", "VISA-L3-010=information");

        fromConfig.ExitCode.ShouldBe(1);
        flagWins.ExitCode.ShouldBe(0);
        flagWins.Out.ShouldContain("1 information");
    }

    [Fact]
    public async Task Config_fail_on_and_formats_apply()
    {
        _cli.Write(".l3lint.yaml", "version: 1\noutput:\n  failOn: warning\n  formats:\n    - text\n    - json=reports/l3lint.json\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json");

        run.ExitCode.ShouldBe(1);
        _cli.Exists("reports/l3lint.json").ShouldBeTrue();
    }

    [Fact]
    public async Task Missing_explicit_config_is_a_usage_error()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json", "--config", "nope.yaml")).ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Unknown_key_stops_the_run_with_exit_three_and_the_path()
    {
        _cli.Write(".l3lint.yaml", "version: 1\nrules:\n  disabel: [GEN-DQ-008]\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json");

        run.ExitCode.ShouldBe(3);
        run.Error.ShouldContain(".l3lint.yaml");
        run.Error.ShouldContain("/rules");
    }

    [Fact]
    public async Task Invalid_yaml_stops_the_run_with_exit_three()
    {
        _cli.Write(".l3lint.yaml", "version: 1\nrules: [unclosed\n");
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json")).ExitCode.ShouldBe(3);
    }

    [Fact]
    public async Task Lock_file_mismatch_stops_the_run_with_exit_three()
    {
        _cli.Write(".l3lint.yaml", "version: 1\n");
        _cli.Write(".l3lint.lock", "version: 1\npacks:\n  l3lint.rules.visa: 9.9.9\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json");

        run.ExitCode.ShouldBe(3);
        run.Error.ShouldContain("l3lint.rules.visa");
    }

    [Fact]
    public async Task Matching_lock_file_passes()
    {
        _cli.Write(".l3lint.yaml", "version: 1\n");
        _cli.Write(".l3lint.lock", "version: 1\npacks:\n  l3lint.rules.visa: 1.0.0\n  l3lint.rules.general: 1.0.0\n");
        _cli.Write("p.json", Payloads.SpecExample);

        (await _cli.RunAsync("lint", "p.json")).ExitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Config_context_defaults_fill_gaps_in_the_payload()
    {
        _cli.Write(".l3lint.yaml", "version: 1\ncontext:\n  cardProduct: consumer\n");
        _cli.Write("p.json", Payloads.Mutate(p => p["card"]!.AsObject().Remove("product")));

        var run = await _cli.RunAsync("lint", "p.json");

        run.Out.ShouldContain("GEN-ELIG-001");
        run.Out.ShouldNotContain("VISA-L3-010");
    }

    [Fact]
    public async Task Rate_table_missing_from_config_is_a_configuration_error()
    {
        _cli.Write(".l3lint.yaml", "version: 1\nrates: config/missing.yaml\n");
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json");

        run.ExitCode.ShouldBe(3);
        run.Error.ShouldContain("missing.yaml");
    }

    [Fact]
    public async Task Unknown_processor_profile_is_a_configuration_error()
    {
        _cli.Write("p.json", Payloads.SpecExample);

        var run = await _cli.RunAsync("lint", "p.json", "--processor", "no-such-profile");

        run.ExitCode.ShouldBeOneOf(2, 3);
        run.Error.ShouldContain("no-such-profile");
    }
}
