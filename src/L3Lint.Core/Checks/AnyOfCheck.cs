using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>Kind <c>anyOf</c>: at least one of the fields must be present (MC-L2-002: customer code or PO).</summary>
internal sealed class AnyOfCheck(IReadOnlyList<FieldSelector> selectors) : IRuleCheck
{
    public RuleCheck Description { get; } = new("anyOf", [.. selectors.Select(s => s.Path)], []);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var selector in selectors)
        {
            var value = selector.Read(context, 0);
            if (value.Kind == FieldValueKind.Invalid || !Missing.Is(value, selector.Field, allowZero: true, positiveOnly: false))
            {
                return;
            }
        }

        var arguments = CheckArguments.For(selectors[0].Path, null);
        sink.Report(new FindingDraft(rule, selectors[0].Path) { Arguments = arguments });
    }
}
