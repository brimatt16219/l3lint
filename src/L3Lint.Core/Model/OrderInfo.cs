namespace L3Lint.Core.Model;

/// <summary>
/// Order and invoice references.
/// </summary>
/// <remarks>Canonical object <c>order</c>.</remarks>
public sealed record OrderInfo
{
    /// <summary>Purchase order number (up to 25 characters; 22 for Amex).</summary>
    public string? PurchaseOrderNumber { get; init; }

    /// <summary>Invoice number (up to 25 characters).</summary>
    public string? InvoiceNumber { get; init; }

    /// <summary>Order date.</summary>
    public DateOnly? OrderDate { get; init; }
}
