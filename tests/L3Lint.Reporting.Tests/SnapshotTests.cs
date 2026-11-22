using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Markdown;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Whole-file checks of the fixed report (T-28, T-29) against the checked-in files under Expected/. Those files were
/// written by hand from the spec's mapping and <see cref="TestReports.Small"/>; fingerprints in the SARIF file were
/// computed separately as SHA-256 of the rule ID, source pointer and transaction key.
/// </summary>
public sealed class SnapshotTests
{
    [Fact]
    public void Sarif_matches_the_expected_file() =>
        ExpectedFiles.ShouldMatch(Render(new SarifReportWriter()), "small.sarif");

    [Fact]
    public void Junit_matches_the_expected_file() =>
        ExpectedFiles.ShouldMatch(Render(new JunitReportWriter()), "small.xml");

    [Fact]
    public void Markdown_matches_the_expected_file() =>
        ExpectedFiles.ShouldMatch(Render(new MarkdownReportWriter()), "small.md");

    [Fact]
    public void Expected_files_are_checked_in_with_lf_line_endings()
    {
        foreach (var name in new[] { "small.sarif", "small.xml", "small.md" })
        {
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Expected", name)).ShouldNotContain("\r", customMessage: name);
        }
    }

    private static string Render(IReportWriter writer)
    {
        using var output = new StringWriter { NewLine = "\n" };
        writer.Write(TestReports.Small(), output, ReportWriterOptions.Default);
        return output.ToString();
    }
}
