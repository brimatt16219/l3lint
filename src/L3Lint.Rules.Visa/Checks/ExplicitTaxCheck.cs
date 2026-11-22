using L3Lint.Core.Checks;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.Visa.Checks;

/// <summary>
/// VISA-L3-012: tax must be above 0 unless <c>tax.indicator</c> is exempt. A bare zero is not accepted as an
/// exemption under CEDP (Versapay, Forbes Business Council). No patch: tax treatment is the merchant's call.
/// </summary>
internal sealed class ExplicitTaxCheck : ICustomCheck
{
    private const string Pointer = "/tax/amount";

    public string Name => "ExplicitTax";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var tax = context.Transaction.Tax;
        if (tax.Indicator == TaxIndicator.Exempt || tax.Amount is > 0m || context.IsInvalid(Pointer))
        {
            return;
        }

        sink.Report(new FindingDraft(rule, Pointer)
        {
            Actual = tax.Amount is { } amount ? CheckArguments.Number(amount) : null,
            Arguments = CheckArguments.For(Pointer, null),
        });
    }
}
