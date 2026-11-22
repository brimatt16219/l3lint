using System.CommandLine;
using System.Globalization;
using L3Lint.Core.Model;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting.Mapping;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint convert &lt;input&gt; --to canonical</c>: maps a gateway payload to canonical JSON with the path map,
/// derived fields and adapter notes (US-02, FR-007). Card numbers are masked first.
/// </summary>
/// <remarks>
/// A JSON file gives one indented document. NDJSON gives one line per record. With <c>--canonical-only</c> the
/// output is canonical NDJSON, one transaction per line, ready to lint with <c>--format canonical</c>.
/// </remarks>
internal static class ConvertCommand
{
    public static Command Create(CommandContext context)
    {
        var input = InputArguments.RejectOptionLikeTokens(new Argument<string>("input") { Description = Resources.Strings.ArgConvertInput });
        var options = new ContextOptions();
        var to = new Option<string>("--to") { Description = Resources.Strings.OptTo, HelpName = "canonical", Required = true };
        var canonicalOnly = new Option<bool>("--canonical-only") { Description = Resources.Strings.OptCanonicalOnly };
        var command = new Command("convert", Resources.Strings.CmdConvert) { input };
        options.AddTo(command);
        command.Add(to);
        command.Add(canonicalOnly);
        command.SetAction((result, cancellationToken) => context.GuardAsync(result, async () =>
        {
            var environment = context.Environment;
            var target = result.GetValue(to)!;
            if (!string.Equals(target, "canonical", StringComparison.OrdinalIgnoreCase))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorConvertTarget, target));
            }

            using var loggerFactory = context.CreateLoggerFactory(result);
            var configuration = options.LoadConfiguration(result, environment.WorkingDirectory);
            var settings = SettingsResolver.Resolve(configuration, options.ToOverrides(result, verbose: false, [], failOn: null, maxFindings: null), environment.WorkingDirectory);
            var linter = L3LintFactory.Create(settings, loggerFactory, environment.Time);
            var file = await InputFile.ReadAsync(result.GetValue(input)!, environment.WorkingDirectory, environment.OpenStandardInput, settings.Options, cancellationToken).ConfigureAwait(false);
            var only = result.GetValue(canonicalOnly);
            var output = new System.Text.StringBuilder();
            foreach (var document in file.Documents)
            {
                var mapped = linter.Map(document, settings.Options);
                if (settings.Options.StrictPci && mapped.CardNumbers.Any(m => !m.IsCodeField))
                {
                    return context.Fail(ExitCodes.Findings, Resources.Strings.ErrorConvertStrictPci, null);
                }

                if (only)
                {
                    foreach (var transaction in mapped.Transactions)
                    {
                        output.Append(System.Text.Encoding.UTF8.GetString(CanonicalJson.SerializeToUtf8Bytes(transaction.Transaction))).Append('\n');
                    }
                }
                else
                {
                    output.Append(MappingJson.Render(mapped, indented: file.Kind != InputFileKind.Ndjson)).Append('\n');
                }
            }

            environment.Out.Write(output.ToString());
            return ExitCodes.Clean;
        }));
        return command;
    }
}
