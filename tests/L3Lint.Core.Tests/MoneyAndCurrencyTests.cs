using L3Lint.Core.Model;

namespace L3Lint.Core.Tests;

public sealed class MoneyAndCurrencyTests
{
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("JPY", 0)]
    [InlineData("BHD", 3)]
    [InlineData("EUR", 2)]
    [InlineData("CLF", 4)]
    public void Exponents_come_from_the_embedded_iso_table(string code, int exponent)
    {
        Iso4217.TryGetCurrency(code, out var currency).ShouldBeTrue();

        currency.Exponent.ShouldBe(exponent);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("XXX")]
    [InlineData("US")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_or_lowercase_codes_are_rejected(string? code)
    {
        Iso4217.TryGetCurrency(code, out _).ShouldBeFalse();
    }

    [Fact]
    public void Table_holds_the_list_one_currencies()
    {
        Iso4217.All.Count.ShouldBe(165);
        Iso4217.All.Select(c => c.Code).ShouldBeInOrder(SortDirection.Ascending, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("12.50", "USD", false)]
    [InlineData("12.500", "USD", false)]
    [InlineData("12.505", "USD", true)]
    [InlineData("12", "JPY", false)]
    [InlineData("12.5", "JPY", true)]
    [InlineData("1.234", "BHD", false)]
    [InlineData("1.2345", "BHD", true)]
    public void Excess_precision_ignores_trailing_zeros(string value, string code, bool excess)
    {
        Iso4217.TryGetCurrency(code, out var currency);

        new Money(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), currency).HasExcessPrecision.ShouldBe(excess);
    }

    [Theory]
    [InlineData("0.125", MidpointRounding.AwayFromZero, "0.13")]
    [InlineData("0.125", MidpointRounding.ToEven, "0.12")]
    [InlineData("-0.125", MidpointRounding.AwayFromZero, "-0.13")]
    public void Rounding_honors_the_midpoint_mode(string value, MidpointRounding mode, string expected)
    {
        Iso4217.TryGetCurrency("USD", out var usd);

        new Money(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), usd).Rounded(mode)
            .ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Minor_units_convert_with_the_exponent()
    {
        Iso4217.TryGetCurrency("USD", out var usd);
        Iso4217.TryGetCurrency("JPY", out var jpy);

        Money.FromMinorUnits(12050, usd).Value.ShouldBe(120.50m);
        Money.FromMinorUnits(1205, jpy).Value.ShouldBe(1205m);
        new Money(120.50m, usd).ToMinorUnits().ShouldBe(12050m);
        usd.MinorUnit.ShouldBe(0.01m);
        jpy.MinorUnit.ShouldBe(1m);
    }
}
