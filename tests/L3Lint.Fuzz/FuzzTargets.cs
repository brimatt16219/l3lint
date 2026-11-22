using System.Reflection;
using System.Text;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Rules;
using L3Lint.Core.Yaml;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;

namespace L3Lint.Fuzz;

/// <summary>What a target did with one input.</summary>
public enum FuzzOutcome
{
    /// <summary>The input was processed and every output written.</summary>
    Accepted,

    /// <summary>The input was refused with one of the documented exception types.</summary>
    Rejected,
}

/// <summary>
/// The fuzz targets (T-43, NFR-006). Each one runs an input through a public entry point the way the CLI or the
/// library would. A documented refusal is a normal outcome; any other exception escapes, which libFuzzer and AFL
/// report as a crash and the smoke tests report as a failure.
/// </summary>
public static class FuzzTargets
{
    /// <summary>The target names accepted on the command line.</summary>
    public static IReadOnlyList<string> Names { get; } = ["json", "config", "csv", "rulepack"];

    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    private static readonly Lazy<ILinter> SharedLinter = new(() => L3LintFactory.CreateBuilder().UseTimeProvider(Clock).Build());

    private static readonly Lazy<string> CsvMap = new(() => Resource("csv-map.yaml"));

    private static readonly Lazy<byte[]> SpecExample = new(() => Encoding.UTF8.GetBytes(Resource("canonical-example.json")));

    /// <summary>
    /// The exception types l3lint documents for bad input: <see cref="InputException"/> (with its subclass
    /// <see cref="FormatDetectionException"/>), <see cref="YamlFormatException"/>, <see cref="ConfigurationException"/>
    /// and <see cref="RulePackException"/>.
    /// </summary>
    public static bool IsDocumented(Exception exception) =>
        exception is InputException or YamlFormatException or ConfigurationException or RulePackException;

    /// <summary>Runs the named target on one input.</summary>
    public static FuzzOutcome Run(string target, ReadOnlySpan<byte> input) => target switch
    {
        "json" => Json(input),
        "config" => Config(input),
        "csv" => Csv(input),
        "rulepack" => RulePack(input),
        _ => throw new ArgumentException("Unknown target " + target + ". Use one of: " + string.Join(", ", Names), nameof(target)),
    };

    /// <summary>
    /// JSON or NDJSON bytes: <c>RawDocument.ParseJson</c>, then map and lint, then the whole batch path the CLI uses
    /// for standard input, then all five report writers.
    /// </summary>
    public static FuzzOutcome Json(ReadOnlySpan<byte> input)
    {
        var bytes = input.ToArray();
        var outcome = Guard(() =>
        {
            var document = RawDocument.ParseJson(bytes, "fuzz.json");
            SharedLinter.Value.Map(document);
            SharedLinter.Value.Lint(document);
        });
        RunAndWrite(bytes, LintOptions.Default);
        return outcome;
    }

    /// <summary>YAML text through the <c>.l3lint.yaml</c> loader.</summary>
    public static FuzzOutcome Config(ReadOnlySpan<byte> input)
    {
        var text = Encoding.UTF8.GetString(input);
        return Guard(() => L3LintConfiguration.Parse(text, "fuzz.l3lint.yaml"));
    }

    /// <summary>CSV bytes through the CSV adapter with one fixed column map (fixtures/adapters/csv/01).</summary>
    public static FuzzOutcome Csv(ReadOnlySpan<byte> input)
    {
        var bytes = input.ToArray();
        var options = LintOptions.Default with
        {
            Format = "csv",
            Adapter = AdapterOptions.Default with { CsvMap = CsvMap.Value, CsvMapName = "csv-map.yaml" },
        };
        var outcome = Guard(() =>
        {
            var document = RawDocument.FromCsv(bytes, "fuzz.csv");
            SharedLinter.Value.Map(document, options);
            SharedLinter.Value.Lint(document, options);
        });
        RunAndWrite(bytes, options);
        return outcome;
    }

    /// <summary>
    /// A rule pack YAML body through the pack loader, with a manifest whose checksums match, so the fuzzer reaches
    /// the schema, compile and lint stages instead of stopping at the checksum. A pack that loads lints the spec
    /// example.
    /// </summary>
    public static FuzzOutcome RulePack(ReadOnlySpan<byte> input)
    {
        var body = input.ToArray();
        return Guard(() =>
        {
            var linter = new LinterBuilder()
                .AddRulePack(new InMemoryPackSource(body))
                .UseTimeProvider(Clock)
                .Build();
            linter.Lint(RawDocument.ParseJson(SpecExample.Value, "canonical-example.json"));
        });
    }

    private static FuzzOutcome Guard(Action action)
    {
        try
        {
            action();
            return FuzzOutcome.Accepted;
        }
        catch (Exception ex) when (IsDocumented(ex))
        {
            return FuzzOutcome.Rejected;
        }
    }

    // The runner reports bad input as input errors in the report, so nothing is caught here.
    private static void RunAndWrite(byte[] bytes, LintOptions options)
    {
        var request = new LintRunRequest
        {
            Inputs = ["-"],
            StandardInput = () => new MemoryStream(bytes, writable: false),
            Options = options,
            Parallelism = 1,
        };
        var report = new LintRunner(SharedLinter.Value).RunAsync(request).GetAwaiter().GetResult();
        foreach (var writer in ReportWriters.All)
        {
            using var output = new StringWriter();
            writer.Write(report, output, ReportWriterOptions.Default);
        }
    }

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("L3Lint.Fuzz." + name)
            ?? throw new InvalidOperationException("Missing embedded resource " + name);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>A clock fixed at the spec example's date, so a run never depends on today's date.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
