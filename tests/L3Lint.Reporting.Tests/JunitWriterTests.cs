using System.Text;
using System.Xml.Linq;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Reporting.Junit;

namespace L3Lint.Reporting.Tests;

/// <summary>The JUnit XML rules (Interfaces, "Output formats", FR-024) and ADR-0051.</summary>
public sealed class JunitWriterTests
{
    [Fact]
    public void One_suite_per_input_then_the_batch_suite()
    {
        var suites = Document(TestReports.Small()).Root!.Elements("testsuite").Select(s => (string)s.Attribute("name")!).ToList();

        suites.ShouldBe(["invoices/INV-20417.stripe.json", "invoices/batch 2.ndjson", "invoices/broken.json", "Batch findings"]);
    }

    [Fact]
    public void One_case_per_transaction_named_by_id_or_run_number()
    {
        var suite = Suite(TestReports.Small(), "invoices/batch 2.ndjson");

        suite.Elements("testcase").Select(c => (string)c.Attribute("name")!).ShouldBe(["Transaction 2", "INV-3"]);
    }

    [Fact]
    public void Each_error_finding_is_a_failure_with_rule_message_path_and_fix()
    {
        var failure = Suite(TestReports.Small(), "invoices/INV-20417.stripe.json").Element("testcase")!.Elements("failure").Single();

        ((string)failure.Attribute("message")!).ShouldBe("VISA-L3-004: Line 2 has no commodity code; Visa needs one on every line for Product 3.");
        ((string)failure.Attribute("type")!).ShouldBe("VISA-L3-004");
        failure.Value.ShouldContain("Path: /amount_details/line_items/1/payment_method_options/card/commodity_code");
        failure.Value.ShouldContain("Fix: Add the most specific 8-digit UNSPSC code for this item.");
        failure.Value.ShouldContain("Rule: https://brimatt16219.github.io/l3lint/rules/VISA-L3-004.html");
    }

    [Fact]
    public void Warnings_and_information_go_to_system_out_not_failures()
    {
        var findings = new[]
        {
            TestReports.Finding("GEN-DQ-004", Severity.Warning, "A warning."),
            TestReports.Finding("GEN-DQ-001", Severity.Information, "Some information."),
        };

        var document = Document(TestReports.Many(findings));

        document.Descendants("failure").ShouldBeEmpty();
        document.Descendants("system-out").Select(e => e.Value).ShouldBe(
        [
            "warning GEN-DQ-004 /lineItems/0/description: A warning.\n  Fix: Use the real product or service name.",
            "information GEN-DQ-001 /lineItems/0/description: Some information.\n  Fix: Use the real product or service name.",
        ]);
        ((string)document.Root!.Attribute("failures")!).ShouldBe("0");
    }

    [Fact]
    public void Failures_count_failing_cases_not_failure_elements()
    {
        var finding = TestReports.Finding("VISA-L3-004", Severity.Error, "An error.");
        var report = TestReports.Many([finding, finding]);
        report = report with { Transactions = [report.Transactions[0] with { Findings = [finding, finding] }, report.Transactions[1]] };

        var root = Document(report).Root!;

        root.Descendants("failure").Count().ShouldBe(3);
        ((string)root.Attribute("tests")!).ShouldBe("2");
        ((string)root.Attribute("failures")!).ShouldBe("2");
        ((string)root.Element("testsuite")!.Attribute("failures")!).ShouldBe("2");
    }

    [Fact]
    public void Root_counts_are_the_sums_of_the_suites()
    {
        var root = Document(TestReports.Small()).Root!;
        var suites = root.Elements("testsuite").ToList();

        foreach (var name in new[] { "tests", "failures", "errors" })
        {
            ((int)root.Attribute(name)!).ShouldBe(suites.Sum(s => (int)s.Attribute(name)!), name);
        }

        ((int)root.Attribute("tests")!).ShouldBe(5);
        ((int)root.Attribute("failures")!).ShouldBe(1);
        ((int)root.Attribute("errors")!).ShouldBe(1);
    }

    [Fact]
    public void Input_errors_are_error_elements_in_their_input_suite()
    {
        var testCase = Suite(TestReports.Small(), "invoices/broken.json").Element("testcase")!;
        var error = testCase.Element("error")!;

        ((string)testCase.Attribute("name")!).ShouldBe("read input, line 3");
        ((string)error.Attribute("type")!).ShouldBe("Malformed");
        ((string)error.Attribute("message")!).ShouldBe("invoices/broken.json is not valid JSON: a value is missing after the comma.");
    }

    [Fact]
    public void An_input_error_without_a_listed_input_gets_its_own_suite()
    {
        var report = TestReports.Many([]) with
        {
            InputErrors = [new InputError("missing/*.json", InputErrorKind.Malformed, "No file matches missing/*.json.", Candidates: ["stripe", "adyen"])],
        };

        var suite = Suite(report, "missing/*.json");

        suite.Element("testcase")!.Attribute("name")!.Value.ShouldBe("read input");
        suite.Element("testcase")!.Element("error")!.Value.ShouldBe("No file matches missing/*.json.\nCandidates: stripe, adyen");
    }

    [Fact]
    public void Batch_findings_sit_in_their_own_suite_with_the_anchor_transaction()
    {
        var output = Suite(TestReports.Small(), "Batch findings").Element("testcase")!.Element("system-out")!.Value;

        output.ShouldContain("warning GEN-DQ-007 /lineItems/0/description:");
        output.ShouldContain("Transaction: Transaction 2 in invoices/batch 2.ndjson");
    }

    [Fact]
    public void No_batch_suite_without_batch_findings()
    {
        var report = TestReports.Small() with { BatchFindings = [] };

        Document(report).Root!.Elements("testsuite").ShouldNotContain(s => (string)s.Attribute("name")! == "Batch findings");
    }

    [Fact]
    public void Likely_tier_goes_to_system_out()
    {
        var output = Suite(TestReports.Small(), "invoices/batch 2.ndjson").Elements("testcase").Last().Element("system-out")!.Value;

        output.ShouldBe("Likely tier: Product 3 (meets the documented requirement)");
    }

    [Fact]
    public void No_times_timestamps_or_host_names()
    {
        var xml = JunitReportWriter.Render(TestReports.Small());

        xml.ShouldNotContain("time=");
        xml.ShouldNotContain("timestamp=");
        xml.ShouldNotContain("hostname=");
    }

    [Fact]
    public void Output_is_utf8_without_bom_with_lf_and_two_space_indent()
    {
        using var buffer = new MemoryStream();
        using (var writer = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { NewLine = "\n" })
        {
            new JunitReportWriter().Write(TestReports.Small(), writer, ReportWriterOptions.Default);
        }

        var bytes = buffer.ToArray();
        bytes.Take(3).ShouldNotBe(new byte[] { 0xEF, 0xBB, 0xBF });
        var text = Encoding.UTF8.GetString(bytes);
        text.ShouldStartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<testsuites ");
        text.ShouldContain("\n  <testsuite ");
        text.ShouldContain("\n    <testcase ");
        text.ShouldNotContain("\r");
        text.ShouldEndWith("</testsuites>\n");
    }

    [Fact]
    public async Task Real_run_has_one_suite_per_file_and_cases_named_by_transaction_id()
    {
        using var runs = new RealRuns();
        var report = await runs.RunAsync();

        var document = Document(report);

        document.Root!.Elements("testsuite").Select(s => (string)s.Attribute("name")!).ShouldBe(["invoices/a-clean.json", "invoices/b-broken.json", "invoices/c filler.json"]);
        document.Root.Elements("testsuite").Select(s => (string)s.Element("testcase")!.Attribute("name")!).ShouldBe(["INV-20417", "INV-20417", "INV-20418"]);
        Suite(report, "invoices/b-broken.json").Descendants("failure").Select(f => (string)f.Attribute("type")!).ShouldBe(["VISA-L3-004", "GEN-FMT-001"]);
    }

    private static XDocument Document(LintReport report) => XDocument.Parse(JunitReportWriter.Render(report));

    private static XElement Suite(LintReport report, string name) =>
        Document(report).Root!.Elements("testsuite").Single(s => (string)s.Attribute("name")! == name);
}
