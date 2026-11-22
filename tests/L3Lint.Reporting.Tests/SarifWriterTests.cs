using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Reporting.Sarif;

namespace L3Lint.Reporting.Tests;

/// <summary>The SARIF 2.1.0 mapping table (Interfaces, "Output formats") and GitHub's code scanning limits (FR-023).</summary>
public sealed class SarifWriterTests
{
    private const long GitHubGzipLimit = 10L * 1024 * 1024;

    [Fact]
    public void Driver_names_the_tool_version_and_project()
    {
        var driver = Run(TestReports.Small()).GetProperty("tool").GetProperty("driver");

        driver.GetProperty("name").GetString().ShouldBe("l3lint");
        driver.GetProperty("version").GetString().ShouldBe("1.0.0");
        driver.GetProperty("informationUri").GetString().ShouldBe(EngineInfo.ProjectUrl);
    }

    [Fact]
    public void Rules_are_the_rules_that_fired_sorted_by_id()
    {
        var rules = Run(TestReports.Small()).GetProperty("tool").GetProperty("driver").GetProperty("rules");

        rules.EnumerateArray().Select(r => r.GetProperty("id").GetString()).ShouldBe(["GEN-DQ-001", "GEN-DQ-004", "GEN-DQ-007", "VISA-L3-004"]);
    }

    [Fact]
    public void Rule_descriptor_comes_from_the_rule_metadata()
    {
        var rule = Rules(TestReports.Small()).Single(r => r.GetProperty("id").GetString() == "VISA-L3-004");

        rule.GetProperty("name").GetString().ShouldBe(TestReports.VisaL3004.Title);
        rule.GetProperty("shortDescription").GetProperty("text").GetString().ShouldBe(TestReports.VisaL3004.Title);
        rule.GetProperty("fullDescription").GetProperty("text").GetString().ShouldBe(TestReports.VisaL3004.Requirement);
        rule.GetProperty("helpUri").GetString().ShouldBe("https://brimatt16219.github.io/l3lint/rules/VISA-L3-004.html");
        rule.GetProperty("defaultConfiguration").GetProperty("level").GetString().ShouldBe("error");
        rule.GetProperty("properties").GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldBe(["visa", "L3", "processor-doc"]);
    }

    [Fact]
    public void Every_result_points_at_its_rule()
    {
        var run = Run(TestReports.Small());
        var rules = run.GetProperty("tool").GetProperty("driver").GetProperty("rules").EnumerateArray().ToList();

        foreach (var result in run.GetProperty("results").EnumerateArray())
        {
            var index = result.GetProperty("ruleIndex").GetInt32();
            rules[index].GetProperty("id").GetString().ShouldBe(result.GetProperty("ruleId").GetString());
        }
    }

    [Theory]
    [InlineData(Severity.Error, "error")]
    [InlineData(Severity.Warning, "warning")]
    [InlineData(Severity.Information, "note")]
    public void Severity_maps_to_the_sarif_level(Severity severity, string level)
    {
        var report = TestReports.Many([TestReports.Finding("GEN-DQ-004", severity, "Message.")]);

        Results(report).Single().GetProperty("level").GetString().ShouldBe(level);
    }

    [Fact]
    public void Message_is_the_message_plus_the_fix()
    {
        var report = TestReports.Many([TestReports.Finding("GEN-DQ-004", Severity.Warning, "Line 1 looks like filler.")]);

        Results(report).Single().GetProperty("message").GetProperty("text").GetString().ShouldBe("Line 1 looks like filler. Fix: Use the real product or service name.");
    }

    [Fact]
    public void Automation_id_is_l3lint_plus_the_config_hash()
    {
        var run = Run(TestReports.Small());

        run.GetProperty("automationDetails").GetProperty("id").GetString().ShouldBe("l3lint/" + TestReports.ConfigHash);
    }

    [Fact]
    public void Location_has_the_relative_uri_line_column_and_source_pointer()
    {
        var location = Results(TestReports.Small())[1].GetProperty("locations")[0];
        var physical = location.GetProperty("physicalLocation");

        physical.GetProperty("artifactLocation").GetProperty("uri").GetString().ShouldBe("invoices/INV-20417.stripe.json");
        physical.GetProperty("artifactLocation").GetProperty("uriBaseId").GetString().ShouldBe("%SRCROOT%");
        physical.GetProperty("region").GetProperty("startLine").GetInt32().ShouldBe(31);
        physical.GetProperty("region").GetProperty("startColumn").GetInt32().ShouldBe(9);
        location.GetProperty("logicalLocations")[0].GetProperty("fullyQualifiedName").GetString()
            .ShouldBe("/amount_details/line_items/1/payment_method_options/card/commodity_code");
    }

    [Fact]
    public void Logical_location_falls_back_to_the_canonical_pointer_when_the_format_has_no_source_path()
    {
        var finding = TestReports.Finding("GEN-DQ-004", Severity.Warning, "Message.") with { Path = new FieldPath("/customer/reference", null) };

        var location = Results(TestReports.Many([finding]))[0].GetProperty("locations")[0];

        location.GetProperty("logicalLocations")[0].GetProperty("fullyQualifiedName").GetString().ShouldBe("/customer/reference");
    }

    [Fact]
    public void A_finding_without_a_location_points_at_the_first_line_of_its_transaction()
    {
        var finding = TestReports.Finding("GEN-DQ-004", Severity.Warning, "Message.") with { Location = null };
        var report = TestReports.Many([finding]);
        report = report with { Transactions = [report.Transactions[0] with { FirstLine = 7 }] };

        var region = Results(report)[0].GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("region");

        region.GetProperty("startLine").GetInt32().ShouldBe(7);
        region.TryGetProperty("startColumn", out _).ShouldBeFalse();
    }

    [Fact]
    public void Standard_input_has_a_logical_location_only()
    {
        var report = TestReports.Many([TestReports.Finding("GEN-DQ-004", Severity.Warning, "Message.")], inputPath: "-");

        var location = Results(report)[0].GetProperty("locations")[0];

        location.TryGetProperty("physicalLocation", out _).ShouldBeFalse();
        location.GetProperty("logicalLocations")[0].GetProperty("fullyQualifiedName").GetString().ShouldBe("/lineItems/0/description");
    }

    [Fact]
    public void Absolute_input_paths_become_relative_to_the_base_directory_with_forward_slashes()
    {
        var root = Path.Combine(Path.GetTempPath(), "repo");
        var path = Path.Combine(root, "invoices", "may 2026", "INV#1.json");

        var (uri, relative) = SarifReportWriter.ArtifactUri(path, root);

        uri.ShouldBe("invoices/may%202026/INV%231.json");
        relative.ShouldBeTrue();
    }

    [Fact]
    public void Inputs_outside_the_base_directory_keep_a_relative_uri_with_parent_segments()
    {
        var root = Path.Combine(Path.GetTempPath(), "repo", "app");
        var path = Path.Combine(Path.GetTempPath(), "repo", "shared", "a.json");

        SarifReportWriter.ArtifactUri(path, root).ShouldBe(("../shared/a.json", true));
    }

    [Fact]
    public void Relative_input_paths_stay_relative_and_lose_dot_segments()
    {
        SarifReportWriter.ArtifactUri("./invoices\\a.json", baseDirectory: null).ShouldBe(("invoices/a.json", true));
    }

    [Fact]
    public void Absolute_input_paths_without_a_base_directory_become_file_uris()
    {
        var path = Path.Combine(Path.GetTempPath(), "a.json");

        var (uri, relative) = SarifReportWriter.ArtifactUri(path, baseDirectory: null);

        uri.ShouldBe(new Uri(path).AbsoluteUri);
        uri.ShouldStartWith("file:///");
        relative.ShouldBeFalse();
    }

    [Fact]
    public void Fingerprint_is_sha256_of_rule_source_pointer_and_transaction_id()
    {
        var expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            "VISA-L3-004\n/amount_details/line_items/1/payment_method_options/card/commodity_code\nINV-20417")));

        var result = Results(TestReports.Small())[1];

        result.GetProperty("partialFingerprints").GetProperty("l3lint/v1").GetString().ShouldBe(expected);
    }

    [Fact]
    public void Fingerprint_survives_line_moves_and_value_changes()
    {
        var finding = TestReports.Finding("GEN-DQ-004", Severity.Warning, "\"111111\" looks like filler.", line: 3, actual: "111111");
        var moved = finding with { Location = new SourceLocation(40, 2), Actual = "222222", Message = "\"222222\" looks like filler." };

        Fingerprint(TestReports.Many([finding])).ShouldBe(Fingerprint(TestReports.Many([moved])));
    }

    [Fact]
    public void Fingerprint_changes_with_the_transaction_id()
    {
        var report = TestReports.Many([TestReports.Finding("GEN-DQ-004", Severity.Warning, "Message.")]);
        var renamed = report with { Transactions = [report.Transactions[0] with { Id = "INV-99999" }] };

        Fingerprint(report).ShouldNotBe(Fingerprint(renamed));
    }

    [Fact]
    public void Fingerprint_without_a_transaction_id_uses_the_input_path_and_position()
    {
        var finding = TestReports.Finding("GEN-DQ-004", Severity.Warning, "Message.");
        var report = TestReports.Many([finding, finding]);
        report = report with { Transactions = [.. report.Transactions.Select(t => t with { Id = null })] };
        var expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("GEN-DQ-004\n/lineItems/0/description\ninvoices/many.ndjson#2")));

        var results = Results(report);

        results[1].GetProperty("partialFingerprints").GetProperty("l3lint/v1").GetString().ShouldBe(expected);
        results[0].GetProperty("partialFingerprints").GetProperty("l3lint/v1").GetString().ShouldNotBe(expected);
    }

    [Fact]
    public void Result_properties_carry_the_finding_details()
    {
        var properties = Results(TestReports.Small())[1].GetProperty("properties");

        properties.GetProperty("transactionIndex").GetInt32().ShouldBe(0);
        properties.GetProperty("transactionId").GetString().ShouldBe("INV-20417");
        properties.GetProperty("canonicalPath").GetString().ShouldBe("/lineItems/1/commodityCode");
        properties.GetProperty("expected").GetString().ShouldBe("8-digit commodity code");
        properties.GetProperty("actual").ValueKind.ShouldBe(JsonValueKind.Null);
        properties.GetProperty("confidence").GetString().ShouldBe("processor-doc");
        properties.GetProperty("sources")[0].GetString().ShouldBe(TestReports.Payroc.AbsoluteUri);
        properties.GetProperty("impact").GetString().ShouldBe("blocksLevel3");
    }

    [Fact]
    public void Input_errors_become_tool_execution_notifications()
    {
        var invocation = Run(TestReports.Small()).GetProperty("invocations")[0];
        var notification = invocation.GetProperty("toolExecutionNotifications")[0];

        invocation.GetProperty("executionSuccessful").GetBoolean().ShouldBeFalse();
        invocation.GetProperty("exitCode").GetInt32().ShouldBe(2);
        notification.GetProperty("level").GetString().ShouldBe("error");
        notification.GetProperty("message").GetProperty("text").GetString().ShouldBe(TestReports.Small().InputErrors[0].Message);
        notification.GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("region").GetProperty("startLine").GetInt32().ShouldBe(3);
    }

    [Fact]
    public void Run_properties_record_findings_left_out_by_max_findings()
    {
        var report = TestReports.Small() with { FindingsOmitted = 4 };

        Run(report).GetProperty("properties").GetProperty("findingsOmitted").GetInt32().ShouldBe(4);
    }

    [Fact]
    public void Six_thousand_findings_give_five_thousand_results_with_errors_and_warnings_kept()
    {
        // 2,000 of each severity, interleaved so the cut cannot follow report order by accident.
        var findings = Enumerable.Range(0, 6000)
            .Select(i => TestReports.Finding(i % 3 == 0 ? "VISA-L3-004" : "GEN-DQ-004", (Severity)(2 - (i % 3)), "Finding " + i, line: i + 1))
            .ToList();

        var run = Run(TestReports.Many(findings));
        var results = run.GetProperty("results").EnumerateArray().ToList();

        results.Count.ShouldBe(5000);
        results.Count(r => r.GetProperty("level").GetString() == "error").ShouldBe(2000);
        results.Count(r => r.GetProperty("level").GetString() == "warning").ShouldBe(2000);
        var notes = results.Where(r => r.GetProperty("ruleId").GetString() != SarifReportWriter.TruncatedRuleId && r.GetProperty("level").GetString() == "note").ToList();
        notes.Count.ShouldBe(999);
        notes.Select(r => r.GetProperty("properties").GetProperty("transactionIndex").GetInt32())
            .ShouldBe(Enumerable.Range(0, 6000).Where(i => i % 3 == 2).Take(999));
        run.GetProperty("properties").GetProperty("resultsDropped").GetInt32().ShouldBe(1001);
    }

    [Fact]
    public void Kept_results_stay_in_report_order_and_the_note_comes_last()
    {
        var findings = Enumerable.Range(0, 5100)
            .Select(i => TestReports.Finding("GEN-DQ-004", i % 2 == 0 ? Severity.Information : Severity.Warning, "Finding " + i))
            .ToList();

        var results = Results(TestReports.Many(findings));
        var indexes = results.Take(results.Count - 1).Select(r => r.GetProperty("properties").GetProperty("transactionIndex").GetInt32()).ToList();

        indexes.ShouldBe(indexes.Order().ToList());
        var note = results[^1];
        note.GetProperty("ruleId").GetString().ShouldBe(SarifReportWriter.TruncatedRuleId);
        note.GetProperty("level").GetString().ShouldBe("note");
        note.GetProperty("message").GetProperty("text").GetString()
            .ShouldBe("This run has 5,100 findings, more than the 5,000 results GitHub code scanning accepts in one upload. The 101 least severe findings are left out of this SARIF file; the JSON output lists every finding.");
        note.GetProperty("locations")[0].GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString().ShouldBe("invoices/many.ndjson");
    }

    [Fact]
    public void Truncation_note_has_a_rule_descriptor()
    {
        var findings = Enumerable.Range(0, 5001).Select(i => TestReports.Finding("GEN-DQ-004", Severity.Warning, "Finding " + i)).ToList();

        var rule = Rules(TestReports.Many(findings)).Single(r => r.GetProperty("id").GetString() == SarifReportWriter.TruncatedRuleId);

        rule.GetProperty("defaultConfiguration").GetProperty("level").GetString().ShouldBe("note");
        rule.GetProperty("shortDescription").GetProperty("text").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Exactly_five_thousand_findings_are_all_kept()
    {
        var findings = Enumerable.Range(0, 5000).Select(i => TestReports.Finding("GEN-DQ-004", Severity.Information, "Finding " + i)).ToList();

        var run = Run(TestReports.Many(findings));

        run.GetProperty("results").GetArrayLength().ShouldBe(5000);
        run.GetProperty("results").EnumerateArray().ShouldNotContain(r => r.GetProperty("ruleId").GetString() == SarifReportWriter.TruncatedRuleId);
        run.GetProperty("properties").TryGetProperty("resultsDropped", out _).ShouldBeFalse();
    }

    [Fact]
    public void A_full_upload_stays_under_github_gzip_limit()
    {
        var message = "Line 1 description \"" + new string('x', 400) + "\" is a placeholder, not an item name.";
        var findings = Enumerable.Range(0, 6000).Select(i => TestReports.Finding("GEN-DQ-004", Severity.Warning, message + i, actual: new string('y', 400))).ToList();
        var sarif = SarifReportWriter.Render(TestReports.Many(findings), ReportWriterOptions.Default);

        var size = GzipSize(sarif);

        size.ShouldBeLessThan(GitHubGzipLimit);
        size.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Gzip_size_helper_measures_compressed_bytes()
    {
        var text = new string('a', 1_000_000);

        GzipSize(text).ShouldBeLessThan(Encoding.UTF8.GetByteCount(text) / 100);
    }

    [Fact]
    public void Masked_transaction_values_never_reach_the_output()
    {
        var report = TestReports.Small();
        var transaction = report.Transactions[0] with
        {
            Transaction = new Transaction { Id = "INV-20417", Order = new OrderInfo { InvoiceNumber = "SECRET-INVOICE-77" } },
        };
        report = report with { Transactions = [transaction, .. report.Transactions.Skip(1)] };

        SarifReportWriter.Render(report, ReportWriterOptions.Default).ShouldNotContain("SECRET-INVOICE-77");
    }

    [Fact]
    public void Rules_from_a_real_run_use_the_pack_titles()
    {
        var rule = RealRuns.Linter.Explain("VISA-L3-004");
        var report = TestReports.Small() with { Rules = [rule] };

        var descriptor = Rules(report).Single(r => r.GetProperty("id").GetString() == "VISA-L3-004");

        descriptor.GetProperty("name").GetString().ShouldBe(rule.Title);
        descriptor.GetProperty("fullDescription").GetProperty("text").GetString().ShouldBe(rule.Requirement);
    }

    [Fact]
    public void Writer_ends_with_one_newline_and_uses_lf_only()
    {
        using var output = new StringWriter();
        new SarifReportWriter().Write(TestReports.Small(), output, ReportWriterOptions.Default);
        var text = output.ToString();

        text.ShouldEndWith("}\n");
        text.ShouldNotEndWith("}\n\n");
        text.ShouldNotContain("\r");
    }

    internal static long GzipSize(string text)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            gzip.Write(bytes, 0, bytes.Length);
        }

        return buffer.Length;
    }

    private static JsonElement Run(LintReport report) =>
        JsonDocument.Parse(SarifReportWriter.Render(report, ReportWriterOptions.Default)).RootElement.GetProperty("runs")[0].Clone();

    private static List<JsonElement> Results(LintReport report) => [.. Run(report).GetProperty("results").EnumerateArray()];

    private static List<JsonElement> Rules(LintReport report) => [.. Run(report).GetProperty("tool").GetProperty("driver").GetProperty("rules").EnumerateArray()];

    private static string? Fingerprint(LintReport report) =>
        Results(report)[0].GetProperty("partialFingerprints").GetProperty("l3lint/v1").GetString();
}
