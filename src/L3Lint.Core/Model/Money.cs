namespace L3Lint.Core.Model;

/// <summary>
/// A decimal amount in major units with its currency.
/// </summary>
/// <param name="Value">The amount in major units, for example 12.50.</param>
/// <param name="Currency">The currency that sets the exponent.</param>
/// <remarks>
/// All money arithmetic uses <see cref="decimal"/>, never floating point (D9). Rounding happens only at
/// comparisons, with the configured midpoint mode.
/// </remarks>
public readonly record struct Money(decimal Value, Currency Currency)
{
    /// <summary>
    /// Gets a value indicating whether <see cref="Value"/> has more decimal places than the currency allows.
    /// </summary>
    /// <remarks>Trailing zeros do not count: 12.500 USD has no excess precision because it equals 12.50.</remarks>
    public bool HasExcessPrecision => decimal.Round(Value, Currency.Exponent) != Value;

    /// <summary>
    /// Rounds the value to the currency exponent.
    /// </summary>
    /// <param name="mode">How midpoints round: away from zero (default in l3lint) or to even.</param>
    /// <returns>The rounded value in major units.</returns>
    public decimal Rounded(MidpointRounding mode) => Math.Round(Value, Currency.Exponent, mode);

    /// <summary>
    /// Converts a minor-unit integer, as sent by gateways such as Stripe and Adyen, to major units.
    /// </summary>
    /// <param name="minorUnits">The amount in minor units, for example 1250 for 12.50 USD.</param>
    /// <param name="currency">The currency that sets the exponent.</param>
    /// <returns>The amount in major units.</returns>
    public static Money FromMinorUnits(decimal minorUnits, Currency currency) =>
        new(minorUnits * DecimalPowers.NegativePowerOfTen(currency.Exponent), currency);

    /// <summary>
    /// Gets the value in minor units, for example 1250 for 12.50 USD.
    /// </summary>
    /// <returns>The value multiplied by ten to the power of the exponent.</returns>
    public decimal ToMinorUnits() => Value * DecimalPowers.PowerOfTen(Currency.Exponent);
}
