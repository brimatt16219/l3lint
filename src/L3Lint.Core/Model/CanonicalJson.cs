using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace L3Lint.Core.Model;

/// <summary>
/// Writes a <see cref="Transaction"/> as canonical JSON (schema v1).
/// </summary>
/// <remarks>
/// Output uses camelCase names, omits absent values and empty objects, keeps decimal scale (12.50 stays
/// 12.50) and is deterministic for a given transaction. Serialization uses a source-generated context, so
/// it needs no reflection. Reading canonical JSON goes through the linter's tolerant reader instead, so
/// that type errors become findings rather than exceptions (ADR-0036).
/// </remarks>
public static class CanonicalJson
{
    private static readonly JsonSerializerOptions Indented = CreateOptions(writeIndented: true);
    private static readonly JsonSerializerOptions Compact = CreateOptions(writeIndented: false);

    /// <summary>
    /// Serializes a transaction to an indented canonical JSON string.
    /// </summary>
    /// <param name="transaction">The transaction to write.</param>
    /// <returns>Canonical JSON text with LF line endings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    public static string Serialize(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return JsonSerializer.Serialize(transaction, TypeInfo(Indented));
    }

    /// <summary>
    /// Serializes a transaction to compact canonical JSON as UTF-8 bytes.
    /// </summary>
    /// <param name="transaction">The transaction to write.</param>
    /// <returns>UTF-8 encoded canonical JSON without indentation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    public static byte[] SerializeToUtf8Bytes(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return JsonSerializer.SerializeToUtf8Bytes(transaction, TypeInfo(Compact));
    }

    /// <summary>
    /// Serializes a transaction to a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="transaction">The transaction to write.</param>
    /// <returns>A detached element holding the canonical JSON.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    public static JsonElement SerializeToElement(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return JsonSerializer.SerializeToElement(transaction, TypeInfo(Compact));
    }

    private static JsonTypeInfo<Transaction> TypeInfo(JsonSerializerOptions options) =>
        (JsonTypeInfo<Transaction>)options.GetTypeInfo(typeof(Transaction));

    private static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions(CanonicalJsonContext.Default.Options)
        {
            WriteIndented = writeIndented,
            NewLine = "\n",
            IndentSize = 2,
            TypeInfoResolver = CanonicalJsonContext.Default.WithAddedModifier(SkipEmptyObjects),
        };
        options.MakeReadOnly();
        return options;
    }

    // Absent objects stay absent: an empty card or amex object would add noise to converted payloads.
    private static void SkipEmptyObjects(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (IsNestedRecord(property.PropertyType) || property.PropertyType == typeof(IReadOnlyList<LineItem>))
            {
                property.ShouldSerialize = static (_, value) => !IsEmpty(value);
            }
        }
    }

    private static bool IsNestedRecord(Type type) =>
        type == typeof(CardInfo) || type == typeof(MerchantInfo) || type == typeof(CustomerInfo)
        || type == typeof(OrderInfo) || type == typeof(TaxInfo) || type == typeof(SummaryAmounts)
        || type == typeof(ShippingInfo) || type == typeof(BillingInfo) || type == typeof(AmexInfo);

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        CardInfo card => card == EmptyRecords.Card,
        MerchantInfo merchant => merchant == EmptyRecords.Merchant,
        CustomerInfo customer => customer == EmptyRecords.Customer,
        OrderInfo order => order == EmptyRecords.Order,
        TaxInfo tax => tax == EmptyRecords.Tax,
        SummaryAmounts amounts => amounts == EmptyRecords.Amounts,
        ShippingInfo shipping => shipping == EmptyRecords.Shipping,
        BillingInfo billing => billing == EmptyRecords.Billing,
        AmexInfo amex => amex.RequestorName is null && amex.AdviceText is null,
        IReadOnlyList<LineItem> lines => lines.Count == 0,
        _ => false,
    };
}
