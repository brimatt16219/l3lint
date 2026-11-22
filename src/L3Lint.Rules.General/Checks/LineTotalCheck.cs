using System.Text.Json.Nodes;
using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Fixes;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-ARITH-001 (formula 1): expected_i = round_c(q x u) - d + t x I. Fails when the reported total differs by
/// more than the line tolerance. Totals the adapter derived are skipped (ADR-0008). Recomputing the line
/// total is a mechanical fix.
/// </summary>
internal sealed class LineTotalCheck : ICustomCheck
{
    public string Name => "LineTotal";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var line in context.Facts.Lines)
        {
            var pointer = Pointers.Line(line.Index, "total");
            if (!line.TotalMismatch || line.ExpectedTotal is not { } expected || line.Difference is not { } difference || context.IsInvalid(pointer))
            {
                continue;
            }

            var reported = context.Transaction.LineItems[line.Index].Total!.Value;
            var arguments = CheckArguments.For(pointer, line.Index + 1);
            arguments["reported"] = CheckArguments.Number(reported);
            arguments["expected"] = CheckArguments.Number(expected);
            arguments["difference"] = CheckArguments.Number(difference);
            sink.Report(new FindingDraft(rule, pointer)
            {
                Actual = CheckArguments.Number(reported),
                Expected = CheckArguments.Number(expected),
                Arguments = arguments,
                Fix = new FixIntent([new PatchOperation("add", pointer, JsonValue.Create(expected))]),
            });
        }
    }
}
