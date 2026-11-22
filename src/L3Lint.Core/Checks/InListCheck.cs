using System.Collections.Frozen;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// Kind <c>inList</c>: text must be on a list (mode allow) or must not be on it (mode deny). Lists come from
/// pack CSV files or inline <c>values</c>. Missing values are skipped unless <c>allowMissing</c> is false.
/// </summary>
internal sealed class InListCheck : IRuleCheck
{
    private readonly FieldSelector _selector;
    private readonly IReadOnlyList<string> _lists;
    private readonly FrozenSet<string>? _values;
    private readonly bool _ignoreCase;
    private readonly bool _deny;
    private readonly bool _allowMissing;

    public InListCheck(FieldSelector selector, IReadOnlyList<string> lists, IReadOnlyList<string> values, bool ignoreCase, bool deny, bool allowMissing)
    {
        _selector = selector;
        _lists = lists;
        _values = values.Count == 0 ? null : values.ToFrozenSet(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        _ignoreCase = ignoreCase;
        _deny = deny;
        _allowMissing = allowMissing;
        Description = new RuleCheck(
            "inList",
            [selector.Path],
            [
                new(lists.Count > 0 ? "list" : "values", lists.Count > 0 ? string.Join(", ", lists) : string.Join(", ", values)),
                new("mode", deny ? "deny" : "allow"),
                new("caseInsensitive", ignoreCase ? "true" : "false"),
                new("allowMissing", allowMissing ? "true" : "false"),
            ]);
    }

    public RuleCheck Description { get; }

    public IReadOnlyList<string> ListNames => _lists;

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var count = _selector.Count(context);
        for (var i = 0; i < count; i++)
        {
            var value = _selector.Read(context, i);
            if (value.Kind == FieldValueKind.Invalid)
            {
                continue;
            }

            var missing = Missing.Is(value, _selector.Field, allowZero: true, positiveOnly: false);
            if (missing && (_allowMissing || _deny))
            {
                continue;
            }

            var text = value.Display() ?? string.Empty;
            var listed = !missing && IsListed(context, text);
            if (listed != _deny)
            {
                continue;
            }

            var pointer = _selector.PointerAt(i);
            var arguments = CheckArguments.For(pointer, _selector.DisplayIndex(i));
            arguments["list"] = _lists.Count > 0 ? string.Join(", ", _lists) : string.Join(", ", _values!.Order(StringComparer.Ordinal));
            sink.Report(new FindingDraft(rule, pointer)
            {
                Actual = missing ? null : text,
                Arguments = arguments,
                Fix = _deny || missing ? null : UnitFix(context, pointer, text),
            });
        }
    }

    private bool IsListed(in RuleContext context, string text)
    {
        if (_values is not null)
        {
            return _values.Contains(text);
        }

        foreach (var list in _lists)
        {
            if (context.Lists.Contains(list, text, _ignoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private Fixes.FixIntent? UnitFix(in RuleContext context, string pointer, string text)
    {
        foreach (var list in _lists)
        {
            if (list is UnitOfMeasureMaps.MastercardList or UnitOfMeasureMaps.AmexList)
            {
                return MechanicalFixes.UnitOfMeasure(context, list, pointer, text);
            }
        }

        return null;
    }
}
