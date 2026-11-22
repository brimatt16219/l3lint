using System.CommandLine;
using System.Globalization;
using L3Lint.Hosting;
using L3Lint.Reporting.Rules;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint explain &lt;ruleId&gt;</c>: requirement, rationale, sources, confidence, dates and examples (US-05, FR-026).
/// </summary>
internal static class ExplainCommand
{
    public static Command Create(CommandContext context)
    {
        var ruleId = new Argument<string>("ruleId") { Description = Resources.Strings.ArgRuleId };
        var output = new Option<string>("--output", "-o") { Description = Resources.Strings.OptExplainOutput, HelpName = "text|json", DefaultValueFactory = _ => "text" };
        var rulePack = new Option<string[]>("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path" };
        var command = new Command("explain", Resources.Strings.CmdExplain) { ruleId, output, rulePack };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var linter = Linters.Create(context, result, result.GetValue(rulePack) ?? []);
            var id = result.GetValue(ruleId)!;
            if (!linter.TryGetRule(id, out var rule))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleUnknown, id));
            }

            var format = (result.GetValue(output) ?? "text").ToLowerInvariant();
            context.Environment.Out.Write(format switch
            {
                "json" => RuleJson.Render(rule, includeExamples: true) + "\n",
                "text" => RuleMarkdown.Render(rule),
                _ => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorTextOrJson, format)),
            });
            return Task.FromResult(ExitCodes.Clean);
        }));
        return command;
    }
}
