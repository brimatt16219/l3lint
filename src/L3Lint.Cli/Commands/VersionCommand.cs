using System.CommandLine;
using System.Globalization;
using L3Lint.Core;

namespace L3Lint.Cli.Commands;

/// <summary><c>l3lint version</c>: engine and rule pack versions.</summary>
internal static class VersionCommand
{
    public static Command Create(CommandContext context)
    {
        var rulePack = new Option<string[]>("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path" };
        var command = new Command("version", Resources.Strings.CmdVersion) { rulePack };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var linter = Linters.Create(context, result, result.GetValue(rulePack) ?? []);
            var output = context.Environment.Out;
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.VersionLine, EngineInfo.Version));
            foreach (var pack in linter.RulePacks)
            {
                output.WriteLine("  " + pack.Id + " " + pack.Version + (pack.Origin == "embedded" ? string.Empty : " (" + pack.Origin + ")"));
            }

            return Task.FromResult(ExitCodes.Clean);
        }));
        return command;
    }
}
