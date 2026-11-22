using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// Kind <c>required</c>: each selected value must be present (ADR-0013). Parameters: field or fields,
/// allowZero, positiveOnly, report (each or once, ADR-0039).
/// </summary>
internal sealed class RequiredCheck(IReadOnlyList<FieldSelector> selectors, bool allowZero, bool positiveOnly, bool reportOnce) : IRuleCheck
{
    public RuleCheck Description { get; } = new(
        "required",
        [.. selectors.Select(s => s.Path)],
        [new("allowZero", allowZero ? "true" : "false"), new("positiveOnly", positiveOnly ? "true" : "false"), new("report", reportOnce ? "once" : "each")]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var missingCount = 0;
        FindingDraft? first = null;
        foreach (var selector in selectors)
        {
            var count = selector.Count(context);
            for (var i = 0; i < count; i++)
            {
                var value = selector.Read(context, i);
                if (!Missing.Is(value, selector.Field, allowZero, positiveOnly))
                {
                    continue;
                }

                var pointer = selector.PointerAt(i);
                var draft = new FindingDraft(rule, pointer)
                {
                    Actual = value.IsPresent ? value.Display() : null,
                    Arguments = CheckArguments.For(pointer, selector.DisplayIndex(i)),
                    Fix = MechanicalFixes.ForMissing(context, pointer, selector.Field),
                };
                missingCount++;
                if (reportOnce)
                {
                    first ??= draft;
                }
                else
                {
                    sink.Report(draft);
                }
            }
        }

        if (reportOnce && first is { } once)
        {
            var arguments = new Dictionary<string, string>(once.Arguments!, StringComparer.Ordinal)
            {
                ["count"] = CheckArguments.Number(missingCount),
            };
            sink.Report(once with { Arguments = arguments });
        }
    }
}
