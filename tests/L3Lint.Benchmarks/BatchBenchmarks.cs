using BenchmarkDotNet.Attributes;
using L3Lint.Core;
using L3Lint.Hosting.Runs;

namespace L3Lint.Benchmarks;

/// <summary>
/// Batch throughput of the CLI's runner over an NDJSON file of 10,000 records (US-04; NFR-003: at least 2,000
/// payloads per second on 4 cores). One operation lints the whole file, so payloads per second is 10,000 divided by
/// the mean.
/// </summary>
[MemoryDiagnoser]
public class BatchBenchmarks
{
    public const int Records = 10_000;

    private ILinter _linter = null!;
    private string _folder = null!;
    private string _file = null!;

    /// <summary>Worker count: 4 for the spec's "4 cores", and this machine's logical processor count (the CLI default).</summary>
    [Params(4, 8)]
    public int Workers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _linter = BenchmarkPayloads.CreateLinter();
        _folder = Path.Combine(Path.GetTempPath(), "l3lint-benchmarks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _file = BenchmarkPayloads.WriteNdjson(Path.Combine(_folder, "batch.ndjson"), Records);
    }

    [GlobalCleanup]
    public void Cleanup() => Directory.Delete(_folder, recursive: true);

    [Benchmark]
    public async Task<int> Lint_10000_ndjson_records()
    {
        var report = await new LintRunner(_linter).RunAsync(new LintRunRequest
        {
            Inputs = [_file],
            WorkingDirectory = _folder,
            Parallelism = Workers,
        });
        return report.Transactions.Count;
    }
}
