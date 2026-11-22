using System.CommandLine;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using L3Lint.Core.Input;
using L3Lint.Core.Rules;
using L3Lint.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace L3Lint.Cli;

/// <summary>Shared services for commands: the environment, global options, logging and error handling.</summary>
internal sealed class CommandContext(CliEnvironment environment, GlobalOptions global)
{
    public CliEnvironment Environment { get; } = environment;

    public GlobalOptions Global { get; } = global;

    /// <summary>Color only on a terminal, never with NO_COLOR set or --no-color (NFR-010).</summary>
    public bool UseColor(ParseResult result) =>
        Environment.OutputIsTerminal && !result.GetValue(Global.NoColor) && string.IsNullOrEmpty(Environment.GetVariable("NO_COLOR"));

    public ILoggerFactory CreateLoggerFactory(ParseResult result)
    {
        var level = (result.GetValue(Global.LogLevel) ?? "warn").ToLowerInvariant() switch
        {
            "error" => LogLevel.Error,
            "warn" or "warning" => LogLevel.Warning,
            "info" or "information" => LogLevel.Information,
            "debug" => LogLevel.Debug,
            "trace" => LogLevel.Trace,
            var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorLogLevel, other)),
        };
        var json = (result.GetValue(Global.LogFormat) ?? "text").ToLowerInvariant() switch
        {
            "json" => true,
            "text" => false,
            var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorLogFormat, other)),
        };

        // The console logger is set up on the first message that passes the level (cold start, NFR-002).
        return new Output.DeferredLoggerFactory(level, () => LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(level);
            if (json)
            {
                builder.AddJsonConsole(o => o.IncludeScopes = false);
            }
            else
            {
                builder.AddSimpleConsole(o => o.SingleLine = true);
            }

            // stdout carries only results; every log level goes to stderr (NFR-012).
            builder.Services.Configure<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions>(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        }));
    }

    /// <summary>Runs a command body and maps failures to exit codes (Architecture tab, "Error taxonomy").</summary>
    public async Task<int> GuardAsync(ParseResult result, Func<Task<int>> body)
    {
        var verbose = result.GetValue(Global.Verbose);
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (UsageException ex)
        {
            return Fail(ExitCodes.Usage, ex.Message, Resources.Strings.HintHelp);
        }
        catch (InputException ex)
        {
            var where = ex.Line > 0 ? $"{ex.SourceName}:{ex.Line.ToString(CultureInfo.InvariantCulture)}:{ex.Column.ToString(CultureInfo.InvariantCulture)}: " : ex.SourceName is null ? string.Empty : ex.SourceName + ": ";
            var candidates = ex is Core.Adapters.FormatDetectionException { Candidates.Count: > 0 } detection
                ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.HintCandidates, string.Join(", ", detection.Candidates))
                : Resources.Strings.HintHelp;
            return Fail(ExitCodes.Usage, where + ex.Message, candidates);
        }
        catch (ConfigurationException ex)
        {
            var where = ex.File is null ? string.Empty : ex.File + (ex.Pointer is null ? ": " : " " + ex.Pointer + ": ");
            return Fail(ExitCodes.Internal, where + ex.Message, null);
        }
        catch (RulePackException ex)
        {
            return Fail(ExitCodes.Internal, ex.Message, Resources.Strings.HintRulePack);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Internal errors print a short, stable ID; the stack trace only with --verbose.
            var id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ex.GetType().FullName + ex.StackTrace)))[..8];
            Environment.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorInternal, id, ex.GetType().Name));
            if (verbose)
            {
                Environment.Error.WriteLine(ex.ToString());
            }

            return ExitCodes.Internal;
        }
    }

    public int Fail(int code, string message, string? hint)
    {
        Environment.Error.WriteLine(Resources.Strings.ErrorPrefix + message);
        if (hint is not null)
        {
            Environment.Error.WriteLine(hint);
        }

        return code;
    }
}
