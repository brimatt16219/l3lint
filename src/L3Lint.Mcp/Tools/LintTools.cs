using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Hosting;
using L3Lint.Hosting.Fixes;
using L3Lint.Reporting.Fixes;
using L3Lint.Reporting.Json;
using L3Lint.Reporting.Mapping;
using L3Lint.Reporting.Rules;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace L3Lint.Mcp.Tools;

/// <summary>
/// The six read-only tools (Interfaces tab, "MCP server", FR-029, US-08). Each call is independent: the server
/// keeps nothing after it returns, and payload values never reach the log.
/// </summary>
[McpServerToolType]
internal sealed class LintTools(ILinter linter, ServerSettings settings)
{
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [McpServerTool(Name = "lint_payload", Title = "Lint a payload", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("Lint a commercial card payment payload against Visa, Mastercard, American Express and Discover Level 2 and Level 3 rules. Returns findings with the JSON path, the fix, the source and the likely interchange tier. Findings are advisory: they say a payload is likely to downgrade or meets the documented requirement, and never promise an interchange rate.")]
    public CallToolResult LintPayload(
        [Description("The payment request body, as a JSON object or a string holding JSON, at most 2 MB.")] JsonElement payload,
        [Description("Input format: canonical, stripe, adyen, braintree, cybersource, authorizenet or checkoutcom. Omit to detect it.")] string? format = null,
        [Description("Brand, card product, MCC, merchant country, processor profile and as-of date. Values here beat the payload.")] LintContext? context = null,
        [Description("Lowest severity that counts as failing: error, warning, information or none. Default: error.")] string? failOn = null,
        [Description("Most findings to return. The summary still counts every finding.")] int? maxFindings = null) =>
        ToolResults.Run(() =>
        {
            var options = PayloadReader.Options(format, context, failOn, settings.WorkingDirectory);
            var result = linter.Lint(PayloadReader.Read(payload), options);
            var packs = linter.RulePacks.ToDictionary(p => p.Id, p => p.Version, StringComparer.Ordinal);
            var report = SingleReport.From(result, options, maxFindings, packs);
            return ToolResults.Success(Summarize(report), JsonReportWriter.Render(report));
        });

    [McpServerTool(Name = "explain_rule", Title = "Explain a rule", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("Explain one l3lint rule: requirement, rationale, severity, confidence, effective dates, sources, and a passing and a failing example payload.")]
    public CallToolResult ExplainRule([Description("The rule ID, for example VISA-L3-004.")] string ruleId) =>
        ToolResults.Run(() =>
        {
            if (!linter.TryGetRule(ruleId, out var rule))
            {
                return ToolResults.Error(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownRule, ruleId));
            }

            var summary = rule.Id + ": " + rule.Title + ". " + rule.Requirement;
            return ToolResults.Success(summary, RuleJson.Render(rule, includeExamples: true));
        });

    [McpServerTool(Name = "list_rules", Title = "List rules", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("List l3lint rules with their IDs, titles and severities, optionally filtered by brand, level or text.")]
    public CallToolResult ListRules(
        [Description("Only rules for this brand: general, visa, mastercard, amex or discover.")] string? brand = null,
        [Description("Only rules at this level: general, L2 or L3.")] string? level = null,
        [Description("Only rules whose ID, title or requirement contains this text.")] string? query = null) =>
        ToolResults.Run(() =>
        {
            var rules = linter.Rules(new RuleQuery { Brand = ParseBrand(brand), Level = ParseLevel(level), Text = query });
            var json = "{\"rules\":" + RuleJson.RenderList(rules) + "}";
            var summary = string.Format(CultureInfo.InvariantCulture, Resources.Strings.SummaryRules, rules.Count, string.Join(", ", rules.Take(20).Select(r => r.Id)));
            return ToolResults.Success(summary, json);
        });

    [McpServerTool(Name = "map_payload", Title = "Map to canonical JSON", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("Map a gateway payload (Stripe, Adyen, Braintree, Cybersource, Authorize.net, Checkout.com) to l3lint's canonical JSON, with the path map from canonical fields to source fields, derived fields and adapter notes. Card numbers are masked first.")]
    public CallToolResult MapPayload(
        [Description("The payment request body, as a JSON object or a string holding JSON, at most 2 MB.")] JsonElement payload,
        [Description("Input format; omit to detect it.")] string? format = null) =>
        ToolResults.Run(() =>
        {
            var options = PayloadReader.Options(format, null, null, settings.WorkingDirectory);
            var mapped = linter.Map(PayloadReader.Read(payload), options);
            var summary = string.Format(CultureInfo.InvariantCulture, Resources.Strings.SummaryMap, mapped.Format, mapped.Transactions.Count, mapped.CardNumbers.Count);
            return ToolResults.Success(summary, MappingJson.Render(mapped, indented: false));
        });

    [McpServerTool(Name = "suggest_fixes", Title = "Suggest fixes", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("Suggest mechanical fixes as an RFC 6902 JSON Patch in the payload's own format (recomputed line totals, normalized unit codes, trimmed lengths, explicit indicators), plus the findings a person must decide. Descriptions, commodity codes and tax amounts are never invented, and the transaction amount is never changed.")]
    public CallToolResult SuggestFixes(
        [Description("The payment request body, as a JSON object or a string holding JSON, at most 2 MB.")] JsonElement payload,
        [Description("Input format; omit to detect it.")] string? format = null,
        [Description("Brand, card product, MCC, merchant country, processor profile and as-of date.")] LintContext? context = null) =>
        ToolResults.Run(() =>
        {
            var options = PayloadReader.Options(format, context, null, settings.WorkingDirectory);
            var result = linter.Lint(PayloadReader.Read(payload), options);
            var plans = FixPlanner.Plan(result, includeAmount: false);
            var operations = plans.SelectMany(p => p.Operations).ToList();
            var needsHuman = plans.SelectMany(p => p.NeedsHuman).ToList();
            var summary = string.Format(CultureInfo.InvariantCulture, Resources.Strings.SummaryFixes, operations.Count, needsHuman.Count);
            return ToolResults.Success(summary, FixJson(operations, plans.Any(p => p.TargetsCanonical), needsHuman));
        });

    [McpServerTool(Name = "get_schema", Title = "Get a JSON Schema", ReadOnly = true, Idempotent = true, OpenWorld = false, Destructive = false)]
    [Description("Get one of l3lint's JSON Schemas: canonical (the payload model), results (lint output), config (.l3lint.yaml) or rulepack, and also csvmap, rates, profile and lock.")]
    public static CallToolResult GetSchema([Description("The schema name: canonical, results, config or rulepack.")] string name) =>
        ToolResults.Run(() => SchemaCatalog.TryGetText(name, out var text)
            ? ToolResults.Success(string.Format(CultureInfo.InvariantCulture, Resources.Strings.SummarySchema, name), text)
            : ToolResults.Error(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownSchema, name, string.Join(", ", SchemaCatalog.Names))));

    private static string Summarize(Reporting.LintReport report)
    {
        var builder = new StringBuilder();
        builder.Append(Reporting.Text.TextReportWriter.SummaryLine(report));
        foreach (var transaction in report.Transactions)
        {
            foreach (var tier in transaction.Tiers)
            {
                builder.Append(' ').Append(string.Format(CultureInfo.InvariantCulture, Resources.Strings.SummaryTier, CanonicalNames.ToName(tier.Brand), Reporting.TierNames.Describe(tier)));
            }

            foreach (var finding in transaction.Findings.Where(f => f.Severity == Severity.Error))
            {
                builder.Append('\n').Append(finding.RuleId).Append(" at ").Append(finding.Path.Source ?? finding.Path.Canonical).Append(": ").Append(finding.Message);
            }
        }

        foreach (var error in report.InputErrors)
        {
            builder.Append('\n').Append(error.Message);
        }

        return builder.ToString();
    }

    private static string FixJson(IReadOnlyList<Core.Fixes.PatchOperation> operations, bool targetsCanonical, IReadOnlyList<Finding> needsHuman)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteStartArray("patch");
            PatchJson.WriteOperations(json, operations);
            json.WriteEndArray();
            json.WriteBoolean("targetsCanonical", targetsCanonical);
            json.WriteStartArray("needsHuman");
            foreach (var finding in needsHuman)
            {
                json.WriteStartObject();
                json.WriteString("ruleId", finding.RuleId);
                json.WriteString("severity", WireNames.Of(finding.Severity));
                json.WriteString("path", finding.Path.Source ?? finding.Path.Canonical);
                json.WriteString("message", finding.Message);
                json.WriteString("fix", finding.Fix);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static CardBrand? ParseBrand(string? text) => text?.ToLowerInvariant() switch
    {
        null or "" => null,
        "general" => CardBrand.Unknown,
        var name when CanonicalNames.TryParse(name, out CardBrand brand) && brand != CardBrand.Unknown => brand,
        var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorBrand, other)),
    };

    private static RuleLevel? ParseLevel(string? text) => text?.ToUpperInvariant() switch
    {
        null or "" => null,
        "GENERAL" => RuleLevel.General,
        "L2" => RuleLevel.L2,
        "L3" => RuleLevel.L3,
        var other => throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorLevel, other)),
    };
}
