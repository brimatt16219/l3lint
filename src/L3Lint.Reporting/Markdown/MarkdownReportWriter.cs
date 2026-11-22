using System.Globalization;
using System.Text;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Reporting.Text;

namespace L3Lint.Reporting.Markdown;

/// <summary>
/// GitHub-flavored Markdown for people and for the GitHub Action's job summary (FR-024): the summary line, a table
/// of transactions, errors, warnings and information by brand, then one collapsible section per transaction with
/// findings, holding a findings table, the likely tier and the cost estimate. Payload text is escaped so it never
/// becomes Markdown or HTML, and the page stays under GitHub's 1 MiB job summary limit (ADR-0052).
/// </summary>
public sealed class MarkdownReportWriter : IReportWriter
{
    /// <summary>The largest page in UTF-8 bytes; GitHub accepts at most 1 MiB per step summary.</summary>
    internal const int MaxBytes = 1_000_000;

    /// <summary>The longest text in one table cell, in characters, before it is cut with an ellipsis.</summary>
    internal const int MaxCellLength = 2_000;

    /// <summary>Bytes kept back for closing tags and the truncation note.</summary>
    private const int Reserve = 4_096;

    private const string FindingsHeader = "| {0} | {1} | {2} | {3} | {4} |\n| --- | --- | --- | --- | --- |\n";

    /// <inheritdoc/>
    public string Format => "markdown";

    /// <inheritdoc/>
    public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        output.Write(Render(report));
    }

    /// <summary>Renders the page.</summary>
    /// <param name="report">The report.</param>
    /// <param name="maxBytes">The page size limit in UTF-8 bytes.</param>
    /// <returns>Markdown with LF line endings, ending with one newline.</returns>
    internal static string Render(LintReport report, int maxBytes = MaxBytes)
    {
        var index = new ReportIndex(report);
        var page = new Page(maxBytes - Reserve);
        page.Force("## " + MarkdownText.Escape(Resources.Strings.MdHeading) + "\n\n");
        foreach (var note in report.Notes)
        {
            page.TryAdd("> " + MarkdownText.Escape(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextNote, MarkdownText.Clip(note, MaxCellLength))) + "\n\n");
        }

        // The summary line lists packs in dictionary order; sorting here keeps the page independent of how the map was filled.
        var sortedPacks = report with { RulePacks = new SortedDictionary<string, string>(report.RulePacks.ToDictionary(), StringComparer.Ordinal) };
        page.Force(MarkdownText.Escape(TextReportWriter.SummaryLine(sortedPacks)) + "\n\n");
        page.Force(SummaryTable(report));
        var clean = report.Transactions.Count(t => t.Findings.Count == 0);
        if (clean > 0)
        {
            page.Force((clean == 1 ? Resources.Strings.MdCleanOne : string.Format(CultureInfo.InvariantCulture, Resources.Strings.MdCleanMany, clean)) + "\n\n");
        }

        WriteInputErrors(page, report);
        foreach (var group in index.Groups)
        {
            // The heading goes in before the group's first failing transaction; a group without one gets none.
            var headed = false;
            foreach (var transaction in group.Transactions.Where(t => t.Findings.Count > 0))
            {
                if (!headed)
                {
                    if (!page.TryAdd("### " + MarkdownText.Escape(MarkdownText.Clip(group.Name, MaxCellLength)) + "\n\n"))
                    {
                        break;
                    }

                    headed = true;
                }

                WriteTransaction(page, transaction, index);
            }
        }

        WriteBatch(page, index);
        if (report.FindingsOmitted > 0)
        {
            page.TryAdd(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextOmitted, report.FindingsOmitted) + "\n\n");
        }

        if (page.Truncated)
        {
            var hidden = report.AllFindings().Count() - page.Rows;
            page.Force("> " + string.Format(CultureInfo.InvariantCulture, Resources.Strings.MdTruncated, hidden) + "\n\n");
        }

        return page.ToString().TrimEnd('\n') + "\n";
    }

    private static string SummaryTable(LintReport report)
    {
        var brands = JsonBrandCounts(report);
        var transactions = report.Transactions.CountBy(t => CanonicalNames.ToName(t.Brand), StringComparer.Ordinal).ToDictionary(StringComparer.Ordinal);
        var names = brands.Keys.Union(transactions.Keys).Order(StringComparer.Ordinal);
        var table = new StringBuilder();
        table.Append("| ").Append(Resources.Strings.MdBrand)
            .Append(" | ").Append(Resources.Strings.MdTransactions)
            .Append(" | ").Append(Resources.Strings.MdErrors)
            .Append(" | ").Append(Resources.Strings.MdWarnings)
            .Append(" | ").Append(Resources.Strings.MdInformation)
            .Append(" |\n| --- | ---: | ---: | ---: | ---: |\n");
        foreach (var name in names)
        {
            var (errors, warnings, information) = brands.GetValueOrDefault(name);
            Row(table, MarkdownText.Escape(name), Number(transactions.GetValueOrDefault(name)), Number(errors), Number(warnings), Number(information));
        }

        if (report.BatchFindings.Count > 0)
        {
            Row(
                table,
                MarkdownText.Escape(Resources.Strings.TextBatchHeader),
                string.Empty,
                Number(report.BatchFindings.Count(f => f.Severity == Severity.Error)),
                Number(report.BatchFindings.Count(f => f.Severity == Severity.Warning)),
                Number(report.BatchFindings.Count(f => f.Severity == Severity.Information)));
        }

        var summary = report.Summary;
        Row(table, "**" + Resources.Strings.MdTotal + "**", Number(summary.Transactions), Number(summary.Errors), Number(summary.Warnings), Number(summary.Information));
        return table.Append('\n').ToString();
    }

    private static SortedDictionary<string, (int Errors, int Warnings, int Information)> JsonBrandCounts(LintReport report) =>
        Json.JsonReportWriter.ByBrand(report);

    private static void WriteInputErrors(Page page, LintReport report)
    {
        if (report.InputErrors.Count == 0 || !page.TryAdd("### " + MarkdownText.Escape(Resources.Strings.MdInputErrors) + "\n\n"))
        {
            return;
        }

        if (!page.TryAdd("| " + Resources.Strings.MdInput + " | " + Resources.Strings.MdLine + " | " + Resources.Strings.LabelMessage + " |\n| --- | ---: | --- |\n"))
        {
            return;
        }

        foreach (var error in report.InputErrors)
        {
            var message = error.Candidates is { Count: > 0 } candidates
                ? error.Message + " " + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextCandidates, string.Join(", ", candidates))
                : error.Message;
            var row = new StringBuilder();
            Row(row, Cell(error.SourceName), error.Line is { } line ? Number(line) : string.Empty, Cell(message));
            if (!page.TryAdd(row.ToString()))
            {
                return;
            }
        }

        page.TryAdd("\n");
    }

    private static void WriteTransaction(Page page, TransactionResult transaction, ReportIndex index)
    {
        var counts = Counts(transaction.Findings);
        var summary = string.Format(CultureInfo.InvariantCulture, Resources.Strings.MdTransactionSummary, "<strong>" + MarkdownText.Html(MarkdownText.Clip(ReportIndex.TransactionName(transaction), MaxCellLength)) + "</strong>", MarkdownText.Html(counts));
        var header = string.Format(CultureInfo.InvariantCulture, FindingsHeader, Resources.Strings.LabelSeverity, Resources.Strings.MdRule, Resources.Strings.MdPath, Resources.Strings.LabelMessage, Resources.Strings.LabelFix);
        if (!page.TryAdd("<details>\n<summary>" + summary + "</summary>\n\n" + header))
        {
            return;
        }

        foreach (var finding in transaction.Findings.OrderByDescending(f => f.Severity))
        {
            var row = new StringBuilder();
            Row(row, WireNames.Of(finding.Severity), RuleLink(finding.RuleId, index), PathCell(finding), Cell(finding.Message), Cell(finding.Fix));
            if (!page.TryAddRow(row.ToString()))
            {
                break;
            }
        }

        if (!page.Truncated)
        {
            WriteTiers(page, transaction);
            WriteEstimate(page, transaction);
        }

        page.Force("\n</details>\n\n");
    }

    private static void WriteTiers(Page page, TransactionResult transaction)
    {
        var lines = new StringBuilder("\n");
        foreach (var tier in transaction.Tiers)
        {
            var label = transaction.Tiers.Count > 1
                ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextTierForBrand, CanonicalNames.ToName(tier.Brand))
                : Resources.Strings.TextTier;
            lines.Append("**").Append(MarkdownText.Escape(label)).Append("** ").Append(MarkdownText.Escape(TierNames.Describe(tier))).Append("\n\n");
        }

        page.TryAdd(lines.ToString().TrimEnd('\n') + "\n");
    }

    private static void WriteEstimate(Page page, TransactionResult transaction)
    {
        if (transaction.CostEstimate is not { } estimate)
        {
            return;
        }

        var table = new StringBuilder("\n");
        table.Append(MarkdownText.Escape(string.Format(
            CultureInfo.InvariantCulture,
            Resources.Strings.TextEstimateHeader,
            estimate.RateTableName,
            estimate.Effective.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))).Append("\n\n");
        table.Append("| ").Append(Resources.Strings.MdBrand)
            .Append(" | ").Append(Resources.Strings.MdLikelyTier)
            .Append(" | ").Append(Resources.Strings.MdLikelyCost)
            .Append(" | ").Append(Resources.Strings.MdBestTier)
            .Append(" | ").Append(Resources.Strings.MdBestCost)
            .Append(" | ").Append(Resources.Strings.MdDifference)
            .Append(" |\n| --- | --- | ---: | --- | ---: | ---: |\n");
        foreach (var line in estimate.Lines)
        {
            Row(
                table,
                CanonicalNames.ToName(line.Brand),
                Cell(line.LikelyTier),
                Cell(Money(line.LikelyCost, estimate.Currency)),
                Cell(line.BestTier),
                Cell(Money(line.BestCost, estimate.Currency)),
                Cell(Money(line.Difference, estimate.Currency)));
        }

        page.TryAdd(table.ToString());
    }

    private static void WriteBatch(Page page, ReportIndex index)
    {
        var batch = index.Report.BatchFindings;
        if (batch.Count == 0 || !page.TryAdd("### " + MarkdownText.Escape(Resources.Strings.TextBatchHeader) + "\n\n"))
        {
            return;
        }

        var header = "| " + Resources.Strings.LabelSeverity + " | " + Resources.Strings.MdRule + " | " + Resources.Strings.MdInput + " | "
            + Resources.Strings.MdTransaction + " | " + Resources.Strings.MdPath + " | " + Resources.Strings.LabelMessage + " | " + Resources.Strings.LabelFix
            + " |\n| --- | --- | --- | --- | --- | --- | --- |\n";
        if (!page.TryAdd(header))
        {
            return;
        }

        foreach (var finding in batch.OrderByDescending(f => f.Severity))
        {
            var anchor = index.Transaction(finding.TransactionIndex);
            var row = new StringBuilder();
            Row(
                row,
                WireNames.Of(finding.Severity),
                RuleLink(finding.RuleId, index),
                anchor is null ? string.Empty : Cell(index.InputPath(anchor) ?? string.Empty),
                anchor is null ? string.Empty : Cell(ReportIndex.TransactionName(anchor)),
                PathCell(finding),
                Cell(finding.Message),
                Cell(finding.Fix));
            if (!page.TryAddRow(row.ToString()))
            {
                return;
            }
        }

        page.TryAdd("\n");
    }

    private static string RuleLink(string ruleId, ReportIndex index)
    {
        var link = "[" + MarkdownText.Escape(ruleId) + "](" + EngineInfo.RuleHelpUri(ruleId).AbsoluteUri;
        if (index.Rule(ruleId) is { } rule)
        {
            link += " \"" + MarkdownText.Escape(MarkdownText.Clip(rule.Title, MaxCellLength)).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }

        return link + ")";
    }

    private static string PathCell(Finding finding)
    {
        var path = finding.Path.Source ?? finding.Path.Canonical;
        return MarkdownText.TableCode(MarkdownText.Clip(path.Length == 0 ? "/" : path, MaxCellLength));
    }

    private static string Cell(string text) => MarkdownText.Escape(MarkdownText.Clip(text, MaxCellLength));

    private static string Counts(IReadOnlyList<Finding> findings)
    {
        var parts = new List<string>();
        Add(parts, findings.Count(f => f.Severity == Severity.Error), Resources.Strings.WordError, Resources.Strings.WordErrors);
        Add(parts, findings.Count(f => f.Severity == Severity.Warning), Resources.Strings.WordWarning, Resources.Strings.WordWarnings);
        Add(parts, findings.Count(f => f.Severity == Severity.Information), Resources.Strings.WordInformation, Resources.Strings.WordInformation);
        return string.Join(", ", parts);

        static void Add(List<string> parts, int count, string singular, string plural)
        {
            if (count > 0)
            {
                parts.Add(Number(count) + " " + (count == 1 ? singular : plural));
            }
        }
    }

    private static void Row(StringBuilder table, params string[] cells) =>
        table.Append("| ").AppendJoin(" | ", cells).Append(" |\n");

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Money(decimal? value, string currency) =>
        value is { } amount ? amount.ToString(CultureInfo.InvariantCulture) + " " + currency : Resources.Strings.TextNoCategory;

    /// <summary>The page under construction, with a UTF-8 byte budget.</summary>
    private sealed class Page(int budget)
    {
        private readonly StringBuilder _text = new();
        private int _bytes;

        /// <summary>Gets a value indicating whether something did not fit, so the page stops growing.</summary>
        public bool Truncated { get; private set; }

        /// <summary>Gets how many finding rows were written.</summary>
        public int Rows { get; private set; }

        /// <summary>Appends text when it fits the budget and nothing was cut before.</summary>
        public bool TryAdd(string chunk)
        {
            if (Truncated)
            {
                return false;
            }

            var size = Encoding.UTF8.GetByteCount(chunk);
            if (_bytes + size > budget)
            {
                Truncated = true;
                return false;
            }

            _text.Append(chunk);
            _bytes += size;
            return true;
        }

        /// <summary>Appends one finding row when it fits.</summary>
        public bool TryAddRow(string row)
        {
            if (!TryAdd(row))
            {
                return false;
            }

            Rows++;
            return true;
        }

        /// <summary>Appends text that must appear, such as the heading, closing tags and the truncation note.</summary>
        public void Force(string chunk)
        {
            _text.Append(chunk);
            _bytes += Encoding.UTF8.GetByteCount(chunk);
        }

        public override string ToString() => _text.ToString();
    }
}
