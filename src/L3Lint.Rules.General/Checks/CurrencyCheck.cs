using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>GEN-FMT-002: the currency must be present and an active ISO 4217 alpha-3 code (case-sensitive).</summary>
internal sealed class CurrencyCheck : ICustomCheck
{
    private const string Pointer = "/currency";

    public string Name => "Currency";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        if (context.IsInvalid(Pointer))
        {
            return;
        }

        var currency = context.Transaction.Currency;
        if (string.IsNullOrWhiteSpace(currency))
        {
            sink.Report(new FindingDraft(rule, Pointer) { MessageVariant = "missing" });
            return;
        }

        if (!Iso4217.TryGetCurrency(currency, out _))
        {
            var upper = currency.Trim().ToUpperInvariant();
            sink.Report(new FindingDraft(rule, Pointer)
            {
                Actual = currency,
                Expected = Core.Resources.Strings.ExpectedIsoCurrency,
                Fix = Iso4217.TryGetCurrency(upper, out _)
                    ? new Core.Fixes.FixIntent([new Core.Fixes.PatchOperation("add", Pointer, System.Text.Json.Nodes.JsonValue.Create(upper))])
                    : null,
            });
        }
    }
}
