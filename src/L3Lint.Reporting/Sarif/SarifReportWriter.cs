using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Reporting.Markdown;

namespace L3Lint.Reporting.Sarif;

/// <summary>
/// SARIF 2.1.0 within GitHub's code scanning subset (FR-023), hand-written with <see cref="Utf8JsonWriter"/>. It
/// follows the mapping table in the Interfaces spec ("Output formats") and ADR-0050. Property order is fixed, rules
/// sort by ID, results keep the report's order, and nothing depends on the clock or the machine, so identical
/// reports give byte-identical files (NFR-001).
/// </summary>
/// <remarks>
/// GitHub shows at most 5,000 results per upload. Above that, the writer keeps errors first, then warnings, then
/// information, and spends the last result on a note that says how many findings were left out.
/// </remarks>
public sealed class SarifReportWriter : IReportWriter
{
    /// <summary>The most results one SARIF file holds, including the truncation note.</summary>
    internal const int MaxResults = 5000;

    /// <summary>The <c>$schema</c> URI: the OASIS SARIF 2.1.0 errata 01 schema.</summary>
    internal const string SchemaUri = "https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json";

    /// <summary>The key of l3lint's fingerprint in <c>partialFingerprints</c>.</summary>
    internal const string FingerprintKey = "l3lint/v1";

    /// <summary>The rule ID of the note that reports results left out for GitHub's limit (reserved L3LINT- namespace, ADR-0033).</summary>
    internal const string TruncatedRuleId = "L3LINT-SARIF-001";

    /// <summary>The URI base ID for paths relative to the repository root, as GitHub's examples use.</summary>
    internal const string SourceRoot = "%SRCROOT%";

    private const int MaxNameLength = 255;
    private const int MaxDescriptionLength = 1024;

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc/>
    public string Format => "sarif";

    /// <inheritdoc/>
    public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(options);
        output.Write(Render(report, options));
        output.Write('\n');
    }

    /// <summary>Renders the report as a SARIF log.</summary>
    internal static string Render(LintReport report, ReportWriterOptions options)
    {
        var index = new ReportIndex(report);
        var (kept, total) = Select(report.AllFindings());
        var dropped = total - kept.Count;
        var rules = BuildRules(kept, index, dropped > 0);
        var ruleIndex = rules.Select((rule, i) => (rule.Id, i)).ToDictionary(p => p.Id, p => p.i, StringComparer.Ordinal);

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteString("$schema", SchemaUri);
            json.WriteString("version", "2.1.0");
            json.WriteStartArray("runs");
            json.WriteStartObject();
            WriteTool(json, report, rules);
            WriteInvocation(json, report, options);
            json.WriteStartObject("automationDetails");
            json.WriteString("id", "l3lint/" + report.ConfigHash);
            json.WriteEndObject();
            json.WriteString("columnKind", "utf16CodeUnits");
            json.WriteStartArray("results");
            foreach (var finding in kept)
            {
                WriteResult(json, finding, ruleIndex[finding.RuleId], index, options);
            }

            if (dropped > 0)
            {
                WriteTruncationNote(json, total, dropped, ruleIndex[TruncatedRuleId], report, options);
            }

            json.WriteEndArray();
            WriteRunProperties(json, report, dropped);
            json.WriteEndObject();
            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Picks the findings that fit GitHub's limit: all of them when they fit, otherwise the most severe
    /// <see cref="MaxResults"/> minus one (earlier findings win ties), kept in report order.
    /// </summary>
    /// <remarks>
    /// With three severities, "most severe, earliest first" means the first few findings of each severity, so
    /// one counting pass and one keeping pass hold no more than the kept findings (NFR-003).
    /// </remarks>
    /// <returns>The kept findings and the number of findings in the report.</returns>
    internal static (List<Finding> Kept, int Total) Select(IEnumerable<Finding> findings)
    {
        var bySeverity = new int[3];
        foreach (var finding in findings)
        {
            bySeverity[(int)finding.Severity]++;
        }

        var total = bySeverity.Sum();
        var room = total <= MaxResults ? total : MaxResults - 1;
        var quota = new int[3];
        foreach (var severity in new[] { Severity.Error, Severity.Warning, Severity.Information })
        {
            quota[(int)severity] = Math.Min(bySeverity[(int)severity], room);
            room -= quota[(int)severity];
        }

        var kept = new List<Finding>(quota.Sum());
        foreach (var finding in findings)
        {
            if (quota[(int)finding.Severity] > 0)
            {
                quota[(int)finding.Severity]--;
                kept.Add(finding);
            }
        }

        return (kept, total);
    }

    /// <summary>Gets the SARIF level of a severity: error, warning or note.</summary>
    internal static string Level(Severity severity) => severity switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "note",
    };

    /// <summary>
    /// Computes the stable fingerprint: SHA-256 of the rule ID, the source pointer (canonical when the format has
    /// none) and the transaction ID (the input path and position when the payload has no ID). Payload values other
    /// than the transaction ID never enter it, so alerts survive line moves and value edits.
    /// </summary>
    internal static string Fingerprint(Finding finding, TransactionResult? transaction, ReportIndex index)
    {
        var key = transaction is null
            ? string.Empty
            : transaction.Id ?? (index.InputPath(transaction) ?? string.Empty) + "#" + index.PositionInInput(transaction).ToString(CultureInfo.InvariantCulture);
        var text = finding.RuleId + "\n" + (finding.Path.Source ?? finding.Path.Canonical) + "\n" + key;
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>
    /// Turns an input path into an artifact URI: relative to <paramref name="baseDirectory"/> with forward slashes
    /// and percent-encoded segments when possible, otherwise an absolute file URI.
    /// </summary>
    /// <returns>The URI and whether it is relative to the repository root.</returns>
    internal static (string Uri, bool Relative) ArtifactUri(string path, string? baseDirectory)
    {
        var relative = path;
        if (IsAbsolute(path))
        {
            if (baseDirectory is null)
            {
                return (new Uri(path).AbsoluteUri, false);
            }

            relative = Path.GetRelativePath(baseDirectory, path);
            if (IsAbsolute(relative))
            {
                return (new Uri(path).AbsoluteUri, false);
            }
        }

        var segments = relative.Replace('\\', '/').Split('/').Where(s => s.Length > 0 && s != ".");
        return (string.Join('/', segments.Select(Uri.EscapeDataString)), true);
    }

    private static bool IsAbsolute(string path) =>
        Path.IsPathRooted(path) || (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\');

    private static List<Descriptor> BuildRules(List<Finding> kept, ReportIndex index, bool truncated)
    {
        var rules = kept
            .GroupBy(f => f.RuleId, StringComparer.Ordinal)
            .Select(g => index.Rule(g.Key) is { } rule ? Descriptor.From(rule) : Descriptor.Fallback(g.First()))
            .ToList();
        if (truncated)
        {
            rules.Add(Descriptor.Truncated());
        }

        rules.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return rules;
    }

    private static void WriteTool(Utf8JsonWriter json, LintReport report, List<Descriptor> rules)
    {
        json.WriteStartObject("tool");
        json.WriteStartObject("driver");
        json.WriteString("name", "l3lint");
        json.WriteString("version", report.ToolVersion);
        json.WriteString("informationUri", EngineInfo.ProjectUrl);
        json.WriteStartArray("rules");
        foreach (var rule in rules)
        {
            rule.Write(json);
        }

        json.WriteEndArray();
        json.WriteEndObject();
        json.WriteEndObject();
    }

    private static void WriteInvocation(Utf8JsonWriter json, LintReport report, ReportWriterOptions options)
    {
        json.WriteStartArray("invocations");
        json.WriteStartObject();
        json.WriteBoolean("executionSuccessful", report.ExitCode is 0 or 1);
        json.WriteNumber("exitCode", report.ExitCode);
        if (report.InputErrors.Count > 0)
        {
            json.WriteStartArray("toolExecutionNotifications");
            foreach (var error in report.InputErrors)
            {
                json.WriteStartObject();
                if (error.SourceName != "-")
                {
                    json.WriteStartArray("locations");
                    json.WriteStartObject();
                    WritePhysicalLocation(json, error.SourceName, error.Line is { } line ? (line, null) : null, options);
                    json.WriteEndObject();
                    json.WriteEndArray();
                }

                json.WriteStartObject("message");
                json.WriteString("text", error.Message);
                json.WriteEndObject();
                json.WriteString("level", "error");
                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        json.WriteEndObject();
        json.WriteEndArray();
    }

    private static void WriteResult(Utf8JsonWriter json, Finding finding, int ruleIndex, ReportIndex index, ReportWriterOptions options)
    {
        var transaction = index.Transaction(finding.TransactionIndex);
        json.WriteStartObject();
        json.WriteString("ruleId", finding.RuleId);
        json.WriteNumber("ruleIndex", ruleIndex);
        json.WriteString("level", Level(finding.Severity));
        json.WriteStartObject("message");
        json.WriteString("text", string.Format(CultureInfo.InvariantCulture, Resources.Strings.SarifResultMessage, finding.Message, finding.Fix));
        json.WriteEndObject();

        json.WriteStartArray("locations");
        json.WriteStartObject();
        var path = transaction is null ? null : index.InputPath(transaction);
        if (path is not null)
        {
            (int Line, int? Column) region = finding.Location is { } location ? (location.Line, location.Column) : (transaction!.FirstLine, null);
            WritePhysicalLocation(json, path, region, options);
        }

        json.WriteStartArray("logicalLocations");
        json.WriteStartObject();
        json.WriteString("fullyQualifiedName", finding.Path.Source ?? finding.Path.Canonical);
        json.WriteEndObject();
        json.WriteEndArray();
        json.WriteEndObject();
        json.WriteEndArray();

        json.WriteStartObject("partialFingerprints");
        json.WriteString(FingerprintKey, Fingerprint(finding, transaction, index));
        json.WriteEndObject();

        json.WriteStartObject("properties");
        json.WriteNumber("transactionIndex", finding.TransactionIndex);
        if (transaction?.Id is { } id)
        {
            json.WriteString("transactionId", id);
        }

        json.WriteString("canonicalPath", finding.Path.Canonical);
        WriteNullable(json, "expected", finding.Expected);
        WriteNullable(json, "actual", finding.Actual);
        json.WriteString("confidence", WireNames.Of(finding.Confidence));
        json.WriteStartArray("sources");
        foreach (var source in finding.Sources)
        {
            json.WriteStringValue(source.AbsoluteUri);
        }

        json.WriteEndArray();
        json.WriteString("impact", WireNames.Of(finding.Impact));
        json.WriteEndObject();
        json.WriteEndObject();
    }

    private static void WriteTruncationNote(Utf8JsonWriter json, int total, int dropped, int ruleIndex, LintReport report, ReportWriterOptions options)
    {
        json.WriteStartObject();
        json.WriteString("ruleId", TruncatedRuleId);
        json.WriteNumber("ruleIndex", ruleIndex);
        json.WriteString("level", "note");
        json.WriteStartObject("message");
        json.WriteString("text", string.Format(CultureInfo.InvariantCulture, Resources.Strings.SarifTruncatedMessage, total, MaxResults, dropped));
        json.WriteEndObject();
        json.WriteStartArray("locations");
        if (report.Inputs.FirstOrDefault(i => i.Path != "-") is { } input)
        {
            json.WriteStartObject();
            WritePhysicalLocation(json, input.Path, (1, 1), options);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteStartObject("partialFingerprints");
        json.WriteString(FingerprintKey, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(TruncatedRuleId))));
        json.WriteEndObject();
        json.WriteStartObject("properties");
        json.WriteNumber("findings", total);
        json.WriteNumber("findingsDropped", dropped);
        json.WriteEndObject();
        json.WriteEndObject();
    }

    private static void WritePhysicalLocation(Utf8JsonWriter json, string path, (int Line, int? Column)? region, ReportWriterOptions options)
    {
        if (path == "-")
        {
            return;
        }

        var (uri, relative) = ArtifactUri(path, options.BaseDirectory);
        json.WriteStartObject("physicalLocation");
        json.WriteStartObject("artifactLocation");
        json.WriteString("uri", uri);
        if (relative)
        {
            json.WriteString("uriBaseId", SourceRoot);
        }

        json.WriteEndObject();
        if (region is { } r && r.Line > 0)
        {
            json.WriteStartObject("region");
            json.WriteNumber("startLine", r.Line);
            if (r.Column is { } column && column > 0)
            {
                json.WriteNumber("startColumn", column);
            }

            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteRunProperties(Utf8JsonWriter json, LintReport report, int dropped)
    {
        json.WriteStartObject("properties");
        json.WriteStartObject("rulePacks");
        foreach (var (id, version) in report.RulePacks.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            json.WriteString(id, version);
        }

        json.WriteEndObject();
        json.WriteString("profile", report.Profile);
        if (report.AsOf is { } asOf)
        {
            json.WriteString("asOf", asOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (report.FindingsOmitted > 0)
        {
            json.WriteNumber("findingsOmitted", report.FindingsOmitted);
        }

        if (dropped > 0)
        {
            json.WriteNumber("resultsDropped", dropped);
        }

        json.WriteEndObject();
    }

    private static void WriteNullable(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null)
        {
            json.WriteNull(name);
        }
        else
        {
            json.WriteString(name, value);
        }
    }

    /// <summary>One <c>reportingDescriptor</c> in <c>tool.driver.rules</c>.</summary>
    private sealed record Descriptor(
        string Id,
        string Name,
        string ShortDescription,
        string FullDescription,
        string HelpText,
        string HelpMarkdown,
        Uri HelpUri,
        Severity DefaultSeverity,
        IReadOnlyList<string> Tags)
    {
        public static Descriptor From(RuleMetadata rule)
        {
            var helpUri = EngineInfo.RuleHelpUri(rule.Id);
            var requirement = rule.Requirement ?? rule.Title;
            var text = new StringBuilder(requirement);
            var markdown = new StringBuilder(MarkdownText.Escape(requirement));
            if (rule.Rationale is { } rationale)
            {
                text.Append("\n\n").Append(Resources.Strings.LabelWhy).Append(": ").Append(rationale);
                markdown.Append("\n\n**").Append(Resources.Strings.LabelWhy).Append(":** ").Append(MarkdownText.Escape(rationale));
            }

            text.Append("\n\n").Append(Resources.Strings.LabelFix).Append(": ").Append(rule.Fix);
            text.Append("\n\n").Append(Resources.Strings.SarifRuleReference).Append(": ").Append(helpUri.AbsoluteUri);
            markdown.Append("\n\n**").Append(Resources.Strings.LabelFix).Append(":** ").Append(MarkdownText.Escape(rule.Fix));
            markdown.Append("\n\n[").Append(Resources.Strings.SarifRuleReference).Append("](").Append(helpUri.AbsoluteUri).Append(')');
            var tags = new List<string>
            {
                rule.Brand is { } brand ? CanonicalNames.ToName(brand) : "general",
                WireNames.Of(rule.Level),
                WireNames.Of(rule.Confidence),
            };
            return new Descriptor(rule.Id, rule.Title, rule.Title, requirement, text.ToString(), markdown.ToString(), helpUri, rule.Severity, [.. tags.Distinct(StringComparer.Ordinal)]);
        }

        public static Descriptor Fallback(Finding finding)
        {
            var helpUri = EngineInfo.RuleHelpUri(finding.RuleId);
            var fix = string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextFix, finding.Fix);
            var markdown = "**" + Resources.Strings.LabelFix + ":** " + MarkdownText.Escape(finding.Fix) + "\n\n[" + Resources.Strings.SarifRuleReference + "](" + helpUri.AbsoluteUri + ")";
            var tags = new List<string>
            {
                finding.Brand is { } brand ? CanonicalNames.ToName(brand) : "general",
                WireNames.Of(finding.Confidence),
            };
            return new Descriptor(finding.RuleId, finding.RuleId, finding.RuleId, finding.RuleId, fix, markdown, helpUri, finding.Severity, tags);
        }

        public static Descriptor Truncated()
        {
            var helpUri = new Uri(EngineInfo.DocsUrl + "articles/output-formats.html");
            var text = Resources.Strings.SarifTruncatedDescription + "\n\n" + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextFix, Resources.Strings.SarifTruncatedFix);
            var markdown = MarkdownText.Escape(Resources.Strings.SarifTruncatedDescription) + "\n\n**" + Resources.Strings.LabelFix + ":** " + MarkdownText.Escape(Resources.Strings.SarifTruncatedFix);
            return new Descriptor(TruncatedRuleId, Resources.Strings.SarifTruncatedTitle, Resources.Strings.SarifTruncatedTitle, Resources.Strings.SarifTruncatedDescription, text, markdown, helpUri, Severity.Information, ["general", WireNames.Of(Confidence.Policy)]);
        }

        public void Write(Utf8JsonWriter json)
        {
            json.WriteStartObject();
            json.WriteString("id", Id);
            json.WriteString("name", MarkdownText.Clip(Name, MaxNameLength));
            json.WriteStartObject("shortDescription");
            json.WriteString("text", MarkdownText.Clip(ShortDescription, MaxDescriptionLength));
            json.WriteEndObject();
            json.WriteStartObject("fullDescription");
            json.WriteString("text", MarkdownText.Clip(FullDescription, MaxDescriptionLength));
            json.WriteEndObject();
            json.WriteStartObject("help");
            json.WriteString("text", HelpText);
            json.WriteString("markdown", HelpMarkdown);
            json.WriteEndObject();
            json.WriteString("helpUri", HelpUri.AbsoluteUri);
            json.WriteStartObject("defaultConfiguration");
            json.WriteString("level", Level(DefaultSeverity));
            json.WriteEndObject();
            json.WriteStartObject("properties");
            json.WriteStartArray("tags");
            foreach (var tag in Tags)
            {
                json.WriteStringValue(tag);
            }

            json.WriteEndArray();
            json.WriteEndObject();
            json.WriteEndObject();
        }
    }
}
