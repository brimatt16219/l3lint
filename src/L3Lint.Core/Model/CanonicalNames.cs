namespace L3Lint.Core.Model;

/// <summary>
/// Converts canonical enumerations to and from their JSON string values.
/// </summary>
/// <remarks>Matching is exact and case-sensitive, as the canonical schema requires.</remarks>
public static class CanonicalNames
{
    /// <summary>Gets the JSON value of a card brand.</summary>
    /// <param name="value">The brand.</param>
    /// <returns>For example "visa".</returns>
    public static string ToName(CardBrand value) => value switch
    {
        CardBrand.Visa => "visa",
        CardBrand.Mastercard => "mastercard",
        CardBrand.Amex => "amex",
        CardBrand.Discover => "discover",
        _ => "unknown",
    };

    /// <summary>Gets the JSON value of a card product.</summary>
    /// <param name="value">The product.</param>
    /// <returns>For example "purchasing".</returns>
    public static string ToName(CardProduct value) => value switch
    {
        CardProduct.Consumer => "consumer",
        CardProduct.Business => "business",
        CardProduct.Corporate => "corporate",
        CardProduct.Purchasing => "purchasing",
        CardProduct.Fleet => "fleet",
        CardProduct.CommercialPrepaid => "commercialPrepaid",
        _ => "unknown",
    };

    /// <summary>Gets the JSON value of a tax indicator.</summary>
    /// <param name="value">The indicator.</param>
    /// <returns>For example "exempt".</returns>
    public static string ToName(TaxIndicator value) => value switch
    {
        TaxIndicator.Taxable => "taxable",
        TaxIndicator.Exempt => "exempt",
        _ => "notProvided",
    };

    /// <summary>Gets the JSON value of a debit or credit indicator.</summary>
    /// <param name="value">The indicator.</param>
    /// <returns>"debit" or "credit".</returns>
    public static string ToName(DebitCredit value) => value == DebitCredit.Credit ? "credit" : "debit";

    /// <summary>Gets the JSON value of a channel.</summary>
    /// <param name="value">The channel.</param>
    /// <returns>"cardPresent" or "cardNotPresent".</returns>
    public static string ToName(Channel value) => value == Channel.CardPresent ? "cardPresent" : "cardNotPresent";

    /// <summary>Gets the JSON value of a commodity scheme.</summary>
    /// <param name="value">The scheme.</param>
    /// <returns>For example "unspsc".</returns>
    public static string ToName(CommodityScheme value) => value switch
    {
        CommodityScheme.Unspsc => "unspsc",
        CommodityScheme.Nigp => "nigp",
        CommodityScheme.Hs => "hs",
        CommodityScheme.Naics => "naics",
        CommodityScheme.Other => "other",
        _ => "unknown",
    };

    /// <summary>Parses a card brand JSON value.</summary>
    /// <param name="name">The JSON value, for example "visa".</param>
    /// <param name="value">The brand when recognized.</param>
    /// <returns><see langword="true"/> when the value is a canonical brand name.</returns>
    public static bool TryParse(string? name, out CardBrand value)
    {
        (var ok, value) = name switch
        {
            "visa" => (true, CardBrand.Visa),
            "mastercard" => (true, CardBrand.Mastercard),
            "amex" => (true, CardBrand.Amex),
            "discover" => (true, CardBrand.Discover),
            "unknown" => (true, CardBrand.Unknown),
            _ => (false, CardBrand.Unknown),
        };
        return ok;
    }

    /// <summary>Parses a card product JSON value.</summary>
    /// <param name="name">The JSON value, for example "purchasing".</param>
    /// <param name="value">The product when recognized.</param>
    /// <returns><see langword="true"/> when the value is a canonical product name.</returns>
    public static bool TryParse(string? name, out CardProduct value)
    {
        (var ok, value) = name switch
        {
            "consumer" => (true, CardProduct.Consumer),
            "business" => (true, CardProduct.Business),
            "corporate" => (true, CardProduct.Corporate),
            "purchasing" => (true, CardProduct.Purchasing),
            "fleet" => (true, CardProduct.Fleet),
            "commercialPrepaid" => (true, CardProduct.CommercialPrepaid),
            "unknown" => (true, CardProduct.Unknown),
            _ => (false, CardProduct.Unknown),
        };
        return ok;
    }

    /// <summary>Parses a tax indicator JSON value.</summary>
    /// <param name="name">The JSON value, for example "taxable".</param>
    /// <param name="value">The indicator when recognized.</param>
    /// <returns><see langword="true"/> when the value is a canonical indicator name.</returns>
    public static bool TryParse(string? name, out TaxIndicator value)
    {
        (var ok, value) = name switch
        {
            "taxable" => (true, TaxIndicator.Taxable),
            "exempt" => (true, TaxIndicator.Exempt),
            "notProvided" => (true, TaxIndicator.NotProvided),
            _ => (false, TaxIndicator.NotProvided),
        };
        return ok;
    }

    /// <summary>Parses a debit or credit JSON value.</summary>
    /// <param name="name">"debit" or "credit".</param>
    /// <param name="value">The indicator when recognized.</param>
    /// <returns><see langword="true"/> when the value is recognized.</returns>
    public static bool TryParse(string? name, out DebitCredit value)
    {
        (var ok, value) = name switch
        {
            "debit" => (true, DebitCredit.Debit),
            "credit" => (true, DebitCredit.Credit),
            _ => (false, DebitCredit.Debit),
        };
        return ok;
    }

    /// <summary>Parses a channel JSON value.</summary>
    /// <param name="name">"cardPresent" or "cardNotPresent".</param>
    /// <param name="value">The channel when recognized.</param>
    /// <returns><see langword="true"/> when the value is recognized.</returns>
    public static bool TryParse(string? name, out Channel value)
    {
        (var ok, value) = name switch
        {
            "cardPresent" => (true, Channel.CardPresent),
            "cardNotPresent" => (true, Channel.CardNotPresent),
            _ => (false, Channel.CardNotPresent),
        };
        return ok;
    }

    /// <summary>Parses a commodity scheme JSON value.</summary>
    /// <param name="name">The JSON value, for example "unspsc".</param>
    /// <param name="value">The scheme when recognized.</param>
    /// <returns><see langword="true"/> when the value is recognized.</returns>
    public static bool TryParse(string? name, out CommodityScheme value)
    {
        (var ok, value) = name switch
        {
            "unspsc" => (true, CommodityScheme.Unspsc),
            "nigp" => (true, CommodityScheme.Nigp),
            "hs" => (true, CommodityScheme.Hs),
            "naics" => (true, CommodityScheme.Naics),
            "other" => (true, CommodityScheme.Other),
            "unknown" => (true, CommodityScheme.Unknown),
            _ => (false, CommodityScheme.Unknown),
        };
        return ok;
    }
}
