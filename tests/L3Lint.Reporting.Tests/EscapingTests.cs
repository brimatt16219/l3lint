using System.Text.Json;
using System.Xml.Linq;
using L3Lint.Core;
using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Markdown;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Payload text is data: pipes, angle brackets, Markdown syntax, control characters, unpaired surrogates and very
/// long messages must never break a format or change its structure.
/// </summary>
public sealed class EscapingTests
{
    private const string Hostile = "a|b <script>alert(1)</script> *bold* _it_ `code` [link](http://x) # $x$ ~s~ & \\ \"q\"";
    private const string Controls = "bell\u0007 nul\u0000 esc\u001b lone\ud800 pair😀 end";

    [Fact]
    public void Markdown_cells_escape_markdown_and_html_syntax()
    {
        var page = MarkdownReportWriter.Render(Report(Hostile));
        var row = page.Split('\n').Single(l => l.StartsWith("| warning |", StringComparison.Ordinal));

        row.ShouldContain("a\\|b \\<script\\>alert(1)\\</script\\> \\*bold\\* \\_it\\_ \\`code\\` \\[link\\](http://x) \\# \\$x\\$ \\~s\\~ \\& \\\\ \"q\"");
        row.Split(" | ").Length.ShouldBe(5);
    }

    [Fact]
    public void Markdown_summary_encodes_html_in_transaction_ids()
    {
        var report = Report("Message.");
        report = report with { Transactions = [report.Transactions[0] with { Id = "<img src=x onerror=alert(1)>&\"'" }] };

        var page = MarkdownReportWriter.Render(report);

        page.ShouldContain("<summary><strong>&lt;img src=x onerror=alert(1)&gt;&amp;&quot;&#39;</strong>: 1 warning</summary>");
    }

    [Fact]
    public void Markdown_line_breaks_in_cells_become_br_tags()
    {
        var page = MarkdownReportWriter.Render(Report("first\r\nsecond\nthird\rfourth"));

        page.ShouldContain("| first<br>second<br>third<br>fourth |");
    }

    [Fact]
    public void Markdown_replaces_control_characters()
    {
        var page = MarkdownReportWriter.Render(Report(Controls));

        page.ShouldContain("bell� nul� esc�");
        page.ShouldNotContain("\u0007");
        page.ShouldNotContain("\u001b");
        page.ShouldNotContain("\0");
    }

    [Fact]
    public void Markdown_code_span_survives_backticks_and_pipes_in_paths()
    {
        MarkdownText.TableCode("/a`b|c").ShouldBe("``/a`b\\|c``");
        MarkdownText.TableCode("`x`").ShouldBe("`` `x` ``");
        MarkdownText.TableCode("/a\nb").ShouldBe("`/a b`");
    }

    [Fact]
    public void Markdown_clip_never_splits_a_surrogate_pair()
    {
        var text = new string('a', 8) + "😀" + "tail";

        var clipped = MarkdownText.Clip(text, 10);

        clipped.ShouldBe(new string('a', 8) + "…");
        MarkdownText.Clip("short", 10).ShouldBe("short");
        MarkdownText.Clip("exactly-10", 10).ShouldBe("exactly-10");
        MarkdownText.Clip("eleven-char", 10).ShouldBe("eleven-ch…");
    }

    [Fact]
    public void Junit_escapes_markup_and_keeps_the_text()
    {
        var xml = JunitReportWriter.Render(Report(Hostile, Severity.Error));
        var failure = XDocument.Parse(xml).Descendants("failure").Single();

        xml.ShouldContain("&lt;script&gt;");
        failure.Attribute("message")!.Value.ShouldBe("GEN-DQ-004: " + Hostile);
        Schemas.ValidateJunit(xml).ShouldBeEmpty();
    }

    [Fact]
    public void Junit_replaces_characters_xml_cannot_carry()
    {
        var xml = JunitReportWriter.Render(Report(Controls, Severity.Error));
        var failure = XDocument.Parse(xml).Descendants("failure").Single();

        failure.Attribute("message")!.Value.ShouldBe("GEN-DQ-004: bell� nul� esc� lone� pair😀 end");
        Schemas.ValidateJunit(xml).ShouldBeEmpty();
    }

    [Fact]
    public void Junit_normalizes_carriage_returns_to_lf()
    {
        JunitReportWriter.Clean("a\r\nb\rc\nd").ShouldBe("a\nb\nc\nd");
    }

    [Fact]
    public void Sarif_keeps_hostile_text_as_json_strings()
    {
        var sarif = SarifReportWriter.Render(Report(Hostile), ReportWriterOptions.Default);
        var result = JsonDocument.Parse(sarif).RootElement.GetProperty("runs")[0].GetProperty("results")[0];

        result.GetProperty("message").GetProperty("text").GetString().ShouldBe(Hostile + " Fix: Use the real product or service name.");
        Schemas.ValidateSarif(sarif).ShouldBeEmpty();
    }

    [Fact]
    public void Sarif_escapes_control_characters_and_replaces_unpaired_surrogates()
    {
        var sarif = SarifReportWriter.Render(Report(Controls), ReportWriterOptions.Default);
        var text = JsonDocument.Parse(sarif).RootElement.GetProperty("runs")[0].GetProperty("results")[0].GetProperty("message").GetProperty("text").GetString();

        sarif.ShouldContain("bell\\u0007 nul\\u0000 esc\\u001B");
        text.ShouldStartWith("bell\u0007 nul\u0000 esc\u001b lone� pair😀 end");
        Schemas.ValidateSarif(sarif).ShouldBeEmpty();
    }

    [Fact]
    public void Very_long_messages_stay_whole_in_sarif_and_junit()
    {
        var message = string.Concat(Enumerable.Repeat("long message | <x> ", 10_000));
        var report = Report(message, Severity.Error);

        var sarif = JsonDocument.Parse(SarifReportWriter.Render(report, ReportWriterOptions.Default)).RootElement;
        var junit = XDocument.Parse(JunitReportWriter.Render(report));

        sarif.GetProperty("runs")[0].GetProperty("results")[0].GetProperty("message").GetProperty("text").GetString().ShouldStartWith(message);
        junit.Descendants("failure").Single().Attribute("message")!.Value.ShouldBe("GEN-DQ-004: " + message);
    }

    [Fact]
    public void Very_long_rule_titles_are_cut_to_github_limits_in_sarif()
    {
        var rule = TestReports.GenDq004 with { Title = new string('t', 300), Requirement = new string('r', 1100) };
        var report = Report("Message.") with { Rules = [rule] };

        var descriptor = JsonDocument.Parse(SarifReportWriter.Render(report, ReportWriterOptions.Default)).RootElement
            .GetProperty("runs")[0].GetProperty("tool").GetProperty("driver").GetProperty("rules")[0];

        descriptor.GetProperty("name").GetString().ShouldBe(new string('t', 254) + "…");
        descriptor.GetProperty("shortDescription").GetProperty("text").GetString().ShouldBe(new string('t', 300));
        descriptor.GetProperty("fullDescription").GetProperty("text").GetString().ShouldBe(new string('r', 1023) + "…");
    }

    private static LintReport Report(string message, Severity severity = Severity.Warning) =>
        TestReports.Many([TestReports.Finding("GEN-DQ-004", severity, message, actual: message)]);
}
