using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Model;

namespace L3Lint.Reporting.Text;

/// <summary>
/// The human-readable format (FR-021): one block per transaction with findings grouped by severity, the likely
/// tier, then a one-line summary. Severity words always print, so color is never the only signal (NFR-010).
/// </summary>
public sealed class TextReportWriter : IReportWriter
{
    private const string Indent = "  ";
    private const string Detail = "           ";

    /// <inheritdoc/>
    public string Format => "text";

    /// <inheritdoc/>
    public void Write(LintReport report, TextWriter output, ReportWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(options);
        var palette = new Palette(options.Color);
        if (!options.Quiet)
        {
            foreach (var note in report.Notes)
            {
                output.WriteLine(Resources.Strings.TextNote, note);
            }

            foreach (var transaction in report.Transactions)
            {
                WriteTransaction(transaction, output, options, palette);
            }

            if (report.BatchFindings.Count > 0)
            {
                output.WriteLine(Resources.Strings.TextBatchHeader);
                WriteFindings(report.BatchFindings, output, palette);
                output.WriteLine();
            }

            foreach (var error in report.InputErrors)
            {
                var where = error.Line is { } line ? error.SourceName + ":" + line.ToString(CultureInfo.InvariantCulture) : error.SourceName;
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextInputError, palette.Error(WireNames.Of(Severity.Error)), where, error.Message));
                if (error.Candidates is { Count: > 0 } candidates)
                {
                    output.WriteLine(Detail + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextCandidates, string.Join(", ", candidates)));
                }
            }

            if (report.FindingsOmitted > 0)
            {
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextOmitted, report.FindingsOmitted));
            }
        }

        output.WriteLine(SummaryLine(report));
    }

    /// <summary>Builds the one-line summary, for example "1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0."</summary>
    /// <param name="report">The report.</param>
    /// <returns>The summary line.</returns>
    public static string SummaryLine(LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var summary = report.Summary;
        var parts = new List<string>
        {
            Count(summary.Transactions, Resources.Strings.WordTransaction, Resources.Strings.WordTransactions),
            Count(summary.Errors, Resources.Strings.WordError, Resources.Strings.WordErrors),
            Count(summary.Warnings, Resources.Strings.WordWarning, Resources.Strings.WordWarnings),
        };
        if (summary.Information > 0)
        {
            parts.Add(Count(summary.Information, Resources.Strings.WordInformation, Resources.Strings.WordInformation));
        }

        if (report.InputErrors.Count > 0)
        {
            parts.Add(Count(report.InputErrors.Count, Resources.Strings.WordInputError, Resources.Strings.WordInputErrors));
        }

        var packs = string.Join(", ", UsedPacks(report).Select(p => p.Key + " " + p.Value));
        return string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextSummary, string.Join(", ", parts), packs);
    }

    private static void WriteTransaction(TransactionResult transaction, TextWriter output, ReportWriterOptions options, Palette palette)
    {
        output.WriteLine(Header(transaction));

        // Every override prints a one-line note so a reviewer can see why a rule ran (Interfaces tab, "Precedence").
        // Adapter notes reach this list only with --verbose.
        foreach (var note in transaction.Notes)
        {
            output.WriteLine(Indent + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextNote, note));
        }

        WriteFindings(transaction.Findings, output, palette);
        foreach (var tier in transaction.Tiers)
        {
            var label = transaction.Tiers.Count > 1
                ? string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextTierForBrand, CanonicalNames.ToName(tier.Brand))
                : Resources.Strings.TextTier;
            output.WriteLine(Indent + label + " " + TierNames.Describe(tier));
        }

        if (transaction.CostEstimate is { } estimate)
        {
            output.WriteLine(Indent + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextEstimateHeader, estimate.RateTableName, estimate.Effective.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            foreach (var line in estimate.Lines)
            {
                output.WriteLine(Indent + Indent + string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Strings.TextEstimateLine,
                    CanonicalNames.ToName(line.Brand),
                    line.LikelyTier,
                    Money(line.LikelyCost, estimate.Currency),
                    line.BestTier,
                    Money(line.BestCost, estimate.Currency),
                    Money(line.Difference, estimate.Currency)));
            }
        }

        output.WriteLine();
    }

    private static void WriteFindings(IEnumerable<Finding> findings, TextWriter output, Palette palette)
    {
        foreach (var finding in findings.OrderByDescending(f => f.Severity).ThenBy(f => f.TransactionIndex))
        {
            var severity = WireNames.Of(finding.Severity);
            var word = palette.Paint(finding.Severity, severity.PadRight(12));
            var path = finding.Path.Source ?? finding.Path.Canonical;
            output.WriteLine(Indent + word + finding.RuleId.PadRight(18) + " " + (path.Length == 0 ? "/" : path));
            output.WriteLine(Detail + finding.Message);
            output.WriteLine(Detail + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextFix, finding.Fix));
            if (finding.Sources.Count > 0)
            {
                output.WriteLine(Detail + string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextSource, finding.Sources[0].AbsoluteUri, WireNames.Of(finding.Confidence)));
            }
        }
    }

    private static string Header(TransactionResult transaction)
    {
        var parts = new List<string> { transaction.Id ?? string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextTransactionNumber, transaction.Index + 1) };
        if (transaction.Format is { } format)
        {
            parts.Add(format);
        }

        var brand = transaction.Brands.Count > 1 ? string.Join("+", transaction.Brands.Select(CanonicalNames.ToName)) : CanonicalNames.ToName(transaction.Brand);
        var product = transaction.CardProduct is { } p ? " " + CanonicalNames.ToName(p) : string.Empty;
        parts.Add(brand + product);
        if (transaction.Mcc is { } mcc)
        {
            parts.Add("MCC " + mcc);
        }

        parts.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextAsOf, transaction.AsOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        return string.Join("  ", parts);
    }

    private static IEnumerable<KeyValuePair<string, string>> UsedPacks(LintReport report)
    {
        var brands = report.Transactions.SelectMany(t => t.Brands).ToHashSet();
        foreach (var (id, version) in report.RulePacks)
        {
            var scope = PackScope(id);
            if (scope == "GEN" || brands.Any(b => Scope(b) == scope))
            {
                yield return new(scope, version);
            }
        }
    }

    private static string PackScope(string packId) => packId switch
    {
        "l3lint.rules.general" => "GEN",
        "l3lint.rules.visa" => "VISA",
        "l3lint.rules.mastercard" => "MC",
        "l3lint.rules.amex" => "AMEX",
        "l3lint.rules.discover" => "DISC",
        _ => packId,
    };

    private static string Scope(CardBrand brand) => brand switch
    {
        CardBrand.Visa => "VISA",
        CardBrand.Mastercard => "MC",
        CardBrand.Amex => "AMEX",
        CardBrand.Discover => "DISC",
        _ => "GEN",
    };

    private static string Count(int count, string singular, string plural) =>
        count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? singular : plural);

    private static string Money(decimal? value, string currency) =>
        value is { } amount ? amount.ToString(CultureInfo.InvariantCulture) + " " + currency : Resources.Strings.TextNoCategory;

    private sealed class Palette(bool enabled)
    {
        public string Paint(Severity severity, string text) => severity switch
        {
            Severity.Error => Wrap("\u001b[31m", text),
            Severity.Warning => Wrap("\u001b[33m", text),
            _ => Wrap("\u001b[36m", text),
        };

        public string Error(string text) => Wrap("\u001b[31m", text);

        private string Wrap(string code, string text) => enabled ? code + text + "\u001b[0m" : text;
    }
}
