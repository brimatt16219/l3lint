using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using Json.Schema;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Validators for the three machine formats: the OASIS SARIF 2.1.0 schema and the Jenkins JUnit XSD vendored under
/// Schemas/, and the repository's own results.schema.json.
/// </summary>
internal static class Schemas
{
    private const string Draft04 = "http://json-schema.org/draft-04/schema#";
    private const string Draft07 = "http://json-schema.org/draft-07/schema#";

    private static readonly Lazy<JsonSchema> Sarif = new(() =>
    {
        // The OASIS file declares draft-04, which JsonSchema.Net 8 does not implement. It uses only local $refs and
        // no draft-04-only keyword forms (boolean exclusiveMinimum or exclusiveMaximum), so draft-07 rules give the
        // same verdicts. The vendored file stays byte-for-byte as published; only the loaded copy changes (ADR-0055).
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", "sarif-schema-2.1.0.json"));
        text.ShouldContain("\"$schema\": \"" + Draft04 + "\"");
        text.ShouldNotContain("\"exclusiveMinimum\": true");
        text.ShouldNotContain("\"exclusiveMaximum\": true");
        var draft07 = text.Replace(Draft04, Draft07, StringComparison.Ordinal);
        return JsonSchema.FromText(draft07, new BuildOptions { SchemaRegistry = new SchemaRegistry() });
    });

    private static readonly Lazy<JsonSchema> Results = new(() =>
        JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", "results.schema.json")), new BuildOptions { SchemaRegistry = new SchemaRegistry() }));

    private static readonly Lazy<XmlSchemaSet> Junit = new(() =>
    {
        var set = new XmlSchemaSet();
        using var reader = XmlReader.Create(Path.Combine(AppContext.BaseDirectory, "Schemas", "junit-10.xsd"));
        set.Add(null, reader);
        set.Compile();
        return set;
    });

    /// <summary>Validates SARIF text, checking formats such as uri and uri-reference too.</summary>
    public static IReadOnlyList<string> ValidateSarif(string sarif) => Evaluate(Sarif.Value, sarif, requireFormats: true);

    /// <summary>Validates JSON output against results.schema.json.</summary>
    public static IReadOnlyList<string> ValidateResults(string json) => Evaluate(Results.Value, json, requireFormats: false);

    /// <summary>Validates JUnit XML against the vendored XSD and returns every error and warning.</summary>
    public static IReadOnlyList<string> ValidateJunit(string xml)
    {
        var problems = new List<string>();
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = Junit.Value,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
        };
        settings.ValidationEventHandler += (_, e) => problems.Add($"{e.Severity} line {e.Exception.LineNumber}: {e.Message}");
        using var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(xml)), settings);
        while (reader.Read())
        {
        }

        return problems;
    }

    private static List<string> Evaluate(JsonSchema schema, string json, bool requireFormats)
    {
        using var document = JsonDocument.Parse(json);
        var results = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = requireFormats });
        if (results.IsValid)
        {
            return [];
        }

        var problems = (results.Details ?? [])
            .Where(d => !d.IsValid && d.Errors is not null)
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation} {e.Key}: {e.Value}"))
            .ToList();
        return problems.Count > 0 ? problems : ["invalid"];
    }
}
