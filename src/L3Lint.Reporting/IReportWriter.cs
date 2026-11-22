namespace L3Lint.Reporting;

/// <summary>
/// Renders a <see cref="LintReport"/> in one output format.
/// </summary>
/// <remarks>
/// Writers must be deterministic (NFR-001): the same report gives the same bytes. They must never write
/// unmasked values; reports only contain masked values (NFR-007). Writers are stateless and thread-safe.
/// </remarks>
/// <example>
/// <code>
/// public sealed class CsvSummaryWriter : IReportWriter
/// {
///     public string Format =&gt; "csv-summary";
///
///     public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
///     {
///         output.WriteLine("rule,count");
///         foreach (var (rule, count) in report.Summary.ByRule)
///         {
///             output.WriteLine($"{rule},{count}");
///         }
///     }
/// }
/// </code>
/// </example>
public interface IReportWriter
{
    /// <summary>Gets the format name used with <c>--output</c>, for example "json".</summary>
    string Format { get; }

    /// <summary>
    /// Writes the report.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="output">Where to write.</param>
    /// <param name="options">Rendering options such as color.</param>
    void Write(LintReport report, TextWriter output, ReportWriterOptions options);
}
