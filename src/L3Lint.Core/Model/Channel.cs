using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// Whether the card was present at the sale.
/// </summary>
/// <remarks>Canonical field <c>channel</c>. Card-not-present sales need a billing postal code for Visa (VISA-ELIG-005).</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<Channel>))]
public enum Channel
{
    /// <summary>The card was present.</summary>
    [JsonStringEnumMemberName("cardPresent")]
    CardPresent,

    /// <summary>The card was not present, for example an online or invoice payment.</summary>
    [JsonStringEnumMemberName("cardNotPresent")]
    CardNotPresent,
}
