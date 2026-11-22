namespace L3Lint.Core.Rules;

/// <summary>
/// A readable description of a rule's check, for explain output and the rule reference.
/// </summary>
/// <param name="Kind">The check kind: required, maxLength, pattern, range, inList, ratio, maxCount, anyOf or custom.</param>
/// <param name="Fields">The canonical fields the check reads.</param>
/// <param name="Parameters">Other parameters as text, in pack order.</param>
public sealed record RuleCheck(string Kind, IReadOnlyList<string> Fields, IReadOnlyList<KeyValuePair<string, string>> Parameters);
