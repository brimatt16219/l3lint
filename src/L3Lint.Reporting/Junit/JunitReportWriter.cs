using System.Globalization;
using System.Text;
using System.Xml;
using L3Lint.Core;
using L3Lint.Core.Model;

namespace L3Lint.Reporting.Junit;

/// <summary>
/// JUnit XML for CI dashboards (FR-024): one <c>testsuite</c> per input file and one <c>testcase</c> per transaction,
/// named by transaction ID. Each error finding is a <c>failure</c>; warnings, information and the likely tier go to
/// <c>system-out</c>. Batch findings get their own suite and inputs that could not be read become <c>error</c>
/// elements (ADR-0051). The output validates against the Jenkins xUnit plugin's JUnit schema, uses UTF-8 without
/// a byte order mark and LF line endings, and carries no times or timestamps, so identical reports give identical bytes.
/// </summary>
public sealed class JunitReportWriter : IReportWriter
{
    private const char Replacement = '�';

    private static readonly XmlWriterSettings Settings = new()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = true,
        IndentChars = "  ",
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.Replace,
    };

    /// <inheritdoc/>
    public string Format => "junit";

    /// <inheritdoc/>
    /// <remarks>
    /// Suite counts come from a first pass; test cases are then built and written one at a time, so a large batch
    /// is never held as one document (NFR-003).
    /// </remarks>
    public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        WriteTo(report, output);
        output.Write('\n');
    }

    /// <summary>Renders the report as a JUnit XML document.</summary>
    internal static string Render(LintReport report)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        WriteTo(report, output);
        return output.ToString();
    }

    private static void WriteTo(LintReport report, TextWriter output)
    {
        var index = new ReportIndex(report);
        var suites = BuildSuites(index);
        using var stream = new Utf8TextWriterStream(output);
        using (var xml = XmlWriter.Create(stream, Settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("testsuites");
            xml.WriteAttributeString("name", "l3lint");
            WriteCounts(xml, suites.Aggregate(Counts.None, (total, suite) => total + suite.Counts));
            foreach (var suite in suites)
            {
                xml.WriteStartElement("testsuite");
                xml.WriteAttributeString("name", Clean(suite.Name));
                WriteCounts(xml, suite.Counts);
                foreach (var transaction in suite.Transactions)
                {
                    WriteCase(xml, TransactionCase(transaction));
                }

                foreach (var testCase in suite.Cases)
                {
                    WriteCase(xml, testCase);
                }

                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
    }

    /// <summary>
    /// Makes text safe for XML 1.0: CR LF and lone CR become LF, and characters XML cannot carry (most control
    /// characters and unpaired surrogates) become U+FFFD.
    /// </summary>
    internal static string Clean(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r')
            {
                builder.Append('\n');
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else if (char.IsHighSurrogate(c) && i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c))
            {
                builder.Append(c).Append(text[i + 1]);
                i++;
            }
            else
            {
                builder.Append(XmlConvert.IsXmlChar(c) ? c : Replacement);
            }
        }

        return builder.ToString();
    }

    /// <summary>Lays out the suites. Transaction cases stay unbuilt until they are written; other cases are few.</summary>
    private static List<Suite> BuildSuites(ReportIndex index)
    {
        var report = index.Report;
        var suites = new List<Suite>();
        foreach (var group in index.Groups)
        {
            suites.Add(new Suite(group.Name, group.Transactions, []));
        }

        foreach (var error in report.InputErrors)
        {
            var suite = suites.FirstOrDefault(s => string.Equals(s.Name, error.SourceName, StringComparison.Ordinal));
            if (suite is null)
            {
                suite = new Suite(error.SourceName, [], []);
                suites.Add(suite);
            }

            suite.Cases.Add(InputErrorCase(error));
        }

        if (report.BatchFindings.Count > 0)
        {
            var batch = new Case(Resources.Strings.JunitBatchCase);
            foreach (var finding in report.BatchFindings)
            {
                var anchor = index.Transaction(finding.TransactionIndex);
                var where = anchor is null
                    ? null
                    : string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitTransaction, ReportIndex.TransactionName(anchor), index.InputPath(anchor) ?? string.Empty);
                AddFinding(batch, finding, where);
            }

            suites.Add(new Suite(Resources.Strings.TextBatchHeader, [], [batch]));
        }

        foreach (var suite in suites)
        {
            suite.Count();
        }

        return suites;
    }

    private static Case TransactionCase(TransactionResult transaction)
    {
        var testCase = new Case(ReportIndex.TransactionName(transaction));
        foreach (var tier in transaction.Tiers)
        {
            var label = transaction.Tiers.Count > 1
                ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextTierForBrand, CanonicalNames.ToName(tier.Brand))
                : Resources.Strings.TextTier;
            testCase.Out.Add(label + " " + TierNames.Describe(tier));
        }

        foreach (var finding in transaction.Findings)
        {
            AddFinding(testCase, finding, where: null);
        }

        return testCase;
    }

    private static void AddFinding(Case testCase, Finding finding, string? where)
    {
        var path = finding.Path.Source ?? finding.Path.Canonical;
        path = path.Length == 0 ? "/" : path;
        if (finding.Severity != Severity.Error)
        {
            testCase.Out.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitOutFinding, WireNames.Of(finding.Severity), finding.RuleId, path, finding.Message));
            if (where is not null)
            {
                testCase.Out.Add("  " + where);
            }

            testCase.Out.Add("  " + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextFix, finding.Fix));
            return;
        }

        var lines = new List<string> { string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitPath, path) };
        if (finding.Path.Source is { } source && source != finding.Path.Canonical)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitCanonicalPath, finding.Path.Canonical.Length == 0 ? "/" : finding.Path.Canonical));
        }

        if (finding.Location is { } location)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitLocation, location.Line, location.Column));
        }

        if (where is not null)
        {
            lines.Add(where);
        }

        if (finding.Expected is { } expected)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitExpected, expected));
        }

        if (finding.Actual is { } actual)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitActual, actual));
        }

        lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextFix, finding.Fix));
        lines.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitRule, EngineInfo.RuleHelpUri(finding.RuleId).AbsoluteUri));
        var message = string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitFailureMessage, finding.RuleId, finding.Message);
        testCase.Failures.Add(new Problem(message, finding.RuleId, string.Join('\n', lines)));
    }

    private static Case InputErrorCase(InputError error)
    {
        var name = error.Line is { } line
            ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.JunitInputErrorLine, line)
            : Resources.Strings.JunitInputErrorCase;
        var testCase = new Case(name);
        var text = error.Candidates is { Count: > 0 } candidates
            ? error.Message + "\n" + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextCandidates, string.Join(", ", candidates))
            : error.Message;
        testCase.Errors.Add(new Problem(error.Message, error.Kind.ToString(), text));
        return testCase;
    }

    private static void WriteCounts(XmlWriter xml, Counts counts)
    {
        xml.WriteAttributeString("tests", counts.Tests.ToString(CultureInfo.InvariantCulture));
        xml.WriteAttributeString("failures", counts.Failures.ToString(CultureInfo.InvariantCulture));
        xml.WriteAttributeString("errors", counts.Errors.ToString(CultureInfo.InvariantCulture));
    }

    private static void WriteCase(XmlWriter xml, Case testCase)
    {
        xml.WriteStartElement("testcase");
        xml.WriteAttributeString("name", Clean(testCase.Name));
        foreach (var error in testCase.Errors)
        {
            WriteProblem(xml, "error", error);
        }

        foreach (var failure in testCase.Failures)
        {
            WriteProblem(xml, "failure", failure);
        }

        if (testCase.Out.Count > 0)
        {
            xml.WriteElementString("system-out", Clean(string.Join('\n', testCase.Out)));
        }

        xml.WriteEndElement();
    }

    private static void WriteProblem(XmlWriter xml, string element, Problem problem)
    {
        xml.WriteStartElement(element);
        xml.WriteAttributeString("message", Clean(problem.Message));
        xml.WriteAttributeString("type", Clean(problem.Type));
        xml.WriteString(Clean(problem.Text));
        xml.WriteEndElement();
    }

    /// <summary>
    /// A test suite: one input, the batch checks, or an input that never produced transactions. Its transactions
    /// become cases when written; <see cref="Cases"/> holds the input error and batch cases that follow them.
    /// </summary>
    private sealed record Suite(string Name, IReadOnlyList<TransactionResult> Transactions, List<Case> Cases)
    {
        public Counts Counts { get; private set; } = Counts.None;

        /// <summary>Counts the cases in one pass. A transaction case fails when it has an error finding and never errors.</summary>
        public void Count()
        {
            var failures = Transactions.Count(t => t.Findings.Any(f => f.Severity == Severity.Error));
            Counts = new Counts(
                Transactions.Count + Cases.Count,
                failures + Cases.Count(c => c.Failures.Count > 0),
                Cases.Count(c => c.Errors.Count > 0));
        }
    }

    /// <summary>The tests, failures and errors attributes of a suite or of the whole document.</summary>
    private readonly record struct Counts(int Tests, int Failures, int Errors)
    {
        public static Counts None => default;

        public static Counts operator +(Counts left, Counts right) =>
            new(left.Tests + right.Tests, left.Failures + right.Failures, left.Errors + right.Errors);
    }

    /// <summary>A test case and what it reports.</summary>
    private sealed class Case(string name)
    {
        public string Name { get; } = name;

        public List<Problem> Failures { get; } = [];

        public List<Problem> Errors { get; } = [];

        public List<string> Out { get; } = [];
    }

    /// <summary>One failure or error element.</summary>
    private sealed record Problem(string Message, string Type, string Text);
}
