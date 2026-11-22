using System.Globalization;
using System.Text.Json.Nodes;
using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Fixes;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-FMT-007: line i (0-based) must carry sequence i + 1, so lines are numbered 1, 2, 3 with no gaps or
/// repeats. Renumbering is a mechanical fix.
/// </summary>
internal sealed class SequenceCheck : ICustomCheck
{
    public string Name => "Sequence";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var pointer = Pointers.Line(i, "sequence");
            var expected = i + 1;
            if (context.IsInvalid(pointer) || lines[i].Sequence == expected)
            {
                continue;
            }

            var arguments = CheckArguments.For(pointer, expected);
            arguments["expected"] = expected.ToString(CultureInfo.InvariantCulture);
            sink.Report(new FindingDraft(rule, pointer)
            {
                Actual = lines[i].Sequence?.ToString(CultureInfo.InvariantCulture),
                Expected = expected.ToString(CultureInfo.InvariantCulture),
                MessageVariant = lines[i].Sequence is null ? "missing" : null,
                Arguments = arguments,
                Fix = new FixIntent([new PatchOperation("add", pointer, JsonValue.Create(expected))]),
            });
        }
    }
}
