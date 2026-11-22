namespace L3Lint.Cli;

/// <summary>
/// The process environment the CLI runs in, so tests can run commands in process with captured streams.
/// </summary>
internal sealed record CliEnvironment
{
    public required TextWriter Out { get; init; }

    public required TextWriter Error { get; init; }

    public required Func<Stream> OpenStandardInput { get; init; }

    public required string WorkingDirectory { get; init; }

    public required Func<string, string?> GetVariable { get; init; }

    /// <summary>Whether stdout is a terminal; color is used only then (and never with NO_COLOR or --no-color).</summary>
    public bool OutputIsTerminal { get; init; }

    public TimeProvider Time { get; init; } = TimeProvider.System;

    /// <summary>Creates the environment of the current process. Output uses LF line endings on every operating system.</summary>
    public static CliEnvironment FromConsole()
    {
        Console.Out.NewLine = "\n";
        Console.Error.NewLine = "\n";
        return new()
        {
            Out = Console.Out,
            Error = Console.Error,
            OpenStandardInput = Console.OpenStandardInput,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            GetVariable = Environment.GetEnvironmentVariable,
            OutputIsTerminal = !Console.IsOutputRedirected,
        };
    }
}
