using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using L3Lint.Core;
using L3Lint.Core.Model;

namespace L3Lint.Rules.Tests.Properties;

/// <summary>
/// The arithmetic formulas in the Card brand rules tab, checked over generated payloads (Test and delivery tab,
/// "Property-based": generated valid transactions pass every arithmetic rule; perturbing one amount beyond tolerance
/// fails exactly the expected rule). Expected totals are computed here from the formulas, not by l3lint.
/// </summary>
public sealed class LineArithmeticProperties
{
    /// <summary>The three exponents the spec names: "USD 2, JPY 0, BHD 3".</summary>
    private static readonly (string Code, int Exponent)[] Currencies = [("USD", 2), ("JPY", 0), ("BHD", 3)];

    [Property(MaxTest = 1000)]
    public Property A_payload_built_from_the_formulas_passes_every_arithmetic_rule() =>
        Prop.ForAll(Payloads().ToArbitrary(), payload =>
        {
            var result = Linters.Packs.Lint(payload.Transaction);

            var arithmetic = result.Findings.Where(f => f.RuleId is "GEN-ARITH-001" or "GEN-ARITH-002" or "GEN-ARITH-003" or "L3LINT-ENGINE-001").ToList();
            arithmetic.ShouldBeEmpty(Describe(payload, arithmetic));
        });

    [Property(MaxTest = 1000)]
    public Property A_line_total_moved_beyond_the_tolerance_fails_GEN_ARITH_001_on_that_line_only() =>
        Prop.ForAll(Payloads().Zip(Gen.Choose(0, 1000)).Zip(Gen.Choose(2, 5000)).Zip(Gen.Elements(-1, 1)).ToArbitrary(), input =>
        {
            var (((payload, pick), minorUnits), sign) = input;
            var line = pick % payload.Transaction.LineItems.Count;
            var minor = MinorUnit(payload.Exponent);

            // The default line tolerance is one minor unit (Card brand rules tab, "Tolerance and rounding settings"),
            // so a move of two or more minor units must fail.
            var lines = payload.Transaction.LineItems.ToList();
            lines[line] = lines[line] with { Total = lines[line].Total + (sign * minorUnits * minor) };
            var perturbed = payload.Transaction with { LineItems = lines };

            var failures = Linters.Packs.Lint(perturbed).Findings.Where(f => f.RuleId == "GEN-ARITH-001").ToList();

            failures.ShouldHaveSingleItem(Describe(payload, failures)).Path.Canonical.ShouldBe($"/lineItems/{line}/total");
        });

    [Property(MaxTest = 1000)]
    public Property A_line_total_off_by_less_than_the_tolerance_still_passes() =>
        Prop.ForAll(Payloads().Zip(Gen.Choose(0, 1000)).Zip(Gen.Elements(-1, 1)).ToArbitrary(), input =>
        {
            var ((payload, pick), sign) = input;
            var line = pick % payload.Transaction.LineItems.Count;

            // One minor unit is inside the tolerance ("Absorbs one rounding step, nothing more").
            var lines = payload.Transaction.LineItems.ToList();
            lines[line] = lines[line] with { Total = lines[line].Total + (sign * MinorUnit(payload.Exponent)) };
            var shifted = payload.Transaction with { LineItems = lines };

            Linters.Packs.Lint(shifted).Findings.ShouldNotContain(f => f.RuleId == "GEN-ARITH-001");
        });

    /// <summary>A generated transaction and the exponent of its currency.</summary>
    public sealed record ArithmeticPayload(Transaction Transaction, int Exponent);

    private static Gen<ArithmeticPayload> Payloads() =>
        from currency in Gen.Elements(Currencies)
        from count in Gen.Choose(1, 10)
        from lines in Gen.ListOf(Line(currency.Exponent), count)
        from freight in Gen.Choose(1, 50_000).Select(f => Scaled(f, currency.Exponent))
        select Build(currency.Code, currency.Exponent, [.. lines], freight);

    /// <summary>One line that satisfies formula 1: total = round(q x u) - d + t x I.</summary>
    private static Gen<LineItem> Line(int exponent) =>
        from quantity in Gen.Choose(1, 100_000).Zip(Gen.Choose(0, 3)).Select(p => Scaled(p.Item1, p.Item2))
        from unitCost in Gen.Choose(1, 2_000_000).Zip(Gen.Choose(0, 4)).Select(p => Scaled(p.Item1, p.Item2))
        from discountShare in Gen.Choose(0, 100)
        from taxRate in Gen.Choose(0, 1500)
        from includesTax in Gen.Elements(true, false)
        let extended = Math.Round(quantity * unitCost, exponent, MidpointRounding.AwayFromZero)
        let discount = Math.Round(extended * discountShare / 200m, exponent, MidpointRounding.AwayFromZero)
        let tax = Math.Round((extended - discount) * taxRate / 10_000m, exponent, MidpointRounding.AwayFromZero)
        select new LineItem
        {
            Description = "Nitrile exam gloves L 100",
            ProductCode = "GLV-NTR-L100",
            CommodityCode = "42132203",
            CommodityScheme = CommodityScheme.Unspsc,
            Quantity = quantity,
            UnitOfMeasure = "EA",
            UnitCost = unitCost,
            DiscountAmount = discount,
            TaxAmount = tax,
            Taxable = tax > 0,
            AmountIncludesTax = includesTax,
            Total = extended - discount + (includesTax ? tax : 0m),
        };

    /// <summary>Numbers the lines and sets the amount and header tax from formulas 2 and 3.</summary>
    private static ArithmeticPayload Build(string currency, int exponent, List<LineItem> lines, decimal freight)
    {
        var numbered = lines.Select((l, i) => l with { Sequence = i + 1 }).ToList();
        var headerTax = numbered.Sum(l => l.TaxAmount!.Value);
        var amount = numbered.Sum(l => l.Total!.Value - (l.AmountIncludesTax == true ? l.TaxAmount!.Value : 0m)) + headerTax + freight;
        var transaction = new Transaction
        {
            Id = "INV-PROP",
            Amount = amount,
            Currency = currency,
            TransactionDate = new DateOnly(2026, 10, 19),
            Channel = Channel.CardNotPresent,
            Card = new CardInfo { Brand = CardBrand.Visa, Product = CardProduct.Purchasing },
            Merchant = new MerchantInfo { Mcc = "5047", Name = "Gulfside Medical Supply", Country = "US" },
            Customer = new CustomerInfo { Reference = "PO-77812" },
            Tax = new TaxInfo { Amount = headerTax, Indicator = TaxIndicator.Taxable },
            Amounts = new SummaryAmounts { Freight = freight },
            LineItems = numbered,
        };
        return new ArithmeticPayload(transaction, exponent);
    }

    private static decimal Scaled(int units, int scale) => new(units, 0, 0, false, (byte)scale);

    private static decimal MinorUnit(int exponent) => Scaled(1, exponent);

    private static string Describe(ArithmeticPayload payload, IEnumerable<Finding> findings) =>
        CanonicalJson.Serialize(payload.Transaction) + "\n" + string.Join("\n", findings.Select(f => $"{f.RuleId} {f.Path.Canonical}: {f.Message}"));
}
