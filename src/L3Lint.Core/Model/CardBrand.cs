using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// The card network that issued the card. Selects which brand rule pack applies.
/// </summary>
/// <remarks>Canonical field <c>card.brand</c>. JSON values are lowercase.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CardBrand>))]
public enum CardBrand
{
    /// <summary>The brand is not known; only the general rule pack runs.</summary>
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    /// <summary>Visa.</summary>
    [JsonStringEnumMemberName("visa")]
    Visa,

    /// <summary>Mastercard.</summary>
    [JsonStringEnumMemberName("mastercard")]
    Mastercard,

    /// <summary>American Express.</summary>
    [JsonStringEnumMemberName("amex")]
    Amex,

    /// <summary>Discover (including Diners Club acceptance).</summary>
    [JsonStringEnumMemberName("discover")]
    Discover,
}
