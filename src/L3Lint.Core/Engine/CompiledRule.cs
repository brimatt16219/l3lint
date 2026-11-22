using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>A rule loaded from a pack: metadata plus compiled applicability and check.</summary>
internal sealed class CompiledRule : IRule
{
    private readonly CompiledApplicability _applicability;
    private readonly IRuleCheck? _check;

    public CompiledRule(RuleMetadata metadata, CompiledApplicability applicability, IRuleCheck? check, IBatchCheck? batch, Severity? severityCap)
    {
        Metadata = metadata;
        _applicability = applicability;
        _check = check;
        Batch = batch;
        SeverityCap = severityCap;
    }

    public RuleMetadata Metadata { get; }

    /// <summary>Gets the batch check when the rule is decided over the whole run.</summary>
    public IBatchCheck? Batch { get; }

    /// <summary>Gets a cap on severity, set when the rule reads a list marked partial (ADR-0025).</summary>
    public Severity? SeverityCap { get; }

    public bool AppliesTo(in RuleContext context) => _applicability.Holds(context);

    public void Evaluate(in RuleContext context, IFindingSink sink) => _check?.Evaluate(context, Metadata, sink);
}
