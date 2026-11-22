using System.CommandLine;
using L3Lint.Cli.Commands;

namespace L3Lint.Cli;

/// <summary>
/// Builds the command tree (Interfaces tab, "CLI": eight commands) and runs it. Parse errors are usage errors
/// (exit 2) with a one-line hint.
/// </summary>
internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, CliEnvironment environment)
    {
        var global = new GlobalOptions();
        var root = new RootCommand(Resources.Strings.RootDescription);
        global.AddTo(root);
        var context = new CommandContext(environment, global);
        root.Add(LintCommand.Create(context));
        root.Add(ExplainCommand.Create(context));
        root.Add(FixCommand.Create(context));
        root.Add(ConvertCommand.Create(context));
        root.Add(RulesCommand.Create(context));
        root.Add(SchemaCommand.Create(context));
        root.Add(InitCommand.Create(context));
        root.Add(VersionCommand.Create(context));

        var result = root.Parse(args);
        if (result.Errors.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                environment.Error.WriteLine(Resources.Strings.ErrorPrefix + error.Message);
            }

            environment.Error.WriteLine(Resources.Strings.HintHelp);
            return ExitCodes.Usage;
        }

        var configuration = new InvocationConfiguration
        {
            Output = environment.Out,
            Error = environment.Error,
            EnableDefaultExceptionHandler = false,
        };
        return await result.InvokeAsync(configuration).ConfigureAwait(false);
    }
}
