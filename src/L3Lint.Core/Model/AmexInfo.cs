namespace L3Lint.Core.Model;

/// <summary>
/// American Express specific fields.
/// </summary>
/// <remarks>Canonical object <c>amex</c>.</remarks>
public sealed record AmexInfo
{
    /// <summary>Name of the person who requested the purchase (up to 40 characters).</summary>
    public string? RequestorName { get; init; }

    /// <summary>Transaction advice text: at most 4 lines of 40 characters each.</summary>
    public IReadOnlyList<string>? AdviceText { get; init; }
}
