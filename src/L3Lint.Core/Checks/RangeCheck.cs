using System.Globalization;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>Kind <c>range</c>: a number must lie within min and max (bounds included unless exclusive).</summary>
internal sealed class RangeCheck(FieldSelector selector, decimal? min, decimal? max, bool exclusive) : IRuleCheck
{
    public RuleCheck Description { get; } = new(
        "range",
        [selector.Path],
        [
            new("min", min?.ToString(CultureInfo.InvariantCulture) ?? "none"),
            new("max", max?.ToString(CultureInfo.InvariantCulture) ?? "none"),
            new("exclusive", exclusive ? "true" : "false"),
        ]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var count = selector.Count(context);
        for (var i = 0; i < count; i++)
        {
            var value = selector.Read(context, i);
            if (value.Kind != FieldValueKind.Number || InRange(value.Number))
            {
                continue;
            }

            var pointer = selector.PointerAt(i);
            var arguments = CheckArguments.For(pointer, selector.DisplayIndex(i));
            if (min is { } low)
            {
                arguments["min"] = CheckArguments.Number(low);
            }

            if (max is { } high)
            {
                arguments["max"] = CheckArguments.Number(high);
            }

            sink.Report(new FindingDraft(rule, pointer) { Actual = value.Display(), Arguments = arguments });
        }
    }

    private bool InRange(decimal number) =>
        (min is not { } low || (exclusive ? number > low : number >= low))
        && (max is not { } high || (exclusive ? number < high : number <= high));
}
