using System.Globalization;
using L3Lint.Core.Fields;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Checks;

/// <summary>
/// Kind <c>ratio</c>: numerator divided by denominator must lie within min and max, ends included
/// (formula 4, tax ratio). Skipped when either value is missing or the denominator is 0.
/// </summary>
internal sealed class RatioCheck(FieldSelector numerator, FieldSelector denominator, decimal min, decimal max) : IRuleCheck
{
    public RuleCheck Description { get; } = new(
        "ratio",
        [numerator.Path, denominator.Path],
        [new("min", min.ToString(CultureInfo.InvariantCulture)), new("max", max.ToString(CultureInfo.InvariantCulture))]);

    public IReadOnlyList<string> ListNames => [];

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var top = numerator.Read(context, 0);
        var bottom = denominator.Read(context, 0);
        if (top.Kind != FieldValueKind.Number || bottom.Kind != FieldValueKind.Number || bottom.Number == 0m)
        {
            return;
        }

        var ratio = top.Number / bottom.Number;
        if (ratio >= min && ratio <= max)
        {
            return;
        }

        var arguments = CheckArguments.For(numerator.Path, null);
        arguments["ratio"] = CheckArguments.Percent(ratio);
        arguments["min"] = CheckArguments.Percent(min);
        arguments["max"] = CheckArguments.Percent(max);
        sink.Report(new FindingDraft(rule, numerator.Path)
        {
            Actual = CheckArguments.Percent(ratio),
            Expected = string.Format(CultureInfo.InvariantCulture, Resources.Strings.ExpectedRatio, CheckArguments.Percent(min), CheckArguments.Percent(max)),
            Arguments = arguments,
        });
    }
}
