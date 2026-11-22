using System.Globalization;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>A compiled applicability condition on a transaction-level field or the line-items collection.</summary>
internal sealed class CompiledCondition
{
    private readonly FieldSelector _selector;
    private readonly string _operator;
    private readonly IReadOnlyList<string> _operands;
    private readonly decimal _number;

    public CompiledCondition(RuleCondition condition)
    {
        _selector = FieldSelector.Parse(condition.Field);
        if (_selector.IsWildcard)
        {
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackConditionWildcard, condition.Field));
        }

        _operator = condition.Operator;
        _operands = condition.Operands;
        if (_operator is "greaterThan" or "lessThan")
        {
            _number = decimal.Parse(_operands[0], NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        Source = condition;
    }

    public RuleCondition Source { get; }

    public bool Holds(in RuleContext context)
    {
        var value = _selector.Read(context, 0);
        var missing = Missing.Is(value, _selector.Field, allowZero: true, positiveOnly: false);
        var text = missing ? null : value.Display();
        return _operator switch
        {
            "equals" => text is not null && Same(value, text, _operands[0]),
            "notEquals" => text is null || !Same(value, text, _operands[0]),
            "in" => text is not null && _operands.Any(o => Same(value, text, o)),
            "notIn" => text is null || !_operands.Any(o => Same(value, text, o)),
            "inList" => text is not null && context.Lists.Contains(_operands[0], text),
            "present" => !missing == (_operands[0] == "true"),
            "greaterThan" => value.Kind == FieldValueKind.Number ? value.Number > _number : value.Kind == FieldValueKind.List && value.Count > _number,
            "lessThan" => value.Kind == FieldValueKind.Number ? value.Number < _number : value.Kind == FieldValueKind.List && value.Count < _number,
            _ => false,
        };
    }

    private static bool Same(FieldValue value, string text, string operand) =>
        value.Kind == FieldValueKind.Number
            ? decimal.TryParse(operand, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && n == value.Number
            : string.Equals(text, operand, StringComparison.Ordinal);
}
