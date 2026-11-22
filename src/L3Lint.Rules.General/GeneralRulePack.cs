using L3Lint.Core.Rules;
using L3Lint.Rules.General.Checks;

namespace L3Lint.Rules.General;

/// <summary>
/// The general rule pack (<c>l3lint.rules.general</c>): 23 rules that apply to every brand, with the checks
/// they name.
/// </summary>
/// <remarks>The pack's YAML, lists, examples and manifest are embedded in this assembly.</remarks>
public sealed class GeneralRulePack : EmbeddedRulePackSource
{
    /// <summary>Initializes a new instance.</summary>
    public GeneralRulePack()
        : base(typeof(GeneralRulePack).Assembly, "general")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ICustomCheck> CustomChecks { get; } =
    [
        new CardNumberCheck(),
        new SchemaCheck(),
        new CurrencyCheck(),
        new AmountPrecisionCheck(),
        new SequenceCheck(),
        new LineTotalCheck(),
        new TransactionTotalCheck(),
        new TaxConsistencyCheck(),
        new NegativeDebitLineCheck(),
        new DoubleDiscountCheck(),
        new PlaceholderDescriptionCheck(),
        new MerchantNameDescriptionCheck(),
        new PlaceholderCodeCheck(),
        new FillerReferenceCheck(),
        new OneLineInvoiceCheck(),
        new CommodityCodeShapeCheck(),
        new DuplicateLinesCheck(),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<IBatchCheck> BatchChecks { get; } = [new StaticBatchDataCheck()];
}
