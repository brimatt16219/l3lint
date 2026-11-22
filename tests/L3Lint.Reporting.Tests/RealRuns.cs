using L3Lint.Core;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Real reports from the shipped linter: the spec's canonical example and variants of it, written to a temporary
/// folder and linted by <see cref="LintRunner"/> with a fixed clock (2026-10-19, the date of the spec's example).
/// </summary>
internal sealed class RealRuns : IDisposable
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public RealRuns()
    {
        Folder = Path.Combine(Path.GetTempPath(), "l3lint-reporting-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Folder, "invoices"));
        var canonical = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));
        File.WriteAllText(Path.Combine(Folder, "invoices", "a-clean.json"), canonical);

        // Line 2 loses its commodity code (VISA-L3-004) and gains a field the canonical schema does not know (GEN-FMT-001).
        var broken = canonical
            .Replace("\"commodityCode\": \"42131606\",", string.Empty, StringComparison.Ordinal)
            .Replace("\"quantity\": 2,", "\"quantity\": 2, \"description2\": \"x\",", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(Folder, "invoices", "b-broken.json"), broken);

        // The customer reference becomes filler (GEN-DQ-004) and the ID changes.
        var filler = canonical
            .Replace("\"reference\": \"PO-77812\"", "\"reference\": \"111111\"", StringComparison.Ordinal)
            .Replace("\"id\": \"INV-20417\"", "\"id\": \"INV-20418\"", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(Folder, "invoices", "c filler.json"), filler);
    }

    public static ILinter Linter { get; } = L3LintFactory.CreateBuilder().UseTimeProvider(Clock).Build();

    public string Folder { get; }

    public Task<LintReport> RunAsync(int parallelism = 1) =>
        new LintRunner(Linter).RunAsync(
            new LintRunRequest
            {
                Inputs = ["invoices"],
                WorkingDirectory = Folder,
                Parallelism = parallelism,
                ConfigHash = TestReports.ConfigHash,
            },
            TestContext.Current.CancellationToken);

    public void Dispose() => Directory.Delete(Folder, recursive: true);
}
