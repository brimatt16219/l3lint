using System.CommandLine;
using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Hosting;
using L3Lint.Reporting.Rules;
using Spectre.Console;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint rules list | show &lt;id&gt; | test</c>: browse rules and run every rule's examples (US-11, FR-020).
/// </summary>
internal static class RulesCommand
{
    public static Command Create(CommandContext context)
    {
        var command = new Command("rules", Resources.Strings.CmdRules);
        command.Add(List(context));
        command.Add(Show(context));
        command.Add(Test(context));
        return command;
    }

    private static Command List(CommandContext context)
    {
        var brand = new Option<string>("--brand") { Description = Resources.Strings.OptRulesBrand, HelpName = "general|visa|mastercard|amex|discover" };
        var level = new Option<string>("--level") { Description = Resources.Strings.OptRulesLevel, HelpName = "general|L2|L3" };
        var query = new Option<string>("--query") { Description = Resources.Strings.OptRulesQuery, HelpName = "text" };
        var output = new Option<string>("--output", "-o") { Description = Resources.Strings.OptExplainOutput, HelpName = "text|json", DefaultValueFactory = _ => "text" };
        var rulePack = new Option<string[]>("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path" };
        var command = new Command("list", Resources.Strings.CmdRulesList) { brand, level, query, output, rulePack };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var linter = Linters.Create(context, result, result.GetValue(rulePack) ?? []);
            var rules = linter.Rules(new RuleQuery
            {
                Brand = ParseBrand(result.GetValue(brand)),
                Level = ParseLevel(result.GetValue(level)),
                Text = result.GetValue(query),
            });
            var format = (result.GetValue(output) ?? "text").ToLowerInvariant();
            if (format == "json")
            {
                context.Environment.Out.Write(RuleJson.RenderList(rules) + "\n");
                return Task.FromResult(ExitCodes.Clean);
            }

            if (format != "text")
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorTextOrJson, format));
            }

            using var buffer = new StringWriter(CultureInfo.InvariantCulture);
            var console = Console(context, result, buffer);
            var packs = new Table().Border(TableBorder.Ascii).AddColumns(Resources.Strings.ColPack, Resources.Strings.ColVersion, Resources.Strings.ColRules, Resources.Strings.ColSources, Resources.Strings.ColEffective, Resources.Strings.ColOrigin);
            foreach (var pack in linter.RulePacks)
            {
                var effective = (pack.EarliestEffective is { } from ? RuleText.Date(from) : Resources.Strings.ValueAlways)
                    + " .. " + (pack.LatestEnd is { } end ? RuleText.Date(end) : Resources.Strings.ValueOpen);
                packs.AddRow(Markup.Escape(pack.Id), pack.Version, pack.RuleCount.ToString(CultureInfo.InvariantCulture), pack.SourceCount.ToString(CultureInfo.InvariantCulture), effective, Markup.Escape(pack.Origin));
            }

            console.Write(packs);
            var table = new Table().Border(TableBorder.Ascii).AddColumns(Resources.Strings.ColRule, Resources.Strings.ColSeverity, Resources.Strings.ColLevel, Resources.Strings.ColConfidence, Resources.Strings.ColEffective, Resources.Strings.ColTitle);
            foreach (var rule in rules)
            {
                table.AddRow(rule.Id, WireNames.Of(rule.Severity), WireNames.Of(rule.Level), WireNames.Of(rule.Confidence), Markup.Escape(RuleText.Effective(rule)), Markup.Escape(rule.Title));
            }

            console.Write(table);
            context.Environment.Out.Write(buffer.ToString().ReplaceLineEndings("\n"));
            context.Environment.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.RulesCount, rules.Count));
            return Task.FromResult(ExitCodes.Clean);
        }));
        return command;
    }

    private static Command Show(CommandContext context)
    {
        var ruleId = new Argument<string>("ruleId") { Description = Resources.Strings.ArgRuleId };
        var rulePack = new Option<string[]>("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path" };
        var command = new Command("show", Resources.Strings.CmdRulesShow) { ruleId, rulePack };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var linter = Linters.Create(context, result, result.GetValue(rulePack) ?? []);
            var id = result.GetValue(ruleId)!;
            if (!linter.TryGetRule(id, out var rule))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.RuleUnknown, id));
            }

            context.Environment.Out.Write(RuleMarkdown.Render(rule));
            return Task.FromResult(ExitCodes.Clean);
        }));
        return command;
    }

    private static Command Test(CommandContext context)
    {
        var rulePack = new Option<string[]>("--rule-pack") { Description = Resources.Strings.OptRulePack, HelpName = "path" };
        var only = new Option<string[]>("--only") { Description = Resources.Strings.OptOnly, HelpName = "rule|glob", AllowMultipleArgumentsPerToken = true };
        var command = new Command("test", Resources.Strings.CmdRulesTest) { rulePack, only };
        command.SetAction((result, _) => context.GuardAsync(result, () =>
        {
            var linter = Linters.Create(context, result, result.GetValue(rulePack) ?? []);
            var selection = (result.GetValue(only) ?? []).Select(s => s.ToUpperInvariant()).ToList();
            var rules = linter.Rules().Where(r => selection.Count == 0 || selection.Any(s => Matches(s, r.Id))).ToList();
            var outcomes = RuleExampleTester.Run(linter, rules);
            var quiet = result.GetValue(context.Global.Quiet);
            foreach (var outcome in outcomes)
            {
                if (!outcome.Passed || !quiet)
                {
                    context.Environment.Out.WriteLine(outcome.Passed
                        ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.RulesTestPass, outcome.RuleId)
                        : string.Format(CultureInfo.InvariantCulture, Resources.Strings.RulesTestFail, outcome.RuleId, outcome.Detail));
                }
            }

            var failed = outcomes.Count(o => !o.Passed);
            context.Environment.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.RulesTestSummary, outcomes.Count, outcomes.Count - failed, failed));
            return Task.FromResult(failed == 0 ? ExitCodes.Clean : ExitCodes.Findings);
        }));
        return command;
    }

    private static IAnsiConsole Console(CommandContext context, ParseResult result, TextWriter writer)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            Ansi = context.UseColor(result) ? AnsiSupport.Yes : AnsiSupport.No,
            ColorSystem = context.UseColor(result) ? ColorSystemSupport.Standard : ColorSystemSupport.NoColors,
            Interactive = InteractionSupport.No,
        });

        // A fixed width and ASCII borders keep table output identical on every terminal, code page and CI log.
        console.Profile.Width = 160;
        console.Profile.Capabilities.Unicode = false;
        return console;
    }

    private static CardBrand? ParseBrand(string? text) => text?.ToLowerInvariant() switch
    {
        null => null,
        "general" => CardBrand.Unknown,
        var name when CanonicalNames.TryParse(name, out CardBrand brand) && brand != CardBrand.Unknown => brand,
        var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorRulesBrand, other)),
    };

    private static RuleLevel? ParseLevel(string? text) => text?.ToUpperInvariant() switch
    {
        null => null,
        "GENERAL" => RuleLevel.General,
        "L2" => RuleLevel.L2,
        "L3" => RuleLevel.L3,
        var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorRulesLevel, other)),
    };

    private static bool Matches(string pattern, string id) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            id,
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
}
