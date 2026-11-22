using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Markdown;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>The built-in writer list: text, json, sarif, junit and markdown, in that order.</summary>
public sealed class ReportWritersTests
{
    [Fact]
    public void All_lists_the_five_formats_in_order()
    {
        ReportWriters.All.Select(w => w.Format).ShouldBe(["text", "json", "sarif", "junit", "markdown"]);
    }

    [Theory]
    [InlineData("sarif", typeof(SarifReportWriter))]
    [InlineData("SARIF", typeof(SarifReportWriter))]
    [InlineData("junit", typeof(JunitReportWriter))]
    [InlineData("markdown", typeof(MarkdownReportWriter))]
    public void TryGet_finds_each_new_writer_ignoring_case(string format, Type type)
    {
        ReportWriters.TryGet(format, out var writer).ShouldBeTrue();
        writer.ShouldBeOfType(type);
    }

    [Fact]
    public void TryGet_rejects_an_unknown_format()
    {
        ReportWriters.TryGet("html", out _).ShouldBeFalse();
    }
}
