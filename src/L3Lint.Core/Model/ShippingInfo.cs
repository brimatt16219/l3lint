namespace L3Lint.Core.Model;

/// <summary>
/// Shipping origin and destination.
/// </summary>
/// <remarks>Canonical object <c>shipping</c>.</remarks>
public sealed record ShippingInfo
{
    /// <summary>Ship-from postal code (up to 10 characters).</summary>
    public string? FromPostalCode { get; init; }

    /// <summary>Ship-to postal code (up to 10 characters).</summary>
    public string? ToPostalCode { get; init; }

    /// <summary>Destination country, ISO 3166-1 alpha-2.</summary>
    public string? ToCountry { get; init; }
}
