using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-DQ-007: creates the per-run rule that spots one description or product code on most of a batch.</summary>
internal sealed class StaticBatchDataCheck : IBatchCheck
{
    public string Name => "StaticBatchData";

    public IBatchRule Create(RuleMetadata rule, LintOptions options) => new StaticBatchDataRule(rule, options.Batch);
}
