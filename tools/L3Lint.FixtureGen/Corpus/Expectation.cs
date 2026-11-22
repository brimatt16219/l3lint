using System.Globalization;
using System.Text;

namespace L3Lint.FixtureGen.Corpus;

/// <summary>One finding a fixture must produce. Severity is stated only when the pack and ADR-0016 fix it.</summary>
/// <param name="RuleId">The rule ID.</param>
/// <param name="Path">The canonical JSON Pointer.</param>
/// <param name="Severity">The expected severity, or <see langword="null"/> to accept any.</param>
internal sealed record ExpectedFinding(string RuleId, string Path, string? Severity = null);

/// <summary>The context a fixture states explicitly (Test and delivery tab: "Every fixture states its context explicitly").</summary>
/// <remarks>
/// <c>Only</c> lints with one rule selected, as <c>l3lint rules test</c> does for rule examples, so a rule that another
/// rule supersedes on the same field can still be shown to fire. <c>Copies</c> lints the payload that many times in one
/// batch, for batch rules such as GEN-DQ-007.
/// </remarks>
internal sealed record FixtureContext(string? Brand, string? CardProduct, string? Mcc, string AsOf, string? Processor = null, string? Rates = null, string? Only = null, int? Copies = null);

/// <summary>A fixture's <c>*.expect.yaml</c>, in the format of the Test and delivery tab.</summary>
internal sealed record Expectation(
    string Description,
    FixtureContext Context,
    int? ExitCode,
    IReadOnlyList<ExpectedFinding> Findings,
    IReadOnlyList<string> Absent,
    (string Brand, string Status)? LikelyTier = null,
    string? Operator = null)
{
    public string ToYaml()
    {
        var yaml = new StringBuilder();
        yaml.Append("description: ").Append(Quote(Description)).Append('\n');
        if (Operator is not null)
        {
            yaml.Append("operator: ").Append(Operator).Append('\n');
        }

        yaml.Append("context: { ");
        var parts = new List<string>();
        Add(parts, "brand", Context.Brand);
        Add(parts, "cardProduct", Context.CardProduct);
        Add(parts, "mcc", Context.Mcc is null ? null : "\"" + Context.Mcc + "\"");
        Add(parts, "asOf", Context.AsOf);
        Add(parts, "processor", Context.Processor);
        Add(parts, "rates", Context.Rates);
        Add(parts, "only", Context.Only);
        Add(parts, "copies", Context.Copies?.ToString(CultureInfo.InvariantCulture));
        yaml.Append(string.Join(", ", parts)).Append(" }\n");
        yaml.Append("expect:\n");
        if (ExitCode is { } code)
        {
            yaml.Append("  exitCode: ").Append(code.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        if (Findings.Count == 0)
        {
            yaml.Append("  findings: []\n");
        }
        else
        {
            yaml.Append("  findings:\n");
            foreach (var finding in Findings)
            {
                yaml.Append("    - { ruleId: ").Append(finding.RuleId).Append(", path: ").Append(finding.Path is "" or "*" ? "\"" + finding.Path + "\"" : finding.Path);
                if (finding.Severity is not null)
                {
                    yaml.Append(", severity: ").Append(finding.Severity);
                }

                yaml.Append(" }\n");
            }
        }

        if (Absent.Count > 0)
        {
            yaml.Append("  absent: [").Append(string.Join(", ", Absent)).Append("]\n");
        }

        if (LikelyTier is { } tier)
        {
            yaml.Append("  likelyTier: { brand: ").Append(tier.Brand).Append(", status: ").Append(tier.Status).Append(" }\n");
        }

        return yaml.ToString();
    }

    private static void Add(List<string> parts, string name, string? value)
    {
        if (value is not null)
        {
            parts.Add(name + ": " + value);
        }
    }

    private static string Quote(string text) => "\"" + text.Replace("\"", "'", StringComparison.Ordinal) + "\"";
}
