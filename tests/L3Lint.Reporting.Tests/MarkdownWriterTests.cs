using System.Text;
using L3Lint.Core;
using L3Lint.Reporting.Markdown;

namespace L3Lint.Reporting.Tests;

/// <summary>The Markdown job summary (Interfaces, "Output formats", FR-024) and ADR-0052.</summary>
public sealed class MarkdownWriterTests
{
    [Fact]
    public void Summary_table_counts_transactions_and_findings_by_brand()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small());

        page.ShouldContain("| mastercard | 1 | 0 | 0 | 1 |\n| visa | 2 | 1 | 1 | 0 |\n| Batch findings |  | 0 | 1 | 0 |\n| **Total** | 3 | 1 | 2 | 1 |\n");
    }

    [Fact]
    public void One_collapsible_section_per_transaction_with_findings()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small());

        Occurrences(page, "<details>").ShouldBe(2);
        Occurrences(page, "</details>").ShouldBe(2);
        page.ShouldContain("<summary><strong>INV-20417</strong>: 1 error, 1 warning</summary>");
        page.ShouldNotContain("<strong>INV-3</strong>");
        page.ShouldContain("1 transaction has no findings.");
    }

    [Fact]
    public void Rule_ids_link_to_the_rule_reference_page()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small());

        page.ShouldContain("[VISA-L3-004](" + EngineInfo.RuleHelpUri("VISA-L3-004").AbsoluteUri + " \"Commodity code is required for Product 3\")");
        page.ShouldContain("[GEN-DQ-001](" + EngineInfo.RuleHelpUri("GEN-DQ-001").AbsoluteUri + ")");
    }

    [Fact]
    public void Errors_come_before_warnings_in_a_transaction_table()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small());

        page.IndexOf("| error | [VISA-L3-004]", StringComparison.Ordinal).ShouldBeLessThan(page.IndexOf("| warning | [GEN-DQ-004]", StringComparison.Ordinal));
    }

    [Fact]
    public void Likely_tier_and_cost_estimate_appear_in_the_section()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small());

        page.ShouldContain("**Likely tier:** below Product 3 (blocked by 1 error)");
        page.ShouldContain("Estimated, using example-rates (effective 2026-10-01):");
        page.ShouldContain("| visa | level2 | 6.10 USD | level3 | 5.12 USD | 0.98 USD |");
    }

    [Fact]
    public void Several_tiers_name_their_brand()
    {
        var report = TestReports.Small();
        var transaction = report.Transactions[0] with
        {
            Tiers =
            [
                new Core.Tiers.TierAssessment(Core.Model.CardBrand.Visa, "product3", "belowProduct3", Core.Tiers.TierStatus.Blocked, 1),
                new Core.Tiers.TierAssessment(Core.Model.CardBrand.Mastercard, "dataRate3", "dataRate2", Core.Tiers.TierStatus.AtRisk, 0),
            ],
        };

        var page = MarkdownReportWriter.Render(report with { Transactions = [transaction, .. report.Transactions.Skip(1)] });

        page.ShouldContain("**Likely tier (visa):** below Product 3 (blocked by 1 error)");
        page.ShouldContain("**Likely tier (mastercard):** Data Rate II (at risk)");
    }

    [Fact]
    public void Findings_left_out_by_max_findings_are_mentioned()
    {
        var page = MarkdownReportWriter.Render(TestReports.Small() with { FindingsOmitted = 7 });

        page.ShouldContain("7 more findings are not shown; the summary counts them.");
    }

    [Fact]
    public void A_clean_report_has_no_sections()
    {
        var report = TestReports.Many([]) with { Summary = LintSummary.From([], 0, Severity.Error) };

        var page = MarkdownReportWriter.Render(report);

        page.ShouldNotContain("<details>");
        page.ShouldStartWith("## l3lint results\n\n");
        page.ShouldEndWith("|\n");
    }

    [Fact]
    public void A_page_over_the_limit_stops_with_a_note_that_counts_the_hidden_findings()
    {
        var findings = Enumerable.Range(0, 3000).Select(i => TestReports.Finding("GEN-DQ-004", Severity.Warning, "Finding " + i + " " + new string('m', 600))).ToList();
        var report = TestReports.Many(findings);

        var page = MarkdownReportWriter.Render(report);
        var bytes = Encoding.UTF8.GetByteCount(page);
        var shown = Occurrences(page, "| warning | [GEN-DQ-004]");

        bytes.ShouldBeLessThanOrEqualTo(MarkdownReportWriter.MaxBytes);
        bytes.ShouldBeGreaterThan(MarkdownReportWriter.MaxBytes - 10_000);
        shown.ShouldBeLessThan(3000);
        page.ShouldContain("> This summary stops here to stay under GitHub's 1 MiB job summary limit. " + (3000 - shown) + " more findings are not shown; the JSON output lists every finding.");
        Occurrences(page, "<details>").ShouldBe(Occurrences(page, "</details>"));
    }

    [Fact]
    public void A_cut_inside_a_table_still_closes_the_section()
    {
        var findings = Enumerable.Range(0, 40).Select(i => TestReports.Finding("GEN-DQ-004", Severity.Warning, "Finding " + i + " " + new string('m', 300))).ToList();
        var report = TestReports.Many(findings);
        var oneTransaction = report with { Transactions = [report.Transactions[0] with { Findings = [.. findings.Select(f => f with { TransactionIndex = 0 })] }] };

        var page = MarkdownReportWriter.Render(oneTransaction, maxBytes: 12_000);
        var shown = Occurrences(page, "| warning | [GEN-DQ-004]");

        Encoding.UTF8.GetByteCount(page).ShouldBeLessThanOrEqualTo(12_000);
        shown.ShouldBeGreaterThan(0);
        shown.ShouldBeLessThan(40);
        page.ShouldContain("|\n\n</details>\n\n> This summary stops here");
        page.ShouldContain(" " + (40 - shown) + " more findings are not shown");
    }

    [Fact]
    public void Long_cells_are_cut_with_an_ellipsis()
    {
        var message = new string('m', MarkdownReportWriter.MaxCellLength + 500);

        var page = MarkdownReportWriter.Render(TestReports.Many([TestReports.Finding("GEN-DQ-004", Severity.Warning, message)]));

        page.ShouldContain("| " + new string('m', MarkdownReportWriter.MaxCellLength - 1) + "… |");
        page.ShouldNotContain(new string('m', MarkdownReportWriter.MaxCellLength));
    }

    [Fact]
    public void Writer_output_ends_with_one_newline_and_has_no_carriage_returns()
    {
        using var output = new StringWriter();
        new MarkdownReportWriter().Write(TestReports.Small(), output, ReportWriterOptions.Default);
        var text = output.ToString();

        text.ShouldEndWith("|\n");
        text.ShouldNotEndWith("\n\n");
        text.ShouldNotContain("\r");
    }

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
