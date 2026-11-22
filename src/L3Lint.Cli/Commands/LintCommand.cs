using System.CommandLine;
using System.Globalization;
using L3Lint.Cli.Output;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;
using Microsoft.Extensions.Logging;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint lint &lt;inputs...&gt;</c>: lints files, folders, globs, NDJSON or stdin (US-01 to US-04, US-13, US-14).
/// </summary>
internal static partial class LintCommand
{
    public static Command Create(CommandContext context)
    {
        var inputs = InputArguments.RejectOptionLikeTokens(new Argument<string[]>("inputs") { Description = Resources.Strings.ArgInputs, Arity = ArgumentArity.OneOrMore });
        var options = new ContextOptions();
        var failOn = new Option<string>("--fail-on") { Description = Resources.Strings.OptFailOn, HelpName = "error|warning|information|none" };
        var output = new Option<string[]>("--output", "-o") { Description = Resources.Strings.OptOutput, HelpName = "kind[=path]" };
        var maxFindings = new Option<int?>("--max-findings") { Description = Resources.Strings.OptMaxFindings, HelpName = "n" };
        var annotations = new Option<string>("--annotations") { Description = Resources.Strings.OptAnnotations, HelpName = "github|none", DefaultValueFactory = _ => "none" };

        var command = new Command("lint", Resources.Strings.CmdLint);
        command.Add(inputs);
        options.AddTo(command);
        command.Add(failOn);
        command.Add(output);
        command.Add(maxFindings);
        command.Add(annotations);
        command.SetAction((result, cancellationToken) => context.GuardAsync(result, async () =>
        {
            var environment = context.Environment;
            var verbose = result.GetValue(context.Global.Verbose);
            var annotationMode = (result.GetValue(annotations) ?? "none").ToLowerInvariant();
            if (annotationMode is not ("github" or "none"))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorAnnotations, annotationMode));
            }

            using var loggerFactory = context.CreateLoggerFactory(result);
            var configuration = options.LoadConfiguration(result, environment.WorkingDirectory);
            var overrides = options.ToOverrides(result, verbose, result.GetValue(output) ?? [], result.GetValue(failOn), result.GetValue(maxFindings));
            var settings = SettingsResolver.Resolve(configuration, overrides, environment.WorkingDirectory);
            ReportOutput.EnsureAvailable(settings.Outputs);
            var logValues = result.GetValue(context.Global.LogValues);
            if (logValues)
            {
                environment.Error.WriteLine(Resources.Strings.WarningLogValues);
            }

            var linter = L3LintFactory.Create(settings, loggerFactory, environment.Time);
            var runner = new LintRunner(linter, loggerFactory.CreateLogger<LintRunner>());
            var report = await runner.RunAsync(
                new LintRunRequest
                {
                    Inputs = result.GetValue(inputs) ?? [],
                    Options = settings.Options,
                    WorkingDirectory = environment.WorkingDirectory,
                    StandardInput = environment.OpenStandardInput,
                    MaxFindings = settings.MaxFindings,
                    ConfigHash = settings.ConfigHash,
                    Notes = verbose ? settings.Notes : [],
                    Profile = settings.ProfileName,
                },
                cancellationToken).ConfigureAwait(false);

            if (logValues)
            {
                var logger = loggerFactory.CreateLogger("L3Lint.Findings");
                foreach (var finding in report.AllFindings())
                {
                    LogFindingValue(logger, finding.RuleId, finding.Path.Canonical, finding.Actual);
                }
            }

            var writerOptions = new ReportWriterOptions
            {
                Color = context.UseColor(result),
                Verbose = verbose,
                Quiet = result.GetValue(context.Global.Quiet),
                BaseDirectory = environment.WorkingDirectory,
            };
            if (!ReportOutput.Write(report, settings.Outputs, writerOptions, environment))
            {
                return ExitCodes.Internal;
            }

            if (annotationMode == "github")
            {
                GitHubAnnotations.Write(report, environment.Out);
            }

            return report.ExitCode;
        }));
        return command;
    }

    // Payload values reach logs only with --log-values (FR-033, NFR-005); values are already masked.
    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "{RuleId} at {Path}: {Actual}")]
    private static partial void LogFindingValue(ILogger logger, string ruleId, string path, string? actual);
}
