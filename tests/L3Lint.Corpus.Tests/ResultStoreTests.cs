using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;

namespace L3Lint.Corpus.Tests;

/// <summary>
/// A batch keeps its results compressed in blocks (NFR-003, ADR-0059). Every report writer must give the same bytes
/// whether the results stayed objects or went through the compressed store, so the whole corpus runs both ways.
/// </summary>
public sealed class ResultStoreTests
{
    private static readonly ILinter Linter = L3LintFactory.CreateBuilder().Build();

    private static readonly LintRunRequest Request = new()
    {
        Inputs = ["fixtures/seeded/**/*.json", "fixtures/known-good/**/*.json", "fixtures/pci/**/*.json", "fixtures/adapters/**/*.json"],
        WorkingDirectory = Fixture.RepositoryRoot,
        Parallelism = 4,
    };

    [Fact]
    public async Task Every_writer_gives_the_same_report_from_compressed_results()
    {
        var objects = await new LintRunner(Linter).RunAsync(Request with { ResultBlockSize = int.MaxValue }, TestContext.Current.CancellationToken);
        var compressed = await new LintRunner(Linter).RunAsync(Request with { ResultBlockSize = 7 }, TestContext.Current.CancellationToken);

        // 400 seeded, 200 known-good and 10 PCI payloads plus the gateway fixtures: many blocks of 7.
        compressed.Transactions.Count.ShouldBeGreaterThan(610);
        compressed.Transactions.Count.ShouldBe(objects.Transactions.Count);
        foreach (var writer in ReportWriters.All)
        {
            Render(writer, compressed).ShouldBe(Render(writer, objects), writer.Format);
        }
    }

    [Fact]
    public async Task Max_findings_truncates_the_same_way_from_compressed_results()
    {
        var objects = await new LintRunner(Linter).RunAsync(Request with { ResultBlockSize = int.MaxValue, MaxFindings = 100 }, TestContext.Current.CancellationToken);
        var compressed = await new LintRunner(Linter).RunAsync(Request with { ResultBlockSize = 5, MaxFindings = 100 }, TestContext.Current.CancellationToken);

        compressed.FindingsOmitted.ShouldBeGreaterThan(0);
        compressed.FindingsOmitted.ShouldBe(objects.FindingsOmitted);
        compressed.AllFindings().Count().ShouldBe(100);
        Render(ReportWriters.All.Single(w => w.Format == "json"), compressed).ShouldBe(Render(ReportWriters.All.Single(w => w.Format == "json"), objects));
    }

    [Fact]
    public void The_store_reads_by_index_across_blocks_and_from_the_open_block()
    {
        var store = new TransactionResultStore(blockSize: 3);
        var results = Enumerable.Range(0, 8).Select(Result).ToList();
        foreach (var result in results)
        {
            store.Add(result);
        }

        store.Count.ShouldBe(8);
        Enumerable.Range(0, 8).Select(i => Json(store[i])).ShouldBe(results.Select(Json));
        store.Select(Json).ShouldBe(results.Select(Json));
        Should.Throw<ArgumentOutOfRangeException>(() => store[8]);
    }

    private static TransactionResult Result(int index) => new()
    {
        Index = index,
        Id = "INV-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Brand = CardBrand.Visa,
        AsOf = new DateOnly(2026, 10, 19),
        Notes = ["note " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)],
    };

    private static string Json(TransactionResult result) => JsonSerializer.Serialize(result, TransactionResultStoreJson.Default.TransactionResult);

    private static string Render(IReportWriter writer, LintReport report)
    {
        using var output = new StringWriter { NewLine = "\n" };
        writer.Write(report, output, ReportWriterOptions.Default with { BaseDirectory = Fixture.RepositoryRoot });
        return output.ToString();
    }
}
