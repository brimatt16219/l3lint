using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// The classification scheme of a line item's commodity code.
/// </summary>
/// <remarks>Canonical field <c>lineItems[].commodityScheme</c>. When absent, codes are treated as UNSPSC (GEN-DQ-006).</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CommodityScheme>))]
public enum CommodityScheme
{
    /// <summary>The scheme is not known.</summary>
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    /// <summary>United Nations Standard Products and Services Code (8 digits).</summary>
    [JsonStringEnumMemberName("unspsc")]
    Unspsc,

    /// <summary>National Institute of Governmental Purchasing code.</summary>
    [JsonStringEnumMemberName("nigp")]
    Nigp,

    /// <summary>Harmonized System tariff code.</summary>
    [JsonStringEnumMemberName("hs")]
    Hs,

    /// <summary>North American Industry Classification System code.</summary>
    [JsonStringEnumMemberName("naics")]
    Naics,

    /// <summary>Another scheme.</summary>
    [JsonStringEnumMemberName("other")]
    Other,
}
