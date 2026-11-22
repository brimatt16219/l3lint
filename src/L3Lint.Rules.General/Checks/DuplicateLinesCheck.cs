using System.Globalization;
using L3Lint.Core.Checks;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-DQ-008: two lines identical in description, product code, quantity and unit cost. Lines with neither a
/// description nor a product code are not compared; there is nothing to call a duplicate.
/// </summary>
internal sealed class DuplicateLinesCheck : ICustomCheck
{
    public string Name => "DuplicateLines";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var seen = new Dictionary<(string?, string?, decimal?, decimal?), int>();
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Description is null && line.ProductCode is null)
            {
                continue;
            }

            var key = (line.Description, line.ProductCode, line.Quantity, line.UnitCost);
            if (seen.TryGetValue(key, out var first))
            {
                var pointer = L3Lint.Core.Fields.Pointers.Line(i);
                var arguments = CheckArguments.For(pointer, i + 1);
                arguments["first"] = (first + 1).ToString(CultureInfo.InvariantCulture);
                sink.Report(new FindingDraft(rule, pointer) { Actual = line.Description ?? line.ProductCode, Arguments = arguments });
            }
            else
            {
                seen[key] = i;
            }
        }
    }
}
