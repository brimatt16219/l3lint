using System.Globalization;
using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-DQ-006: all-zero codes always fail; for UNSPSC (or an unknown scheme), codes must be 8 digits, start with
/// a segment from 10 to 95, and not end in 0000 (segment or family level). Structural only.
/// </summary>
internal sealed class CommodityCodeShapeCheck : ICustomCheck
{
    public string Name => "CommodityCodeShape";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.CommodityCode is not { } code)
            {
                continue;
            }

            var unspsc = line.CommodityScheme is null or CommodityScheme.Unknown or CommodityScheme.Unspsc;
            if (DataQuality.CommodityCodeProblem(code, unspsc) is not { } reason)
            {
                continue;
            }

            var pointer = LineScans.Pointer(i, "commodityCode");
            var arguments = CheckArguments.For(pointer, i + 1);
            arguments["detail"] = L3Lint.Core.Resources.Strings.ResourceManager.GetString(reason, CultureInfo.InvariantCulture) ?? reason;
            sink.Report(new FindingDraft(rule, pointer) { Actual = code, Arguments = arguments });
        }
    }
}
