using System.Globalization;
using System.Text;
using L3Lint.Core;
using L3Lint.Reporting;

namespace L3Lint.Cli.Output;

/// <summary>
/// <c>--annotations github</c>: one GitHub workflow command per finding at or above <c>--fail-on</c>, for example
/// <c>::error file=x.json,line=3,col=9,title=VISA-L3-004::message</c> (Interfaces tab, "CLI").
/// </summary>
internal static class GitHubAnnotations
{
    public static void Write(LintReport report, TextWriter output)
    {
        if (report.FailOn is not { } failOn)
        {
            return;
        }

        var paths = report.Inputs.Select(i => i.Path).ToList();
        var inputOf = report.Transactions.ToDictionary(t => t.Index, t => t.Input);
        foreach (var finding in report.AllFindings().Where(f => f.Severity >= failOn))
        {
            var command = finding.Severity switch
            {
                Severity.Error => "error",
                Severity.Warning => "warning",
                _ => "notice",
            };
            var properties = new List<string>();
            if (inputOf.TryGetValue(finding.TransactionIndex, out var input) && input < paths.Count && paths[input] != "-")
            {
                properties.Add("file=" + Property(paths[input]));
            }

            if (finding.Location is { } location)
            {
                properties.Add("line=" + location.Line.ToString(CultureInfo.InvariantCulture));
                properties.Add("col=" + location.Column.ToString(CultureInfo.InvariantCulture));
            }

            properties.Add("title=" + Property(finding.RuleId));
            var message = string.Format(CultureInfo.InvariantCulture, Resources.Strings.AnnotationMessage, finding.Message, finding.Fix, finding.Path.Source ?? finding.Path.Canonical);
            output.WriteLine("::" + command + " " + string.Join(',', properties) + "::" + Data(message));
        }
    }

    // GitHub workflow command escaping: data escapes %, CR and LF; properties also escape : and ,.
    private static string Data(string value) => new StringBuilder(value)
        .Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A").ToString();

    private static string Property(string value) => new StringBuilder(Data(value))
        .Replace(":", "%3A").Replace(",", "%2C").ToString();
}
