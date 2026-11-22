using System.CommandLine;

namespace L3Lint.Cli;

/// <summary>Options every command accepts: color, verbosity and logging (Interfaces tab, lint options table).</summary>
internal sealed class GlobalOptions
{
    public Option<bool> NoColor { get; } = new("--no-color") { Description = Resources.Strings.OptNoColor, Recursive = true };

    public Option<bool> Quiet { get; } = new("--quiet", "-q") { Description = Resources.Strings.OptQuiet, Recursive = true };

    public Option<bool> Verbose { get; } = new("--verbose", "-v") { Description = Resources.Strings.OptVerbose, Recursive = true };

    public Option<string> LogLevel { get; } = new("--log-level")
    {
        Description = Resources.Strings.OptLogLevel,
        DefaultValueFactory = _ => "warn",
        Recursive = true,
    };

    public Option<string> LogFormat { get; } = new("--log-format")
    {
        Description = Resources.Strings.OptLogFormat,
        DefaultValueFactory = _ => "text",
        Recursive = true,
    };

    public Option<bool> LogValues { get; } = new("--log-values") { Description = Resources.Strings.OptLogValues, Recursive = true };

    public void AddTo(RootCommand root)
    {
        root.Add(NoColor);
        root.Add(Quiet);
        root.Add(Verbose);
        root.Add(LogLevel);
        root.Add(LogFormat);
        root.Add(LogValues);
    }
}
