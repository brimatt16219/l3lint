using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-FMT-001: reports schema violations, values an adapter could not convert and CSV rows that disagree.
/// </summary>
internal sealed class SchemaCheck : ICustomCheck
{
    public string Name => "Schema";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var issue in context.State.SchemaIssues)
        {
            sink.Report(new FindingDraft(rule, issue.CanonicalPointer)
            {
                SourcePointer = issue.SourcePointer,
                Arguments = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["field"] = issue.SourcePointer ?? issue.CanonicalPointer,
                    ["detail"] = issue.Detail,
                },
            });
        }
    }
}
