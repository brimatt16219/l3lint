using System.Reflection;
using System.Text;

namespace L3Lint.Security.Tests;

/// <summary>The outcome of one CLI run: exit code, standard output and standard error.</summary>
internal sealed record CliRun(int ExitCode, string Out, string Error)
{
    /// <summary>Everything the run printed, on both streams.</summary>
    public string All => Out + "\n" + Error;
}

/// <summary>Tests that swap the process console run one at a time, after the parallel tests.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleGroup
{
    public const string Name = "Console";
}

/// <summary>
/// Runs the real <c>l3lint</c> entry point in this process with the console redirected, in a temporary working
/// folder.
/// </summary>
/// <remarks>
/// The CLI's loggers write to <see cref="Console.Error"/> itself, not to the error writer that
/// <c>tests/L3Lint.Cli.Tests/Cli.cs</c> passes in, so a log scan has to capture the process console. That is why
/// these tests run the program's entry point with <see cref="Console.SetError(TextWriter)"/> rather than reuse the
/// Cli.Tests helper (which also needs internals that only Cli.Tests can see). Tests that use this class belong to
/// <see cref="ConsoleGroup"/>, so nothing else writes to the console meanwhile.
/// </remarks>
internal sealed class ConsoleCli : IDisposable
{
    private static readonly MethodInfo EntryPoint = Assembly.Load(new AssemblyName("l3lint")).EntryPoint
        ?? throw new InvalidOperationException("The l3lint assembly has no entry point.");

    public ConsoleCli()
    {
        WorkingDirectory = Path.Combine(Path.GetTempPath(), "l3lint-security-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WorkingDirectory);

        // An explicit empty configuration, so no .l3lint.yaml above the temp folder can change a run.
        Write(".l3lint.yaml", "# security tests: defaults only\n");
    }

    public string WorkingDirectory { get; }

    public string PathOf(string relativePath) => Path.Combine(WorkingDirectory, relativePath);

    public string Write(string relativePath, string content)
    {
        var full = PathOf(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, new UTF8Encoding(false));
        return full;
    }

    public string Read(string relativePath) => File.ReadAllText(PathOf(relativePath));

    public async Task<CliRun> RunAsync(params string[] args)
    {
        var output = new StringWriter { NewLine = "\n" };
        var error = new StringWriter { NewLine = "\n" };
        var (oldOut, oldError, oldDirectory) = (Console.Out, Console.Error, Environment.CurrentDirectory);
        try
        {
            Console.SetOut(TextWriter.Synchronized(output));
            Console.SetError(TextWriter.Synchronized(error));
            Environment.CurrentDirectory = WorkingDirectory;
            var code = await Task.Run(() => (int)EntryPoint.Invoke(null, [args])!);
            return new CliRun(code, output.ToString(), error.ToString());
        }
        finally
        {
            Environment.CurrentDirectory = oldDirectory;
            Console.SetOut(oldOut);
            Console.SetError(oldError);
        }
    }

    public void Dispose() => Directory.Delete(WorkingDirectory, recursive: true);
}
