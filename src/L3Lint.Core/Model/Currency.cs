namespace L3Lint.Core.Model;

/// <summary>
/// An ISO 4217 currency and its exponent (number of minor-unit decimal places).
/// </summary>
/// <param name="Code">The alpha-3 code, for example USD.</param>
/// <param name="Exponent">Decimal places of the minor unit: 2 for USD, 0 for JPY, 3 for BHD.</param>
/// <remarks>Exponents come from the embedded ISO 4217 table, never from the machine's culture (D9).</remarks>
public readonly record struct Currency(string Code, int Exponent)
{
    /// <summary>
    /// Gets the value of one minor unit in major units, for example 0.01 for USD and 1 for JPY.
    /// </summary>
    public decimal MinorUnit => DecimalPowers.NegativePowerOfTen(Exponent);
}
