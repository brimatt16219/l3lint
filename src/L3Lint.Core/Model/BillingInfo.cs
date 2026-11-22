namespace L3Lint.Core.Model;

/// <summary>
/// Billing address data used for address verification.
/// </summary>
/// <remarks>Canonical object <c>billing</c>.</remarks>
public sealed record BillingInfo
{
    /// <summary>Billing postal code (up to 10 characters).</summary>
    public string? PostalCode { get; init; }
}
