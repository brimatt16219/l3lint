using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>
/// Holds the per-run instances of batch rules (GEN-DQ-007) and serializes observations to them, so batch rules
/// need no locking of their own (ADR-0034).
/// </summary>
internal sealed class BatchAggregator
{
    private readonly Dictionary<string, (CompiledRule Rule, IBatchRule Instance)> _rules;
    private readonly Lock _gate = new();

    public BatchAggregator(IEnumerable<CompiledRule> batchRules, LintOptions options)
    {
        _rules = batchRules.ToDictionary(r => r.Metadata.Id, r => (r, r.Batch!.Create(r.Metadata, options)), StringComparer.Ordinal);
    }

    public void Observe(CompiledRule rule, in RuleContext context)
    {
        lock (_gate)
        {
            _rules[rule.Metadata.Id].Instance.Observe(context);
        }
    }

    public List<(CompiledRule Rule, FindingDraft Draft)> Complete()
    {
        var collector = new DraftCollector();
        var results = new List<(CompiledRule, FindingDraft)>();
        lock (_gate)
        {
            foreach (var (_, (rule, instance)) in _rules.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                collector.Drafts.Clear();
                instance.Complete(collector);
                results.AddRange(collector.Drafts.Select(d => (rule, d)));
            }
        }

        return results;
    }
}
