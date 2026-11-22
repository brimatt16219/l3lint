using L3Lint.Core.Checks;
using L3Lint.Core.Detectors;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Visa.Checks;

/// <summary>
/// VISA-DQ-002: zeros or filler in the required code fields (product code, commodity code, unit of measure).
/// "Blanks, zeros and filler text in required fields" fail CEDP validation (Payroc).
/// </summary>
internal sealed class VisaCodeQualityCheck : ICustomCheck
{
    public string Name => "VisaCodeQuality";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var (index, field, value) in LineScans.PlaceholderCodes(context, includeCommodity: true))
        {
            var pointer = LineScans.Pointer(index, field);
            sink.Report(new FindingDraft(rule, pointer) { Actual = value, Arguments = CheckArguments.For(pointer, index + 1) });
        }
    }
}
