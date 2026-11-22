namespace L3Lint.Core.Rules;

/// <summary>
/// The enhanced-data level a rule belongs to.
/// </summary>
public enum RuleLevel
{
    /// <summary>Applies regardless of level: format, safety, eligibility.</summary>
    General,

    /// <summary>Level 2 data: tax, customer code, summary fields.</summary>
    L2,

    /// <summary>Level 3 data: line items.</summary>
    L3,
}
