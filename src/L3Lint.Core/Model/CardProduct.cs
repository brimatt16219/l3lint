using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// The card product. Only commercial products can earn an enhanced-data rate.
/// </summary>
/// <remarks>Canonical field <c>card.product</c>. Drives eligibility rules (FR-009, US-03).</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CardProduct>))]
public enum CardProduct
{
    /// <summary>The product is not known; eligibility checks cannot run.</summary>
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    /// <summary>A consumer card. Enhanced data earns no rate.</summary>
    [JsonStringEnumMemberName("consumer")]
    Consumer,

    /// <summary>A small business card.</summary>
    [JsonStringEnumMemberName("business")]
    Business,

    /// <summary>A corporate card.</summary>
    [JsonStringEnumMemberName("corporate")]
    Corporate,

    /// <summary>A purchasing card.</summary>
    [JsonStringEnumMemberName("purchasing")]
    Purchasing,

    /// <summary>A fleet card.</summary>
    [JsonStringEnumMemberName("fleet")]
    Fleet,

    /// <summary>A commercial prepaid card.</summary>
    [JsonStringEnumMemberName("commercialPrepaid")]
    CommercialPrepaid,
}
