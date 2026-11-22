namespace L3Lint.Core.Model;

/// <summary>
/// Merchant context for a transaction.
/// </summary>
/// <remarks>Canonical object <c>merchant</c>.</remarks>
public sealed record MerchantInfo
{
    /// <summary>Merchant category code: 4 digits.</summary>
    public string? Mcc { get; init; }

    /// <summary>Merchant name. Data quality checks compare descriptions against it.</summary>
    public string? Name { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code of the merchant.</summary>
    public string? Country { get; init; }

    /// <summary>Merchant tax ID of up to 15 characters.</summary>
    public string? TaxId { get; init; }
}
