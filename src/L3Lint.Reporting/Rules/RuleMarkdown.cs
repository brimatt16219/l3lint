using System.Globalization;
using System.Text;
using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Reporting.Rules;

/// <summary>
/// Renders a rule's reference page as Markdown (FR-036). The docs generator writes these pages from the packs, and
/// the MCP server serves them as <c>l3lint://rules/{ruleId}</c>, so both always match the loaded pack.
/// </summary>
public static class RuleMarkdown
{
    /// <summary>
    /// Renders the page.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <returns>Markdown with LF line endings.</returns>
    public static string Render(RuleMetadata rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var page = new StringBuilder();
        page.Append("# ").Append(rule.Id).Append(": ").Append(Escape(rule.Title)).Append("\n\n");
        page.Append("| ").Append(Resources.Strings.MdProperty).Append(" | ").Append(Resources.Strings.MdValue).Append(" |\n| --- | --- |\n");
        Row(page, Resources.Strings.LabelSeverity, WireNames.Of(rule.Severity));
        Row(page, Resources.Strings.LabelConfidence, WireNames.Of(rule.Confidence));
        Row(page, Resources.Strings.LabelLevel, WireNames.Of(rule.Level));
        Row(page, Resources.Strings.LabelBrand, RuleText.Brand(rule));
        Row(page, Resources.Strings.LabelImpact, WireNames.Of(rule.Impact));
        Row(page, Resources.Strings.LabelEffective, RuleText.Effective(rule));
        Row(page, Resources.Strings.LabelPack, rule.PackId + " " + rule.PackVersion);
        page.Append('\n');

        Section(page, Resources.Strings.LabelRequirement, rule.Requirement ?? rule.Title);
        if (rule.Rationale is { } rationale)
        {
            Section(page, Resources.Strings.LabelWhy, rationale);
        }

        Section(page, Resources.Strings.LabelCheck, "`" + RuleText.Check(rule) + "`");
        var applies = RuleText.AppliesTo(rule).ToList();
        if (applies.Count > 0)
        {
            page.Append("## ").Append(Resources.Strings.LabelAppliesTo).Append("\n\n");
            foreach (var line in applies)
            {
                page.Append("- ").Append(Escape(line)).Append('\n');
            }

            page.Append('\n');
        }

        Section(page, Resources.Strings.LabelMessage, "`" + rule.Message + "`");
        Section(page, Resources.Strings.LabelFix, rule.Fix);
        page.Append("## ").Append(Resources.Strings.LabelSources).Append("\n\n");
        foreach (var source in rule.Sources)
        {
            page.Append("- <").Append(source.AbsoluteUri).Append(">\n");
        }

        page.Append('\n');
        if (rule.Supersedes.Count > 0)
        {
            Section(page, Resources.Strings.LabelSupersedes, string.Join(", ", rule.Supersedes));
        }

        if (rule.Notes is { } notes)
        {
            Section(page, Resources.Strings.LabelNotes, notes);
        }

        if (rule.Examples is { } examples)
        {
            page.Append("## ").Append(Resources.Strings.LabelExamples).Append("\n\n");
            Example(page, Resources.Strings.LabelPassing, examples.PassNote, examples.PassJson);
            Example(page, Resources.Strings.LabelFailing, examples.FailNote, examples.FailJson);
            if (examples.Context is { AsOf: not null } or { Processor: not null } or { StrictProfiles: true } or { RatesYaml: not null })
            {
                var context = examples.Context;
                var parts = new List<string>();
                if (context.AsOf is { } asOf)
                {
                    parts.Add("--as-of " + RuleText.Date(asOf));
                }

                if (context.Processor is { } processor)
                {
                    parts.Add("--processor " + processor);
                }

                if (context.StrictProfiles)
                {
                    parts.Add("--strict-profiles");
                }

                if (context.RatesYaml is not null)
                {
                    parts.Add("--rates (example rate table)");
                }

                page.Append(string.Format(CultureInfo.InvariantCulture, Resources.Strings.MdExampleContext, string.Join(" ", parts))).Append("\n\n");
            }
        }

        return page.ToString().TrimEnd('\n') + "\n";
    }

    private static void Row(StringBuilder page, string name, string value) =>
        page.Append("| ").Append(name).Append(" | ").Append(Escape(value)).Append(" |\n");

    private static void Section(StringBuilder page, string heading, string body) =>
        page.Append("## ").Append(heading).Append("\n\n").Append(body).Append("\n\n");

    private static void Example(StringBuilder page, string heading, string? note, string json)
    {
        page.Append("### ").Append(heading).Append("\n\n");
        if (note is not null)
        {
            page.Append(Escape(note)).Append("\n\n");
        }

        page.Append("```json\n").Append(json.TrimEnd('\n')).Append("\n```\n\n");
    }

    private static string Escape(string text) => text.Replace("|", "\\|", StringComparison.Ordinal);
}
