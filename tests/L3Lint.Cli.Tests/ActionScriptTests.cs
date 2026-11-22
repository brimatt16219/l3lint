using System.Diagnostics;
using System.Text;

namespace L3Lint.Cli.Tests;

/// <summary>
/// Runs the GitHub Action's <c>scripts/run.sh</c> against the freshly built CLI, the way the composite action
/// does, and checks its outputs for a green and a red run (Interfaces tab "GitHub Action", FR-030, US-07).
/// </summary>
public sealed class ActionScriptTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "l3lint-action-tests", Guid.NewGuid().ToString("N"));

    public ActionScriptTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "ws", "fixtures", "payments"));
        Directory.CreateDirectory(Path.Combine(_root, "tmp"));
        File.WriteAllText(Path.Combine(_root, "ws", "fixtures", "payments", "good.json"), Payloads.SpecExample);
        File.WriteAllText(Path.Combine(_root, "ws", "fixtures", "payments", "bad.json"), Payloads.MissingCommodityCode);

        // The action installs the tool; here a wrapper runs the CLI built with these tests.
        var cli = Path.Combine(AppContext.BaseDirectory, "l3lint.dll").Replace('\\', '/');
        File.WriteAllText(Path.Combine(_root, "l3lint"), "#!/usr/bin/env bash\nexec dotnet \"" + cli + "\" \"$@\"\n");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task Green_run_passes_and_writes_sarif_summary_and_counts()
    {
        var run = await RunAsync(new() { ["L3LINT_PATHS"] = "fixtures/payments/good.json" });

        run.ExitCode.ShouldBe(0);
        run.Outputs["exit-code"].ShouldBe("0");
        run.Outputs["errors"].ShouldBe("0");
        run.Outputs["warnings"].ShouldBe("1");
        run.Outputs["sarif-written"].ShouldBe("true");
        File.Exists(Path.Combine(_root, "ws", "l3lint.sarif")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(_root, "tmp", "summary.md")).ShouldContain("VISA-L3-010");
        run.Log.ShouldContain("1 transaction, 0 errors, 1 warning.");
    }

    [Fact]
    public async Task Red_run_reports_exit_one_and_annotates_the_file()
    {
        var run = await RunAsync(new() { ["L3LINT_PATHS"] = "fixtures/payments/**/*.json" });

        // The script itself always exits 0 so the upload step runs; the action applies exit-code last.
        run.ExitCode.ShouldBe(0);
        run.Outputs["exit-code"].ShouldBe("1");
        run.Outputs["errors"].ShouldBe("1");
        run.Log.ShouldContain("::error file=fixtures/payments/bad.json,");
        run.Log.ShouldContain("title=VISA-L3-004");
    }

    [Fact]
    public async Task Fail_on_none_and_annotations_off_are_honored()
    {
        var run = await RunAsync(new()
        {
            ["L3LINT_PATHS"] = "fixtures/payments/bad.json",
            ["L3LINT_FAIL_ON"] = "none",
            ["L3LINT_ANNOTATIONS"] = "false",
        });

        run.Outputs["exit-code"].ShouldBe("0");
        run.Outputs["errors"].ShouldBe("1");
        run.Log.Split('\n').ShouldNotContain(l => l.StartsWith("::", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("fixtures/../../outside.json")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("")]
    public async Task Paths_outside_the_workspace_or_empty_are_refused(string paths)
    {
        var run = await RunAsync(new() { ["L3LINT_PATHS"] = paths });

        run.Outputs["exit-code"].ShouldBe("2");
        run.Outputs["sarif-written"].ShouldBe("false");
        run.Log.ShouldContain("::error title=l3lint::");
    }

    [Fact]
    public async Task Inputs_are_data_not_shell_code()
    {
        var marker = Path.Combine(_root, "ws", "pwned");

        var run = await RunAsync(new() { ["L3LINT_PATHS"] = "fixtures/payments/good.json\n$(touch pwned).json", ["L3LINT_BRAND"] = "visa; touch pwned" });

        File.Exists(marker).ShouldBeFalse();
        run.Outputs["exit-code"].ShouldBe("2");
    }

    private async Task<ScriptRun> RunAsync(Dictionary<string, string> variables)
    {
        var outputs = Path.Combine(_root, "tmp", "outputs.txt");
        File.WriteAllText(outputs, string.Empty);
        var start = new ProcessStartInfo(Bash())
        {
            WorkingDirectory = Path.Combine(_root, "ws"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(Path.Combine(RepositoryRoot(), "action", "scripts", "run.sh").Replace('\\', '/'));
        start.Environment["L3LINT_BIN"] = Path.Combine(_root, "l3lint").Replace('\\', '/');
        start.Environment["GITHUB_OUTPUT"] = outputs.Replace('\\', '/');
        start.Environment["GITHUB_STEP_SUMMARY"] = Path.Combine(_root, "tmp", "summary.md").Replace('\\', '/');
        start.Environment["RUNNER_TEMP"] = Path.Combine(_root, "tmp").Replace('\\', '/');
        foreach (var (name, value) in variables)
        {
            start.Environment[name] = value;
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var values = File.ReadAllLines(outputs, Encoding.UTF8)
            .Select(l => l.Split('=', 2))
            .ToDictionary(p => p[0], p => p[1], StringComparer.Ordinal);
        return new ScriptRun(process.ExitCode, (await stdout + await stderr).ReplaceLineEndings("\n"), values);
    }

    // Git for Windows ships bash; elsewhere bash is on PATH. Windows' own bash.exe starts WSL, which cannot see these paths.
    private static string Bash()
    {
        var git = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe");
        return OperatingSystem.IsWindows() && File.Exists(git) ? git : "bash";
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("L3Lint.slnx not found above " + AppContext.BaseDirectory);
    }

    private sealed record ScriptRun(int ExitCode, string Log, Dictionary<string, string> Outputs);
}
