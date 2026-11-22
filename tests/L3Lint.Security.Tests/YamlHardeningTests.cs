using System.Diagnostics;
using System.Text;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Profiles;
using L3Lint.Core.Rules;
using L3Lint.Core.Yaml;
using L3Lint.Hosting;

namespace L3Lint.Security.Tests;

/// <summary>
/// YAML deserialization attacks (Architecture tab: "YamlDotNet configured without type tags; config bound to plain
/// option classes"; NFR-006). The <c>.l3lint.yaml</c> loader is the main entry point; rule packs, CSV maps and
/// processor profiles share the same YAML reader and are checked too.
/// </summary>
public sealed class YamlHardeningTests
{
    /// <summary>Nine levels of nine aliases each: about 387 million values if expanded.</summary>
    private static readonly string AliasBomb = string.Join("\n",
        ["a0: &a0 [lol, lol, lol, lol, lol, lol, lol, lol, lol]",
         .. Enumerable.Range(1, 8).Select(i => $"a{i}: &a{i} [{string.Join(", ", Enumerable.Repeat($"*a{i - 1}", 9))}]")]) + "\n";

    [Theory]
    [InlineData("context:\n  brand: !!python/object/apply:os.system [\"calc\"]\n")]
    [InlineData("context: !custom\n  brand: visa\n")]
    [InlineData("context:\n  mcc: !!binary NTA0Nw==\n")]
    [InlineData("context:\n  brand: !<tag:example.com,2026:payload> visa\n")]
    [InlineData("rules:\n  disable: !!set { GEN-DQ-008 }\n")]
    [InlineData("output:\n  failOn: !!timestamp 2026-10-19\n")]
    public void Config_refuses_type_tags(string yaml)
    {
        var exception = Should.Throw<ConfigurationException>(() => L3LintConfiguration.Parse(yaml, "tags.yaml"));

        exception.InnerException.ShouldBeOfType<YamlFormatException>();
    }

    [Fact]
    public void Config_accepts_the_standard_scalar_tags()
    {
        // Positive control: the core schema tags are plain data, so they are not refused.
        var configuration = L3LintConfiguration.Parse("context:\n  brand: !!str visa\n  merchantCountry: !!str US\n", "standard.yaml");

        configuration.Brand.ShouldBe("visa");
        configuration.MerchantCountry.ShouldBe("US");
    }

    [Fact]
    public void Config_alias_bomb_is_refused_instead_of_expanded()
    {
        var exception = Should.Throw<ConfigurationException>(() => L3LintConfiguration.Parse(AliasBomb, "bomb.yaml"));

        exception.InnerException.ShouldBeOfType<YamlFormatException>();
    }

    [Fact]
    public void Config_small_aliases_still_work()
    {
        // Positive control: the limit is on expanded size, not on aliases as such.
        var configuration = L3LintConfiguration.Parse("rules:\n  disable: &skip [GEN-DQ-008]\n  only: *skip\n", "aliases.yaml");

        configuration.DisabledRules.ShouldBe(["GEN-DQ-008"]);
        configuration.OnlyRules.ShouldBe(["GEN-DQ-008"]);
    }

    [Theory]
    [InlineData("context:\n  brand: visa\n  brand: amex\n")]
    [InlineData("context:\n  brand: visa\n  'brand': amex\n")]
    [InlineData("output:\n  failOn: error\noutput:\n  failOn: none\n")]
    [InlineData("rules:\n  severity:\n    GEN-DQ-007: error\n    GEN-DQ-007: information\n")]
    public void Config_refuses_duplicate_keys(string yaml)
    {
        var exception = Should.Throw<ConfigurationException>(() => L3LintConfiguration.Parse(yaml, "duplicates.yaml"));

        exception.InnerException.ShouldBeOfType<YamlFormatException>();
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public void Config_nested_deeper_than_the_JSON_depth_limit_is_a_configuration_error(int depth)
    {
        // A config this deep is never valid; it must be refused as a configuration error (exit 3 with the file
        // name), not escape as an unrelated exception. Kept well below the depth that overflows the stack.
        var yaml = "context: " + new string('[', depth) + new string(']', depth) + "\n";

        Should.Throw<ConfigurationException>(() => L3LintConfiguration.Parse(yaml, "deep.yaml"));
    }

    [Fact]
    public void Rule_pack_alias_bomb_is_refused()
    {
        using var folder = new TempFolder();
        var body = "pack:\n  id: l3lint.rules.local\n  version: 1.0.0\n  brand: general\n  publishedOn: 2026-10-25\n" + AliasBomb;
        TestPacks.Write(folder.Path, "local.rules.yaml", body);

        var exception = Should.Throw<RulePackException>(() => new LinterBuilder().AddRulePack(new DirectoryRulePackSource(folder.Path)).Build());

        exception.InnerException.ShouldBeOfType<YamlFormatException>();
    }

    [Fact]
    public void Csv_map_with_a_type_tag_is_refused()
    {
        var map = "format: csv\ngroupBy: !!python/name:os.system invoice\ntransaction:\n  id: invoice\n";
        var options = LintOptions.Default with { Format = "csv", Adapter = AdapterOptions.Default with { CsvMap = map, CsvMapName = "map.yaml" } };
        var linter = L3LintFactory.CreateBuilder().Build();

        Should.Throw<InputException>(() => linter.Lint(RawDocument.FromCsv(Encoding.UTF8.GetBytes("invoice\nINV-1\n"), "rows.csv"), options));
    }

    [Fact]
    public void Processor_profile_with_an_alias_bomb_is_refused()
    {
        var exception = Should.Throw<RulePackException>(() => ProcessorProfiles.Parse("id: local\n" + AliasBomb, "profile.yaml"));

        exception.InnerException.ShouldBeOfType<YamlFormatException>();
    }

    [Fact]
    public async Task A_config_nested_thousands_deep_is_refused_without_crashing_the_process()
    {
        // Runs the real CLI in a child process: a stack overflow cannot be caught, so in process it would end the
        // whole test run. The CLI must refuse the file with exit 3 (configuration error), not crash.
        using var folder = new TempFolder();
        var payload = Path.Combine(folder.Path, "payload.json");
        var config = Path.Combine(folder.Path, "deep.l3lint.yaml");
        await File.WriteAllTextAsync(payload, SecretPayloads.SpecExample, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(config, "context: " + new string('[', 10_000) + new string(']', 10_000) + "\n", TestContext.Current.CancellationToken);

        var (exitCode, error) = await ChildCli.RunAsync(folder.Path, "lint", payload, "--config", config);

        exitCode.ShouldBe(3, $"The CLI should refuse the config with exit 3. It exited with {exitCode}; standard error starts:\n{error[..Math.Min(error.Length, 600)]}");
    }
}

/// <summary>Runs the built CLI (l3lint.dll next to the test binaries) as a separate process.</summary>
internal static class ChildCli
{
    public static async Task<(int ExitCode, string Error)> RunAsync(string workingDirectory, params string[] args)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "l3lint.dll"));
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("dotnet did not start");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;
        return (process.ExitCode, await error);
    }
}

/// <summary>A temporary folder that deletes itself.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "l3lint-security-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
