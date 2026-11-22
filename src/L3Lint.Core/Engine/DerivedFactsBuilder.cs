using L3Lint.Core.Detectors;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>
/// Pipeline stage 6: computes derived facts once per transaction with the four formulas of the Card brand
/// rules tab, in decimal, rounding only at comparison (ADR-0014).
/// </summary>
internal static class DerivedFactsBuilder
{
    private const string DerivedLineTotal = "/lineItems/*/total";

    public static DerivedFacts Build(Transaction transaction, ArithmeticOptions arithmetic, IReadOnlySet<string> derivedFields)
    {
        Currency? currency = Iso4217.TryGetCurrency(transaction.Currency, out var found) ? found : null;
        var minorUnit = currency?.MinorUnit ?? 0.01m;
        var rounding = arithmetic.Rounding;
        var lineTolerance = arithmetic.LineTolerance ?? minorUnit;
        var totalTolerance = arithmetic.TotalTolerance ?? minorUnit;
        if (arithmetic.ToleranceMode == ToleranceMode.Accumulated)
        {
            // 1 minor unit per line, capped at 5, never below the configured total tolerance.
            totalTolerance = Math.Max(totalTolerance, Math.Min(transaction.LineItems.Count, 5) * minorUnit);
        }

        var totalIsDerived = derivedFields.Contains(DerivedLineTotal);
        var lines = new List<LineFacts>(transaction.LineItems.Count);
        decimal sumLineTax = 0m;
        decimal sumLineDiscount = 0m;
        var anyLineTax = false;
        var anyMismatch = false;
        decimal? lineSum = transaction.LineItems.Count > 0 ? 0m : null;
        for (var i = 0; i < transaction.LineItems.Count; i++)
        {
            var line = transaction.LineItems[i];
            var included = line.AmountIncludesTax == true ? line.TaxAmount ?? 0m : 0m;
            decimal? expected = null;
            if (currency is { } c && line.Quantity is { } q && line.UnitCost is { } u)
            {
                expected = Math.Round(q * u, c.Exponent, rounding) - (line.DiscountAmount ?? 0m) + included;
            }

            decimal? difference = line.Total is { } reported && expected is { } e ? reported - e : null;
            var mismatch = !totalIsDerived && difference is { } d && Math.Abs(d) > lineTolerance;
            anyMismatch |= mismatch;
            var isCredit = line.DebitCreditIndicator == DebitCredit.Credit;
            lines.Add(new LineFacts
            {
                Index = i,
                ExpectedTotal = expected,
                Difference = difference,
                TotalMismatch = mismatch,
                IncludedTax = included,
                IsCredit = isCredit,
                TotalIsDerived = totalIsDerived,
                NormalizedDescription = TextNormalizer.Normalize(line.Description),
            });

            if (line.TaxAmount is { } tax)
            {
                anyLineTax = true;
                sumLineTax += tax;
            }

            sumLineDiscount += line.DiscountAmount ?? 0m;
            lineSum = line.Total is { } total && lineSum is { } sum
                ? sum + ((isCredit ? -1m : 1m) * (total - included))
                : null;
        }

        decimal? expectedAmount = null;
        if (currency is not null && lineSum is { } lines2)
        {
            var amounts = transaction.Amounts;
            expectedAmount = lines2 + (transaction.Tax.Amount ?? 0m) + (amounts.Freight ?? 0m) + (amounts.Duty ?? 0m)
                + (amounts.ShippingTax ?? 0m) - (amounts.Discount ?? 0m);
        }

        return new DerivedFacts
        {
            Currency = currency,
            Rounding = rounding,
            LineTolerance = lineTolerance,
            TotalTolerance = totalTolerance,
            Lines = lines,
            ExpectedAmount = expectedAmount,
            SumLineTax = sumLineTax,
            AnyLineTax = anyLineTax,
            SumLineDiscount = sumLineDiscount,
            TaxRatio = transaction.Tax.Amount is { } t && transaction.Amount is { } a && a != 0m ? t / a : null,
            NormalizedMerchantName = TextNormalizer.Normalize(transaction.Merchant.Name),
            AnyLineTotalMismatch = anyMismatch,
            DerivedFields = derivedFields,
        };
    }
}
