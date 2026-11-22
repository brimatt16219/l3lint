using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using L3Lint.Core.Model;

namespace L3Lint.Core.Tests.Properties;

/// <summary>
/// Money and currency rounding for every ISO 4217 exponent (Architecture tab, "Money rules"; Card brand rules tab,
/// "All math runs in decimal at full precision and rounds once, at the comparison, to the currency's exponent").
/// Each case checks every currency in the embedded table, so every exponent is covered on every run.
/// </summary>
public sealed class MoneyProperties
{
    private static readonly MidpointRounding[] Modes = [MidpointRounding.AwayFromZero, MidpointRounding.ToEven];

    [Fact]
    public void The_table_has_the_exponents_the_spec_names()
    {
        // Card brand rules tab: "USD 2, JPY 0, BHD 3". ISO 4217 list one also has exponent 4 (CLF, UYW).
        Iso4217.TryGetCurrency("USD", out var usd).ShouldBeTrue();
        Iso4217.TryGetCurrency("JPY", out var jpy).ShouldBeTrue();
        Iso4217.TryGetCurrency("BHD", out var bhd).ShouldBeTrue();
        (usd.Exponent, jpy.Exponent, bhd.Exponent).ShouldBe((2, 0, 3));
        Iso4217.All.Select(c => c.Exponent).Distinct().Order().ShouldBe([0, 2, 3, 4]);
    }

    [Property(MaxTest = 1000)]
    public Property Whole_minor_units_round_trip_and_never_round() =>
        Prop.ForAll(MinorUnits(), minor =>
        {
            foreach (var currency in Iso4217.All)
            {
                var money = Money.FromMinorUnits(minor, currency);

                money.ToMinorUnits().ShouldBe(minor, currency.Code);
                money.HasExcessPrecision.ShouldBeFalse(currency.Code);
                foreach (var mode in Modes)
                {
                    money.Rounded(mode).ShouldBe(money.Value, currency.Code);
                }
            }
        });

    [Property(MaxTest = 1000)]
    public Property Rounding_lands_on_the_exponent_within_half_a_minor_unit() =>
        Prop.ForAll(MinorUnitGen().Zip(Gen.Choose(1, 4)).ToArbitrary(), pair =>
        {
            var (raw, extraDigits) = pair;
            foreach (var currency in Iso4217.All)
            {
                // A value with 1 to 4 more decimals than the currency allows.
                var value = raw * PowerOfTen(-(currency.Exponent + extraDigits));
                var money = new Money(value, currency);
                var half = currency.MinorUnit / 2;

                foreach (var mode in Modes)
                {
                    var rounded = money.Rounded(mode);
                    var inMinorUnits = rounded / currency.MinorUnit;

                    inMinorUnits.ShouldBe(decimal.Truncate(inMinorUnits), $"{currency.Code} {value} {mode}");
                    Math.Abs(rounded - value).ShouldBeLessThanOrEqualTo(half, $"{currency.Code} {value} {mode}");
                    new Money(rounded, currency).Rounded(mode).ShouldBe(rounded, $"{currency.Code} {value} {mode}");
                    new Money(rounded, currency).HasExcessPrecision.ShouldBeFalse($"{currency.Code} {value} {mode}");
                }

                var awayFromZero = money.Rounded(MidpointRounding.AwayFromZero);
                money.HasExcessPrecision.ShouldBe(awayFromZero != value, $"{currency.Code} {value}");

                // Half away from zero: an exact midpoint moves away from zero, so the magnitude grows.
                if (Math.Abs(awayFromZero - value) == half)
                {
                    Math.Abs(awayFromZero).ShouldBeGreaterThan(Math.Abs(value), $"{currency.Code} {value}");
                }
            }
        });

    [Property(MaxTest = 1000)]
    public Property Midpoints_differ_between_the_two_modes_by_one_minor_unit_at_most() =>
        Prop.ForAll(MinorUnits(), minor =>
        {
            foreach (var currency in Iso4217.All)
            {
                // minor + 0.5 minor units is always an exact midpoint.
                var value = (minor + 0.5m) * currency.MinorUnit;
                var money = new Money(value, currency);

                var away = money.Rounded(MidpointRounding.AwayFromZero);
                var even = money.Rounded(MidpointRounding.ToEven);

                Math.Abs(away - even).ShouldBeLessThanOrEqualTo(currency.MinorUnit, currency.Code);
                Math.Abs(away).ShouldBeGreaterThan(Math.Abs(value), currency.Code);
                (even / currency.MinorUnit % 2).ShouldBe(0m, currency.Code);
            }
        });

    [Property(MaxTest = 1000)]
    public Property Trailing_zeros_do_not_change_the_result() =>
        Prop.ForAll(MinorUnits(), minor =>
        {
            foreach (var currency in Iso4217.All)
            {
                var plain = Money.FromMinorUnits(minor, currency);
                var padded = plain with { Value = plain.Value * 1.0000m };

                padded.HasExcessPrecision.ShouldBe(plain.HasExcessPrecision, currency.Code);
                padded.Rounded(MidpointRounding.AwayFromZero).ShouldBe(plain.Rounded(MidpointRounding.AwayFromZero), currency.Code);
                padded.ToMinorUnits().ShouldBe(minor, currency.Code);
            }
        });

    /// <summary>Whole minor units up to plus or minus one trillion, with small values more likely.</summary>
    private static Arbitrary<long> MinorUnits() => MinorUnitGen().ToArbitrary();

    private static Gen<long> MinorUnitGen() =>
        Gen.OneOf(
            Gen.Choose(-1000, 1000).Select(i => (long)i),
            Gen.Choose(-1_000_000, 1_000_000).Select(i => (long)i),
            Gen.Choose(-1_000_000, 1_000_000).Zip(Gen.Choose(0, 999_999)).Select(p => ((long)p.Item1 * 1_000_000) + p.Item2));

    private static decimal PowerOfTen(int exponent)
    {
        var result = 1m;
        for (var i = 0; i < Math.Abs(exponent); i++)
        {
            result = exponent < 0 ? result / 10m : result * 10m;
        }

        return result;
    }
}
