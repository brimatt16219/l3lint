using System.Text;
using BenchmarkDotNet.Attributes;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;
using L3Lint.Reporting;
using L3Lint.Reporting.Json;

namespace L3Lint.Benchmarks;

/// <summary>
/// One payload in a warm process (NFR-002: p95 under 50 ms for up to 100 lines; US-09: a 10-line payload in under
/// 1 ms; US-08: lint_payload within 200 ms for 100 lines).
/// </summary>
[MemoryDiagnoser]
public class SinglePayloadBenchmarks
{
    private ILinter _linter = null!;
    private byte[] _spec = null!;
    private byte[] _ten = null!;
    private byte[] _hundred = null!;
    private byte[] _stripe = null!;
    private byte[] _withCard = null!;
    private Transaction _tenTransaction = null!;

    [GlobalSetup]
    public void Setup()
    {
        _linter = BenchmarkPayloads.CreateLinter();
        _spec = Encoding.UTF8.GetBytes(BenchmarkPayloads.SpecExample);
        _ten = Encoding.UTF8.GetBytes(BenchmarkPayloads.WithLines(10));
        _hundred = Encoding.UTF8.GetBytes(BenchmarkPayloads.WithLines(100));
        _stripe = Encoding.UTF8.GetBytes(BenchmarkPayloads.Stripe);
        _withCard = Encoding.UTF8.GetBytes(BenchmarkPayloads.SpecExample.Replace("Nitrile exam gloves L 100", "4111 1111 1111 1111", StringComparison.Ordinal));
        _tenTransaction = _linter.Map(RawDocument.ParseJson(_ten, "ten.json")).Transactions[0].Transaction;
    }

    /// <summary>The spec example (2 lines) from JSON bytes: parse, scrub, detect, map, validate and lint.</summary>
    [Benchmark(Baseline = true)]
    public LintResult Spec_example_2_lines() => _linter.Lint(RawDocument.ParseJson(_spec, "spec.json"));

    /// <summary>US-09: the library API on an in-memory 10-line transaction.</summary>
    [Benchmark]
    public LintResult Library_10_lines_transaction() => _linter.Lint(_tenTransaction);

    /// <summary>A 10-line payload from JSON bytes.</summary>
    [Benchmark]
    public LintResult Lint_10_lines() => _linter.Lint(RawDocument.ParseJson(_ten, "ten.json"));

    /// <summary>NFR-002 and US-08: a 100-line payload from JSON bytes.</summary>
    [Benchmark]
    public LintResult Lint_100_lines() => _linter.Lint(RawDocument.ParseJson(_hundred, "hundred.json"));

    /// <summary>US-08 engine work: 100 lines linted and rendered as the JSON results document the MCP tool returns.</summary>
    [Benchmark]
    public int Lint_100_lines_and_render_json()
    {
        var result = _linter.Lint(RawDocument.ParseJson(_hundred, "hundred.json"));
        var report = new LintReport
        {
            ToolVersion = EngineInfo.Version,
            RulePacks = new SortedDictionary<string, string>(result.RulePackVersions.ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal),
            Profile = "generic",
            Inputs = [new ReportInput("payload", result.Format ?? "unknown", result.InputSha256, result.Transactions.Count)],
            Transactions = result.Transactions,
            InputErrors = result.InputErrors,
            Summary = LintSummary.From(result.Findings, result.Transactions.Count, Severity.Error),
            FailOn = Severity.Error,
            ExitCode = 0,
        };
        return JsonReportWriter.Render(report).Length;
    }

    /// <summary>A Stripe body through format detection and mapping (no rules).</summary>
    [Benchmark]
    public MapResult Stripe_detect_and_map() => _linter.Map(RawDocument.ParseJson(_stripe, "stripe.json"));

    /// <summary>Card-number scrubbing of the spec example with one public test number in a description.</summary>
    [Benchmark]
    public int Scrub_one_test_card()
    {
        var document = RawDocument.ParseJson(_withCard, "card.json");
        return CardNumberScrubber.ScrubJson(document, _linter.Adapters.Resolve(null, document), []).Matches.Count;
    }
}
