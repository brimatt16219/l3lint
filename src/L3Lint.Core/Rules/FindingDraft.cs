using L3Lint.Core.Fixes;
using L3Lint.Core.Input;

namespace L3Lint.Core.Rules;

/// <summary>
/// A raw finding as a rule reports it, before enrichment.
/// </summary>
/// <param name="Rule">The rule that found the problem.</param>
/// <param name="Path">The canonical pointer of the offending value, for example /lineItems/1/commodityCode.</param>
public readonly record struct FindingDraft(RuleMetadata Rule, string Path)
{
    /// <summary>Gets the value found, already masked, for messages and output.</summary>
    public string? Actual { get; init; }

    /// <summary>Gets what was expected, when it can be stated.</summary>
    public string? Expected { get; init; }

    /// <summary>Gets extra message placeholder values, such as count or max.</summary>
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }

    /// <summary>Gets the name of an alternative message template from the rule's <c>messages</c>.</summary>
    public string? MessageVariant { get; init; }

    /// <summary>Gets a severity for this finding that replaces the rule default before overrides (GEN-PCI-001 in code fields).</summary>
    public Severity? Severity { get; init; }

    /// <summary>Gets a mechanical fix in canonical terms.</summary>
    public FixIntent? Fix { get; init; }

    /// <summary>Gets a source pointer that replaces the path map lookup, for values that map to no canonical field.</summary>
    public string? SourcePointer { get; init; }

    /// <summary>Gets a known file position, for adapters that know it (CSV).</summary>
    public SourceLocation? Location { get; init; }

    /// <summary>Gets the transaction the finding belongs to, for batch rules; <see langword="null"/> means the current one.</summary>
    public int? TransactionIndex { get; init; }
}
