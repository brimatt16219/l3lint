using System.ComponentModel;
using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Schemas;
using L3Lint.Reporting.Rules;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace L3Lint.Mcp.Content;

/// <summary>
/// Resource templates (Interfaces tab, "Resources and prompts"): rule reference pages, JSON Schemas and one
/// passing sample payload per brand.
/// </summary>
[McpServerResourceType]
internal sealed class RuleResources(ILinter linter)
{
    [McpServerResource(UriTemplate = "l3lint://rules/{ruleId}", Name = "rule", Title = "Rule reference page", MimeType = "text/markdown")]
    [Description("The reference page of one rule as Markdown: requirement, rationale, check, sources and examples.")]
    public string Rule([Description("The rule ID, for example VISA-L3-004.")] string ruleId) =>
        linter.TryGetRule(ruleId, out var rule)
            ? RuleMarkdown.Render(rule)
            : throw new McpException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownRule, ruleId));

    [McpServerResource(UriTemplate = "l3lint://schemas/{name}", Name = "schema", Title = "JSON Schema", MimeType = "application/schema+json")]
    [Description("One of l3lint's JSON Schemas: canonical, results, config, rulepack, csvmap, rates, profile or lock.")]
    public static string Schema([Description("The schema name.")] string name) =>
        SchemaCatalog.TryGetText(name, out var text)
            ? text
            : throw new McpException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownSchema, name, string.Join(", ", SchemaCatalog.Names)));

    [McpServerResource(UriTemplate = "l3lint://examples/{brand}/{name}", Name = "example", Title = "Sample passing payload", MimeType = "application/json")]
    [Description("A canonical payload that meets every documented Level 3 requirement of a brand, for example l3lint://examples/visa/level3. Brands: visa, mastercard, amex, discover. Name: level3.")]
    public static string Example(
        [Description("visa, mastercard, amex or discover.")] string brand,
        [Description("The sample name: level3.")] string name) =>
        Samples.TryGet(brand, name, out var text)
            ? text
            : throw new McpException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorUnknownExample, brand, name, string.Join(", ", Samples.Names)));
}
