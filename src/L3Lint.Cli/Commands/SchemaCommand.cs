using System.CommandLine;
using System.Globalization;
using L3Lint.Core.Schemas;
using L3Lint.Hosting;

namespace L3Lint.Cli.Commands;

/// <summary><c>l3lint schema &lt;name&gt;</c>: prints a JSON Schema (canonical, results, config, rulepack and the supporting ones).</summary>
internal static class SchemaCommand
{
    public static Command Create(CommandContext context)
    {
        var name = new Argument<string>("name") { Description = string.Format(CultureInfo.InvariantCulture, Resources.Strings.ArgSchemaName, string.Join(", ", SchemaCatalog.Names)) };
        var command = new Command("schema", Resources.Strings.CmdSchema) { name };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var requested = result.GetValue(name)!;
            if (!SchemaCatalog.TryGetText(requested, out var text))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.SchemaUnknown, requested, string.Join(", ", SchemaCatalog.Names)));
            }

            context.Environment.Out.Write(text.EndsWith('\n') ? text : text + "\n");
            return Task.FromResult(ExitCodes.Clean);
        }));
        return command;
    }
}
