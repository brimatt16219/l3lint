namespace L3Lint.Core.Model;

/// <summary>
/// Card context for a transaction. Never holds a full card number.
/// </summary>
/// <remarks>Canonical object <c>card</c>.</remarks>
public sealed record CardInfo
{
    /// <summary>The card network.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>The card product, for example purchasing or consumer.</summary>
    public CardProduct? Product { get; init; }

    /// <summary>Brand product code of up to 3 characters, for example MBS or MEF for Mastercard.</summary>
    public string? ProductCode { get; init; }

    /// <summary>Whether the card is a federal government card. Absent means false.</summary>
    public bool? IsFederalGovernment { get; init; }

    /// <summary>Exactly 4 digits: the end of the card number. Never a full card number.</summary>
    public string? Last4 { get; init; }
}
