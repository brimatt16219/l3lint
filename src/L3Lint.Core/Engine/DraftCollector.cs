using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>Collects raw findings for one transaction.</summary>
internal sealed class DraftCollector : IFindingSink
{
    public List<FindingDraft> Drafts { get; } = [];

    public void Report(in FindingDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft.Rule);
        ArgumentNullException.ThrowIfNull(draft.Path);
        Drafts.Add(draft);
    }
}
