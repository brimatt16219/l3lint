using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Fixes;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Hosting;
using L3Lint.Hosting.Fixes;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting.Fixes;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint fix &lt;input&gt;</c>: prints an RFC 6902 patch of mechanical fixes (<c>--dry-run</c>, the default)
/// or writes a patched copy (<c>--write</c>) (US-06, FR-027, ADR-0022, ADR-0047).
/// </summary>
/// <remarks>
/// A JSON file gives one patch document. NDJSON gives one patch per line, in input order. CSV patches target the
/// canonical form, because CSV cells have no JSON Pointer, and <c>--write</c> writes canonical NDJSON. The copy is
/// built from the masked document, so a card number never reaches the output file. Findings a person must decide
/// go to standard error.
/// </remarks>
internal static class FixCommand
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, IndentSize = 2, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static Command Create(CommandContext context)
    {
        var input = InputArguments.RejectOptionLikeTokens(new Argument<string>("input") { Description = Resources.Strings.ArgFixInput });
        var options = new ContextOptions();
        var dryRun = new Option<bool>("--dry-run") { Description = Resources.Strings.OptDryRun };
        var write = new Option<string>("--write") { Description = Resources.Strings.OptWrite, HelpName = "path" };
        var fixAmount = new Option<bool>("--fix-amount") { Description = Resources.Strings.OptFixAmount };
        var command = new Command("fix", Resources.Strings.CmdFix) { input };
        options.AddTo(command);
        command.Add(dryRun);
        command.Add(write);
        command.Add(fixAmount);
        command.SetAction((result, cancellationToken) => context.GuardAsync(result, async () =>
        {
            var environment = context.Environment;
            var target = result.GetValue(write);
            if (target is not null && result.GetValue(dryRun))
            {
                throw new UsageException(Resources.Strings.ErrorDryRunWrite);
            }

            var source = result.GetValue(input)!;
            if (target is not null && source != "-"
                && string.Equals(Path.GetFullPath(Path.Combine(environment.WorkingDirectory, target)), Path.GetFullPath(Path.Combine(environment.WorkingDirectory, source)), StringComparison.OrdinalIgnoreCase))
            {
                throw new UsageException(Resources.Strings.ErrorWriteOverInput);
            }

            using var loggerFactory = context.CreateLoggerFactory(result);
            var configuration = options.LoadConfiguration(result, environment.WorkingDirectory);
            var settings = SettingsResolver.Resolve(configuration, options.ToOverrides(result, verbose: false, [], failOn: null, maxFindings: null), environment.WorkingDirectory);
            var linter = L3LintFactory.Create(settings, loggerFactory, environment.Time);
            var file = await InputFile.ReadAsync(source, environment.WorkingDirectory, environment.OpenStandardInput, settings.Options, cancellationToken).ConfigureAwait(false);

            var patches = new List<IReadOnlyList<PatchOperation>>();
            var patched = new List<JsonNode?>();
            var needsHuman = new List<Finding>();
            foreach (var document in file.Documents)
            {
                var linted = linter.Lint(document, settings.Options);
                if (linted.InputErrors.Count > 0)
                {
                    var error = linted.InputErrors[0];
                    throw new InputException(error.Kind, error.Message, error.SourceName, error.Line ?? 0, 0);
                }

                if (linted.StoppedForCardNumber)
                {
                    return context.Fail(ExitCodes.Findings, Resources.Strings.ErrorFixStrictPci, null);
                }

                var mapped = linter.Map(document, settings.Options);
                var plans = FixPlanner.Plan(linted, result.GetValue(fixAmount));
                needsHuman.AddRange(plans.SelectMany(p => p.NeedsHuman));
                if (file.Kind == InputFileKind.Csv)
                {
                    foreach (var plan in plans)
                    {
                        patches.Add(plan.Operations);
                        patched.Add(JsonPatch.Apply(JsonNode.Parse(CanonicalJson.Serialize(mapped.Transactions[plan.TransactionIndex].Transaction))!, plan.Operations));
                    }
                }
                else
                {
                    var operations = plans.SelectMany(p => p.Operations).ToList();
                    patches.Add(operations);
                    patched.Add(JsonPatch.Apply(JsonNode.Parse(mapped.Document.GetText())!, operations));
                }
            }

            if (target is null)
            {
                var text = file.Kind == InputFileKind.Json
                    ? PatchJson.Render(patches.Count == 0 ? [] : patches[0], indented: true) + "\n"
                    : string.Concat(patches.Select(p => PatchJson.Render(p, indented: false) + "\n"));
                environment.Out.Write(text);
            }
            else
            {
                var text = file.Kind == InputFileKind.Json
                    ? patched[0]!.ToJsonString(Indented) + "\n"
                    : string.Concat(patched.Select(n => n!.ToJsonString(Compact) + "\n"));
                var full = Path.GetFullPath(Path.Combine(environment.WorkingDirectory, target));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllTextAsync(full, text, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                if (!result.GetValue(context.Global.Quiet))
                {
                    environment.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.FixWritten, patches.Sum(p => p.Count), target));
                }
            }

            WriteNeedsHuman(context, result, needsHuman);
            return ExitCodes.Clean;
        }));
        return command;
    }

    private static void WriteNeedsHuman(CommandContext context, ParseResult result, List<Finding> findings)
    {
        if (findings.Count == 0 || result.GetValue(context.Global.Quiet))
        {
            return;
        }

        var error = context.Environment.Error;
        error.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.FixNeedsHuman, findings.Count));
        foreach (var finding in findings)
        {
            error.WriteLine("  " + WireNames.Of(finding.Severity) + "  " + finding.RuleId + "  " + finding.Path.Source + "  " + finding.Message);
            error.WriteLine("      " + string.Format(CultureInfo.InvariantCulture, Resources.Strings.FixHint, finding.Fix));
        }
    }
}
