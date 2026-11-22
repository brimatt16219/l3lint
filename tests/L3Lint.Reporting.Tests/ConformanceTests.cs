using L3Lint.Core;
using L3Lint.Reporting.Json;
using L3Lint.Reporting.Junit;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Output conformance (Test and delivery, "Output conformance"): SARIF against the OASIS SARIF 2.1.0 schema, JUnit
/// against its XSD, JSON against results.schema.json, for hand-built and real reports.
/// </summary>
public sealed class ConformanceTests
{
    private static readonly ReportWriterOptions Options = ReportWriterOptions.Default;

    [Fact]
    public void Sarif_for_the_fixed_report_validates()
    {
        var errors = Schemas.ValidateSarif(SarifReportWriter.Render(TestReports.Small(), Options));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public async Task Sarif_for_a_real_run_validates()
    {
        using var runs = new RealRuns();
        var report = await runs.RunAsync();

        var errors = Schemas.ValidateSarif(SarifReportWriter.Render(report, Options with { BaseDirectory = runs.Folder }));

        report.Summary.Errors.ShouldBeGreaterThan(0);
        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Sarif_for_a_truncated_run_validates()
    {
        var findings = Enumerable.Range(0, SarifReportWriter.MaxResults + 10)
            .Select(i => TestReports.Finding("GEN-DQ-004", (Severity)(i % 3), "Message " + i))
            .ToList();

        var errors = Schemas.ValidateSarif(SarifReportWriter.Render(TestReports.Many(findings), Options));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Sarif_for_an_empty_report_validates()
    {
        var errors = Schemas.ValidateSarif(SarifReportWriter.Render(TestReports.Many([]), Options));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Sarif_validator_rejects_a_level_outside_the_schema()
    {
        var sarif = SarifReportWriter.Render(TestReports.Small(), Options).Replace("\"level\": \"error\"", "\"level\": \"fatal\"", StringComparison.Ordinal);

        Schemas.ValidateSarif(sarif).ShouldNotBeEmpty();
    }

    [Fact]
    public void Sarif_validator_rejects_a_property_the_schema_does_not_define()
    {
        var sarif = SarifReportWriter.Render(TestReports.Small(), Options).Replace("\"ruleIndex\"", "\"ruleNumber\"", StringComparison.Ordinal);

        Schemas.ValidateSarif(sarif).ShouldNotBeEmpty();
    }

    [Fact]
    public void Sarif_validator_rejects_an_artifact_uri_that_is_not_a_uri_reference()
    {
        var sarif = SarifReportWriter.Render(TestReports.Small(), Options).Replace("invoices/batch%202.ndjson", "invoices/batch 2.ndjson", StringComparison.Ordinal);

        Schemas.ValidateSarif(sarif).ShouldNotBeEmpty();
    }

    [Fact]
    public void Junit_for_the_fixed_report_validates()
    {
        var errors = Schemas.ValidateJunit(JunitReportWriter.Render(TestReports.Small()));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public async Task Junit_for_a_real_run_validates()
    {
        using var runs = new RealRuns();
        var report = await runs.RunAsync();

        var errors = Schemas.ValidateJunit(JunitReportWriter.Render(report));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Junit_for_an_empty_report_validates()
    {
        var errors = Schemas.ValidateJunit(JunitReportWriter.Render(TestReports.Many([])));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Junit_validator_rejects_a_testcase_without_a_name()
    {
        var xml = JunitReportWriter.Render(TestReports.Small()).Replace("<testcase name=\"INV-20417\"", "<testcase", StringComparison.Ordinal);

        Schemas.ValidateJunit(xml).ShouldNotBeEmpty();
    }

    [Fact]
    public void Junit_validator_rejects_an_unknown_attribute()
    {
        var xml = JunitReportWriter.Render(TestReports.Small()).Replace("<testcase name=\"INV-20417\"", "<testcase file=\"x\" name=\"INV-20417\"", StringComparison.Ordinal);

        Schemas.ValidateJunit(xml).ShouldNotBeEmpty();
    }

    [Fact]
    public void Json_for_the_fixed_report_validates()
    {
        var errors = Schemas.ValidateResults(JsonReportWriter.Render(TestReports.Small()));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public async Task Json_for_a_real_run_validates()
    {
        using var runs = new RealRuns();
        var report = await runs.RunAsync();

        var errors = Schemas.ValidateResults(JsonReportWriter.Render(report));

        errors.ShouldBeEmpty(string.Join("; ", errors));
    }

    [Fact]
    public void Json_validator_rejects_an_unknown_top_level_property()
    {
        var json = JsonReportWriter.Render(TestReports.Small()).Replace("\"exitCode\"", "\"exitStatus\"", StringComparison.Ordinal);

        Schemas.ValidateResults(json).ShouldNotBeEmpty();
    }
}
