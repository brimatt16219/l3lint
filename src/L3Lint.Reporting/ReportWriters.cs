using L3Lint.Reporting.Json;
using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Markdown;
using L3Lint.Reporting.Sarif;
using L3Lint.Reporting.Text;

namespace L3Lint.Reporting;

/// <summary>
/// The built-in output writers, by format name.
/// </summary>
public static class ReportWriters
{
    /// <summary>Gets every built-in writer: text, json, sarif, junit and markdown.</summary>
    public static IReadOnlyList<IReportWriter> All { get; } =
        [new TextReportWriter(), new JsonReportWriter(), new SarifReportWriter(), new JunitReportWriter(), new MarkdownReportWriter()];

    /// <summary>
    /// Finds a writer by format name.
    /// </summary>
    /// <param name="format">For example "json".</param>
    /// <param name="writer">The writer when found.</param>
    /// <returns><see langword="true"/> when the format is known.</returns>
    public static bool TryGet(string format, out IReportWriter writer)
    {
        writer = All.FirstOrDefault(w => string.Equals(w.Format, format, StringComparison.OrdinalIgnoreCase))!;
        return writer is not null;
    }
}
