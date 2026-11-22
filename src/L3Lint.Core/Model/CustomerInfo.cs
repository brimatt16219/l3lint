namespace L3Lint.Core.Model;

/// <summary>
/// The buyer's reference for the transaction.
/// </summary>
/// <remarks>Canonical object <c>customer</c>.</remarks>
public sealed record CustomerInfo
{
    /// <summary>Customer code, often the buyer's purchase order number (up to 25 characters).</summary>
    public string? Reference { get; init; }
}
