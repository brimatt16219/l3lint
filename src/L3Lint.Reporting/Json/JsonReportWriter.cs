using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Estimates;
using L3Lint.Core.Fixes;
using L3Lint.Core.Model;
using L3Lint.Core.Tiers;

namespace L3Lint.Reporting.Json;

/// <summary>
/// The machine contract (FR-022): JSON that validates against <c>results.schema.json</c>, with engine and pack
/// versions. Property order is fixed and output has no timestamps, so it is byte-identical for identical runs (NFR-001).
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <inheritdoc/>
    public string Format => "json";

    /// <summary>Bytes the JSON writer may hold before they are passed on to the output.</summary>
    private const int FlushThreshold = 64 * 1024;

    /// <inheritdoc/>
    /// <remarks>The report streams to <paramref name="output"/> as it is written, so a large batch is never held as one string (NFR-003).</remarks>
    public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        WriteTo(report, output);
        output.Write('\n');
    }

    /// <summary>
    /// Renders the report as a JSON string.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>Indented JSON with LF line endings.</returns>
    public static string Render(LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        using var output = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
        WriteTo(report, output);
        return output.ToString();
    }

    private static void WriteTo(LintReport report, TextWriter output)
    {
        using var stream = new Utf8TextWriterStream(output);
        using (var json = new Utf8JsonWriter(stream, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteString("schemaVersion", "1.0");
            json.WriteStartObject("tool");
            json.WriteString("name", "l3lint");
            json.WriteString("version", report.ToolVersion);
            json.WriteString("informationUri", EngineInfo.ProjectUrl);
            json.WriteEndObject();
            json.WriteStartObject("rulePacks");
            foreach (var (id, version) in report.RulePacks)
            {
                json.WriteString(id, version);
            }

            json.WriteEndObject();
            WriteDate(json, "asOf", report.AsOf);
            json.WriteString("profile", report.Profile);
            json.WriteStartArray("inputs");
            foreach (var input in report.Inputs)
            {
                json.WriteStartObject();
                json.WriteString("path", input.Path);
                json.WriteString("format", input.Format);
                json.WriteString("sha256", input.Sha256);
                json.WriteNumber("transactions", input.Transactions);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            if (report.InputErrors.Count > 0)
            {
                WriteInputErrors(json, report.InputErrors);
            }

            json.WriteStartArray("transactions");
            foreach (var transaction in report.Transactions)
            {
                WriteTransaction(json, transaction);
                if (json.BytesPending > FlushThreshold)
                {
                    json.Flush();
                }
            }

            json.WriteEndArray();
            if (report.BatchFindings.Count > 0)
            {
                json.WriteStartArray("batchFindings");
                foreach (var finding in report.BatchFindings)
                {
                    WriteFinding(json, finding, includeIndex: true);
                }

                json.WriteEndArray();
            }

            WriteSummary(json, report);
            if (report.Notes.Count > 0)
            {
                json.WriteStartArray("notes");
                foreach (var note in report.Notes)
                {
                    json.WriteStringValue(note);
                }

                json.WriteEndArray();
            }

            json.WriteNumber("exitCode", report.ExitCode);
            json.WriteEndObject();
        }
    }

    private static void WriteInputErrors(Utf8JsonWriter json, IReadOnlyList<InputError> errors)
    {
        json.WriteStartArray("inputErrors");
        foreach (var error in errors)
        {
            json.WriteStartObject();
            json.WriteString("path", error.SourceName);
            if (error.Line is { } line)
            {
                json.WriteNumber("line", line);
            }
            else
            {
                json.WriteNull("line");
            }

            json.WriteString("message", error.Message);
            if (error.Candidates is { Count: > 0 } candidates)
            {
                json.WriteStartArray("candidates");
                foreach (var candidate in candidates)
                {
                    json.WriteStringValue(candidate);
                }

                json.WriteEndArray();
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    private static void WriteTransaction(Utf8JsonWriter json, TransactionResult transaction)
    {
        json.WriteStartObject();
        json.WriteNumber("index", transaction.Index);
        json.WriteNumber("input", transaction.Input);
        WriteNullable(json, "id", transaction.Id);
        json.WriteString("brand", CanonicalNames.ToName(transaction.Brand));
        if (transaction.Brands.Count > 0)
        {
            json.WriteStartArray("brands");
            foreach (var brand in transaction.Brands)
            {
                json.WriteStringValue(CanonicalNames.ToName(brand));
            }

            json.WriteEndArray();
        }

        WriteNullable(json, "cardProduct", transaction.CardProduct is { } product ? CanonicalNames.ToName(product) : null);
        WriteNullable(json, "mcc", transaction.Mcc);
        if (transaction.Format is { } format)
        {
            json.WriteString("format", format);
        }

        json.WriteString("asOf", transaction.AsOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (transaction.PrimaryTier is { } primary)
        {
            json.WritePropertyName("likelyTier");
            WriteTier(json, primary, includeBrand: false);
        }
        else
        {
            json.WriteNull("likelyTier");
        }

        if (transaction.Tiers.Count > 1)
        {
            json.WriteStartArray("likelyTiers");
            foreach (var tier in transaction.Tiers)
            {
                WriteTier(json, tier, includeBrand: true);
            }

            json.WriteEndArray();
        }

        if (transaction.CostEstimate is { } estimate)
        {
            WriteEstimate(json, estimate);
        }

        if (transaction.Notes.Count > 0)
        {
            json.WriteStartArray("notes");
            foreach (var note in transaction.Notes)
            {
                json.WriteStringValue(note);
            }

            json.WriteEndArray();
        }

        json.WriteStartArray("findings");
        foreach (var finding in transaction.Findings)
        {
            WriteFinding(json, finding, includeIndex: false);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteTier(Utf8JsonWriter json, TierAssessment tier, bool includeBrand)
    {
        json.WriteStartObject();
        if (includeBrand)
        {
            json.WriteString("brand", CanonicalNames.ToName(tier.Brand));
        }

        json.WriteString("best", tier.Best);
        json.WriteString("likely", tier.Likely);
        json.WriteString("status", WireNames.Of(tier.Status));
        json.WriteNumber("blockingErrors", tier.BlockingErrors);
        json.WriteEndObject();
    }

    private static void WriteEstimate(Utf8JsonWriter json, CostEstimate estimate)
    {
        json.WriteStartObject("costEstimate");
        json.WriteString("label", "estimate");
        json.WriteString("rateTable", estimate.RateTableName);
        json.WriteString("effective", estimate.Effective.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        json.WriteString("currency", estimate.Currency);
        json.WriteStartArray("estimates");
        foreach (var line in estimate.Lines)
        {
            json.WriteStartObject();
            json.WriteString("brand", CanonicalNames.ToName(line.Brand));
            json.WriteString("likelyTier", line.LikelyTier);
            WriteNullableNumber(json, "likelyCost", line.LikelyCost);
            json.WriteString("bestTier", line.BestTier);
            WriteNullableNumber(json, "bestCost", line.BestCost);
            WriteNullableNumber(json, "difference", line.Difference);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    /// <summary>Writes one finding in the results schema shape.</summary>
    internal static void WriteFinding(Utf8JsonWriter json, Finding finding, bool includeIndex)
    {
        json.WriteStartObject();
        json.WriteString("ruleId", finding.RuleId);
        json.WriteString("severity", WireNames.Of(finding.Severity));
        json.WriteString("impact", WireNames.Of(finding.Impact));
        if (finding.Brand is { } brand)
        {
            json.WriteString("brand", CanonicalNames.ToName(brand));
        }

        if (includeIndex)
        {
            json.WriteNumber("transactionIndex", finding.TransactionIndex);
        }

        json.WriteStartObject("path");
        json.WriteString("canonical", finding.Path.Canonical);
        WriteNullable(json, "source", finding.Path.Source);
        json.WriteEndObject();
        if (finding.Location is { } location)
        {
            json.WriteStartObject("location");
            json.WriteNumber("line", location.Line);
            json.WriteNumber("column", location.Column);
            json.WriteEndObject();
        }
        else
        {
            json.WriteNull("location");
        }

        json.WriteString("message", finding.Message);
        WriteNullable(json, "expected", finding.Expected);
        WriteNullable(json, "actual", finding.Actual);
        json.WriteString("fix", finding.Fix);
        WritePatch(json, finding.Patch);
        if (finding.NeedsHuman)
        {
            json.WriteBoolean("needsHuman", true);
        }

        json.WriteString("confidence", WireNames.Of(finding.Confidence));
        json.WriteStartArray("sources");
        foreach (var source in finding.Sources)
        {
            json.WriteStringValue(source.AbsoluteUri);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    /// <summary>Writes an RFC 6902 patch array, or null.</summary>
    internal static void WritePatch(Utf8JsonWriter json, FixPatch? patch)
    {
        if (patch is null)
        {
            json.WriteNull("patch");
            return;
        }

        json.WriteStartArray("patch");
        Fixes.PatchJson.WriteOperations(json, patch.Operations);
        json.WriteEndArray();
    }

    private static void WriteSummary(Utf8JsonWriter json, LintReport report)
    {
        var summary = report.Summary;
        json.WriteStartObject("summary");
        json.WriteNumber("transactions", summary.Transactions);
        json.WriteNumber("errors", summary.Errors);
        json.WriteNumber("warnings", summary.Warnings);
        json.WriteNumber("information", summary.Information);
        json.WriteStartObject("byRule");
        foreach (var (rule, count) in summary.ByRule)
        {
            json.WriteNumber(rule, count);
        }

        json.WriteEndObject();
        var byBrand = ByBrand(report);
        if (byBrand.Count > 0)
        {
            json.WriteStartObject("byBrand");
            foreach (var (brand, counts) in byBrand)
            {
                json.WriteStartObject(brand);
                json.WriteNumber("errors", counts.Errors);
                json.WriteNumber("warnings", counts.Warnings);
                json.WriteNumber("information", counts.Information);
                json.WriteEndObject();
            }

            json.WriteEndObject();
        }

        if (report.FindingsOmitted > 0)
        {
            json.WriteNumber("findingsShown", summary.Errors + summary.Warnings + summary.Information - report.FindingsOmitted);
            json.WriteNumber("findingsOmitted", report.FindingsOmitted);
        }

        json.WriteEndObject();
    }

    /// <summary>Counts findings per brand; general findings count under the transaction's own brand.</summary>
    internal static SortedDictionary<string, (int Errors, int Warnings, int Information)> ByBrand(LintReport report)
    {
        var counts = new SortedDictionary<string, (int Errors, int Warnings, int Information)>(StringComparer.Ordinal);
        foreach (var transaction in report.Transactions)
        {
            foreach (var finding in transaction.Findings)
            {
                var key = CanonicalNames.ToName(finding.Brand ?? transaction.Brand);
                var current = counts.GetValueOrDefault(key);
                counts[key] = finding.Severity switch
                {
                    Severity.Error => (current.Errors + 1, current.Warnings, current.Information),
                    Severity.Warning => (current.Errors, current.Warnings + 1, current.Information),
                    _ => (current.Errors, current.Warnings, current.Information + 1),
                };
            }
        }

        return counts;
    }

    private static void WriteDate(Utf8JsonWriter json, string name, DateOnly? date)
    {
        if (date is { } value)
        {
            json.WriteString(name, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        else
        {
            json.WriteNull(name);
        }
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

    private static void WriteNullableNumber(Utf8JsonWriter json, string name, decimal? value)
    {
        if (value is { } number)
        {
            json.WriteNumber(name, number);
        }
        else
        {
            json.WriteNull(name);
        }
    }
}
