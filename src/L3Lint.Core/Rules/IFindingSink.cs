namespace L3Lint.Core.Rules;

/// <summary>
/// Receives raw findings from rules. The engine enriches them with messages, paths, patches and sources.
/// </summary>
public interface IFindingSink
{
    /// <summary>Reports one problem.</summary>
    /// <param name="draft">The rule, the canonical path and the details.</param>
    void Report(in FindingDraft draft);
}
