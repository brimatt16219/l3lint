using System.CommandLine;
using L3Lint.Core;
using L3Lint.Hosting;

namespace L3Lint.Cli.Commands;

/// <summary>Builds a linter for commands that do not lint files (explain, rules, version, init).</summary>
internal static class Linters
{
    public static ILinter Create(CommandContext context, ParseResult result, IReadOnlyList<string> rulePacks)
    {
        var settings = new ResolvedSettings
        {
            Options = LintOptions.Default,
            LocalPacks = [.. rulePacks.Select(p => Path.GetFullPath(Path.Combine(context.Environment.WorkingDirectory, p)))],
        };
        using var loggerFactory = context.CreateLoggerFactory(result);
        return L3LintFactory.Create(settings, loggerFactory, context.Environment.Time);
    }
}
