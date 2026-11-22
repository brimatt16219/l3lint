using System.Text.Json.Serialization;

namespace L3Lint.Core.Model;

/// <summary>
/// Marks whether a line item adds to or subtracts from the transaction total.
/// </summary>
/// <remarks>
/// Canonical field <c>lineItems[].debitCreditIndicator</c>. Credit lines (returns) carry positive
/// values and subtract in the transaction total formula (ADR-0014).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<DebitCredit>))]
public enum DebitCredit
{
    /// <summary>The line adds to the total. This is the default when the indicator is absent.</summary>
    [JsonStringEnumMemberName("debit")]
    Debit,

    /// <summary>The line subtracts from the total, for example a returned item.</summary>
    [JsonStringEnumMemberName("credit")]
    Credit,
}
