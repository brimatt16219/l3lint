using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// States whether the sale is taxable or explicitly exempt.
/// </summary>
/// <remarks>
/// Canonical field <c>tax.indicator</c>. Under Visa CEDP a zero tax amount is not enough to claim an
/// exemption; the indicator must say <see cref="Exempt"/> (VISA-L3-012).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<TaxIndicator>))]
public enum TaxIndicator
{
    /// <summary>The indicator was not provided. Required-field rules treat this as missing.</summary>
    [JsonStringEnumMemberName("notProvided")]
    NotProvided,

    /// <summary>The sale is taxable.</summary>
    [JsonStringEnumMemberName("taxable")]
    Taxable,

    /// <summary>The sale is tax exempt.</summary>
    [JsonStringEnumMemberName("exempt")]
    Exempt,
}
