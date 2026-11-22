using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using BenchmarkDotNet.Running;
using L3Lint.Benchmarks;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;

// Benchmarks: dotnet run -c Release --project tests/L3Lint.Benchmarks -- --job short --filter "*"
// Memory probe for NFR-003 (bounded memory on a large NDJSON file):
//   dotnet run -c Release --project tests/L3Lint.Benchmarks -- memory <megabytes>
// See docs/dev/benchmarks.md for the recorded results.
if (args.Length == 2 && args[0] == "memory")
{
    return await MemoryProbe.RunAsync(int.Parse(args[1], CultureInfo.InvariantCulture));
}

BenchmarkSwitcher.FromAssembly(typeof(SinglePayloadBenchmarks).Assembly).Run(args);
return 0;

/// <summary>
/// Streams a generated NDJSON file of the given size through the CLI's runner and the JSON writer, then prints
/// throughput and peak memory (NFR-003: under 500 MB for a 1 GB NDJSON file).
/// </summary>
internal static class MemoryProbe
{
    public static async Task<int> RunAsync(int megabytes)
    {
        var folder = Path.Combine(Path.GetTempPath(), "l3lint-memory-probe", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var recordBytes = BenchmarkPayloads.WithLines(2).Length + 1;
            var records = (int)(megabytes * 1024L * 1024L / recordBytes);
            var file = BenchmarkPayloads.WriteNdjson(Path.Combine(folder, "batch.ndjson"), records);
            var size = new FileInfo(file).Length;
            var linter = BenchmarkPayloads.CreateLinter();
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            var before = process.PeakWorkingSet64;

            var watch = Stopwatch.StartNew();
            var report = await new LintRunner(linter).RunAsync(new LintRunRequest { Inputs = [file], WorkingDirectory = folder });
            var lintSeconds = watch.Elapsed.TotalSeconds;
            await using (var sink = new StreamWriter(Stream.Null))
            {
                new L3Lint.Reporting.Json.JsonReportWriter().Write(report, sink, ReportWriterOptions.Default);
            }

            process.Refresh();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"file: {size / 1048576.0:F1} MB, {records} records, {report.Transactions.Count} linted, {report.Summary.Errors + report.Summary.Warnings + report.Summary.Information} findings"));
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"lint: {lintSeconds:F1} s, {records / lintSeconds:F0} payloads per second, {Environment.ProcessorCount} workers"));
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"peak working set: {process.PeakWorkingSet64 / 1048576.0:F0} MB (before the run: {before / 1048576.0:F0} MB)"));
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"managed heap after the run: {GC.GetTotalMemory(forceFullCollection: false) / 1048576.0:F0} MB"));
            var info = GC.GetGCMemoryInfo(GCKind.Any);
            var generations = info.GenerationInfo.ToArray();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"GC: {(GCSettings.IsServerGC ? "server" : "workstation")}, {GCSettings.LatencyMode}; collections gen0 {GC.CollectionCount(0)}, gen1 {GC.CollectionCount(1)}, gen2 {GC.CollectionCount(2)}; after the last GC: gen0 {generations[0].SizeAfterBytes / 1048576.0:F0} MB, gen1 {generations[1].SizeAfterBytes / 1048576.0:F0} MB, gen2 {generations[2].SizeAfterBytes / 1048576.0:F0} MB, large objects {generations[3].SizeAfterBytes / 1048576.0:F0} MB"));

            // What the finished report keeps alive, per transaction, once garbage is collected.
            var live = GC.GetTotalMemory(forceFullCollection: true);
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"live heap holding the report: {live / 1048576.0:F0} MB, {live / Math.Max(1, report.Transactions.Count)} bytes per transaction"));
            GC.KeepAlive(report);
            return 0;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
