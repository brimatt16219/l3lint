using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Reporting.Rules;

/// <summary>
/// Writes a rule's metadata as JSON, for <c>explain --output json</c>, <c>rules list --output json</c> and the
/// MCP <c>explain_rule</c> tool.
/// </summary>
public static class RuleJson
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Renders one rule.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="includeExamples">Whether to include the example payloads.</param>
    /// <returns>Indented JSON.</returns>
    public static string Render(RuleMetadata rule, bool includeExamples)
    {
        ArgumentNullException.ThrowIfNull(rule);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, Options))
        {
            Write(json, rule, includeExamples);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Renders a list of rules with their headline fields.
    /// </summary>
    /// <param name="rules">The rules.</param>
    /// <returns>An indented JSON array.</returns>
    public static string RenderList(IEnumerable<RuleMetadata> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, Options))
        {
            json.WriteStartArray();
            foreach (var rule in rules)
            {
                json.WriteStartObject();
                json.WriteString("id", rule.Id);
                json.WriteString("title", rule.Title);
                json.WriteString("severity", WireNames.Of(rule.Severity));
                json.WriteString("level", WireNames.Of(rule.Level));
                json.WriteString("brand", RuleText.Brand(rule));
                json.WriteString("confidence", WireNames.Of(rule.Confidence));
                json.WriteString("effective", RuleText.Effective(rule));
                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Writes one rule.
    /// </summary>
    /// <param name="json">The writer.</param>
    /// <param name="rule">The rule.</param>
    /// <param name="includeExamples">Whether to include the example payloads.</param>
    public static void Write(Utf8JsonWriter json, RuleMetadata rule, bool includeExamples)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(rule);
        json.WriteStartObject();
        json.WriteString("id", rule.Id);
        json.WriteString("title", rule.Title);
        json.WriteString("requirement", rule.Requirement ?? rule.Title);
        Nullable(json, "rationale", rule.Rationale);
        Nullable(json, "notes", rule.Notes);
        json.WriteString("pack", rule.PackId);
        json.WriteString("packVersion", rule.PackVersion);
        json.WriteString("brand", RuleText.Brand(rule));
        json.WriteString("level", WireNames.Of(rule.Level));
        json.WriteString("severity", WireNames.Of(rule.Severity));
        json.WriteString("confidence", WireNames.Of(rule.Confidence));
        json.WriteString("impact", WireNames.Of(rule.Impact));
        json.WriteStartObject("effective");
        Nullable(json, "from", rule.EffectiveFrom is { } from ? RuleText.Date(from) : null);
        Nullable(json, "until", rule.EffectiveUntil is { } until ? RuleText.Date(until) : null);
        json.WriteString("text", RuleText.Effective(rule));
        json.WriteEndObject();
        json.WriteStartArray("appliesTo");
        foreach (var line in RuleText.AppliesTo(rule))
        {
            json.WriteStringValue(line);
        }

        json.WriteEndArray();
        json.WriteString("check", RuleText.Check(rule));
        json.WriteString("message", rule.Message);
        json.WriteString("fix", rule.Fix);
        json.WriteStartArray("sources");
        foreach (var source in rule.Sources)
        {
            json.WriteStringValue(source.AbsoluteUri);
        }

        json.WriteEndArray();
        json.WriteString("helpUri", EngineInfo.RuleHelpUri(rule.Id).AbsoluteUri);
        if (includeExamples && rule.Examples is { } examples)
        {
            json.WriteStartObject("examples");
            Example(json, "pass", examples.PassFile, examples.PassNote, examples.PassJson);
            Example(json, "fail", examples.FailFile, examples.FailNote, examples.FailJson);
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void Example(Utf8JsonWriter json, string name, string file, string? note, string payload)
    {
        json.WriteStartObject(name);
        json.WriteString("file", file);
        Nullable(json, "note", note);
        json.WritePropertyName("payload");
        using (var document = JsonDocument.Parse(payload))
        {
            document.RootElement.WriteTo(json);
        }

        json.WriteEndObject();
    }

    private static void Nullable(Utf8JsonWriter json, string name, string? value)
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
}
