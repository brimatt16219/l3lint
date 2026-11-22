using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-PCI-001: reports every value the scrub stage masked. Masking itself already happened before mapping;
/// this check only makes it visible. Code fields give warnings (ADR-0023).
/// </summary>
internal sealed class CardNumberCheck : ICustomCheck
{
    public string Name => "CardNumber";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        foreach (var match in context.CardNumberMatches)
        {
            sink.Report(new FindingDraft(rule, match.CanonicalPointer ?? string.Empty)
            {
                Actual = match.MaskedValue,
                SourcePointer = match.SourcePointer,
                Location = match.Location,
                Severity = match.IsCodeField ? Severity.Warning : null,
                Arguments = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["field"] = match.SourcePointer,
                    ["masked"] = match.MaskedValue,
                },
            });
        }
    }
}
