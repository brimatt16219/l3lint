namespace L3Lint.Core.Model;

/// <summary>Exact decimal powers of ten for currency exponents.</summary>
internal static class DecimalPowers
{
    private static readonly decimal[] Negative =
    [
        1m, 0.1m, 0.01m, 0.001m, 0.0001m, 0.00001m, 0.000001m, 0.0000001m, 0.00000001m,
    ];

    private static readonly decimal[] Positive =
    [
        1m, 10m, 100m, 1000m, 10000m, 100000m, 1000000m, 10000000m, 100000000m,
    ];

    /// <summary>Returns 10 to the power of minus <paramref name="exponent"/>.</summary>
    public static decimal NegativePowerOfTen(int exponent) => Negative[ClampExponent(exponent)];

    /// <summary>Returns 10 to the power of <paramref name="exponent"/>.</summary>
    public static decimal PowerOfTen(int exponent) => Positive[ClampExponent(exponent)];

    private static int ClampExponent(int exponent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exponent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(exponent, Negative.Length - 1);
        return exponent;
    }
}
