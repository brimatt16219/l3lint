namespace L3Lint.Core.Rules;

/// <summary>
/// One applicability condition on a canonical field, as written in a rule pack.
/// </summary>
/// <param name="Field">The canonical pointer, for example /tax/indicator.</param>
/// <param name="Operator">One of equals, notEquals, in, notIn, inList, present, greaterThan, lessThan.</param>
/// <param name="Operands">The operand values as text; a list name for inList.</param>
public sealed record RuleCondition(string Field, string Operator, IReadOnlyList<string> Operands);
