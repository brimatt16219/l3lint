using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Amex.Checks;

/// <summary>AMEX-FMT-001: transaction advice text holds at most 4 lines of 40 characters each (J.P. Morgan).</summary>
internal sealed class AdviceTextCheck : ICustomCheck
{
    private const int MaxLines = 4;
    private const int MaxLength = 40;

    public string Name => "AdviceText";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        if (context.Transaction.Amex.AdviceText is not { } lines || context.IsInvalid("/amex/adviceText"))
        {
            return;
        }

        if (lines.Count > MaxLines)
        {
            var arguments = CheckArguments.For("/amex/adviceText", null);
            arguments["count"] = CheckArguments.Number(lines.Count);
            arguments["max"] = CheckArguments.Number(MaxLines);
            sink.Report(new FindingDraft(rule, "/amex/adviceText") { Actual = CheckArguments.Number(lines.Count), Arguments = arguments });
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var length = MaxLengthCheck.Length(lines[i]);
            if (length <= MaxLength)
            {
                continue;
            }

            var pointer = "/amex/adviceText/" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var arguments = CheckArguments.For(pointer, i + 1);
            arguments["length"] = CheckArguments.Number(length);
            arguments["max"] = CheckArguments.Number(MaxLength);
            sink.Report(new FindingDraft(rule, pointer)
            {
                Actual = lines[i],
                MessageVariant = "lineTooLong",
                Arguments = arguments,
                Fix = MechanicalFixes.Trim(pointer, lines[i], MaxLength),
            });
        }
    }
}
