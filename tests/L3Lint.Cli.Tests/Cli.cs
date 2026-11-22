using System.Text;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Cli.Tests;

/// <summary>The outcome of one in-process CLI run.</summary>
internal sealed record CliRun(int ExitCode, string Out, string Error);

/// <summary>
/// Runs the CLI in process with captured streams, a temporary working folder and a fixed clock
/// (2026-10-19, the date of the spec's worked example).
/// </summary>
internal sealed class Cli : IDisposable
{
    private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);

    public Cli()
    {
        WorkingDirectory = Path.Combine(Path.GetTempPath(), "l3lint-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WorkingDirectory);
    }

    public string WorkingDirectory { get; }

    public bool OutputIsTerminal { get; set; }

    public static string PayloadDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Payloads");

    public static string SpecExample { get; } = Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json");

    public void SetVariable(string name, string value) => _variables[name] = value;

    /// <summary>Copies a payload from Payloads/ into the working folder.</summary>
    public string Copy(string payload, string? target = null)
    {
        var relative = target ?? payload;
        var full = Path.Combine(WorkingDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.Copy(Path.Combine(PayloadDirectory, payload), full, overwrite: true);
        return relative;
    }

    public string Write(string relativePath, string content)
    {
        var target = Path.Combine(WorkingDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, content, new UTF8Encoding(false));
        return relativePath;
    }

    public string Read(string relativePath) => File.ReadAllText(Path.Combine(WorkingDirectory, relativePath));

    public bool Exists(string relativePath) => File.Exists(Path.Combine(WorkingDirectory, relativePath));

    public Task<CliRun> RunAsync(params string[] args) => RunWithInputAsync(string.Empty, args);

    public async Task<CliRun> RunWithInputAsync(string standardInput, params string[] args)
    {
        using var output = new StringWriter { NewLine = "\n" };
        using var error = new StringWriter { NewLine = "\n" };
        var environment = new CliEnvironment
        {
            Out = output,
            Error = error,
            OpenStandardInput = () => new MemoryStream(Encoding.UTF8.GetBytes(standardInput)),
            WorkingDirectory = WorkingDirectory,
            GetVariable = name => _variables.GetValueOrDefault(name),
            OutputIsTerminal = OutputIsTerminal,
            Time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)),
        };
        var code = await CliApplication.RunAsync(args, environment);
        return new CliRun(code, output.ToString(), error.ToString());
    }

    public void Dispose() => Directory.Delete(WorkingDirectory, recursive: true);
}
