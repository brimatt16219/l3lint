using System.Text;
using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Markdown;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// NFR-001: identical input, options and pack versions give byte-identical output, whatever the thread count or the
/// order in which collections were filled.
/// </summary>
public sealed class DeterminismTests
{
    public static TheoryData<string> Formats => ["text", "json", "sarif", "junit", "markdown"];

    [Theory]
    [MemberData(nameof(Formats))]
    public void Rendering_the_same_report_twice_gives_the_same_bytes(string format)
    {
        var report = TestReports.Small();

        Bytes(format, report).ShouldBe(Bytes(format, report));
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Two_reports_built_from_the_same_results_render_the_same(string format)
    {
        Bytes(format, TestReports.Small()).ShouldBe(Bytes(format, TestReports.Small()));
    }

    [Fact]
    public void Collection_fill_order_does_not_change_sarif_junit_or_markdown()
    {
        var report = TestReports.Small();
        var reordered = report with
        {
            RulePacks = new Dictionary<string, string>(report.RulePacks.Reverse(), StringComparer.Ordinal),
            Rules = [.. report.Rules.Reverse()],
        };

        foreach (var format in new[] { "sarif", "junit", "markdown" })
        {
            Bytes(format, reordered).ShouldBe(Bytes(format, report), format);
        }
    }

    [Fact]
    public async Task One_thread_and_eight_threads_give_identical_output_in_every_format()
    {
        using var runs = new RealRuns();
        var single = await runs.RunAsync(parallelism: 1);
        var parallel = await runs.RunAsync(parallelism: 8);
        var options = ReportWriterOptions.Default with { BaseDirectory = runs.Folder };

        single.Transactions.Count.ShouldBe(3);
        single.Rules.Select(r => r.Id).ShouldBe(single.Summary.ByRule.Keys);
        foreach (var writer in ReportWriters.All)
        {
            Bytes(writer, parallel, options).ShouldBe(Bytes(writer, single, options), writer.Format);
        }
    }

    [Fact]
    public async Task Real_run_output_has_no_absolute_paths()
    {
        using var runs = new RealRuns();
        var report = await runs.RunAsync();
        var options = ReportWriterOptions.Default with { BaseDirectory = runs.Folder };
        var folderName = Path.GetFileName(runs.Folder);

        foreach (var text in new[] { SarifReportWriter.Render(report, options), JunitReportWriter.Render(report), MarkdownReportWriter.Render(report) })
        {
            text.ShouldNotContain(folderName);
            text.ShouldNotContain("file:");
        }
    }

    private static byte[] Bytes(string format, LintReport report)
    {
        ReportWriters.TryGet(format, out var writer).ShouldBeTrue();
        return Bytes(writer, report, ReportWriterOptions.Default);
    }

    private static byte[] Bytes(IReportWriter writer, LintReport report, ReportWriterOptions options)
    {
        using var buffer = new MemoryStream();
        using (var output = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { NewLine = "\n" })
        {
            writer.Write(report, output, options);
        }

        return buffer.ToArray();
    }
}
