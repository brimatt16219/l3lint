using System.Collections.Frozen;
using System.Globalization;

namespace L3Lint.Core.Detectors;

/// <summary>
/// The per-payload data quality detectors (Card brand rules tab, "Data quality heuristics"). Each is a pure
/// function so it can be tested alone (FR-016).
/// </summary>
internal static class DataQuality
{
    /// <summary>The Jaro-Winkler threshold for "description is the merchant name" (inferred).</summary>
    public const double MerchantNameSimilarity = 0.92;

    private static readonly FrozenSet<string> PlaceholderCodes =
        FrozenSet.ToFrozenSet(["na", "n/a", "none", "sku"], StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> FillerReferences =
        FrozenSet.ToFrozenSet(["111111", "123456", "000000", "po", "none", "n/a", "na"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Placeholder description: the normalized text is on the placeholder list, is 1 character, is all digits,
    /// is empty after normalization, or the raw text repeats one character ("xxxx", "----").
    /// </summary>
    public static bool IsPlaceholderDescription(string description, IReadOnlySet<string> placeholders)
    {
        var compact = new string([.. description.Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c))]);
        if (compact.Length >= 2 && compact.All(c => c == compact[0]))
        {
            return true;
        }

        var normalized = TextNormalizer.Normalize(description) ?? string.Empty;
        return normalized.Length <= 1
            || placeholders.Contains(normalized)
            || normalized.Replace(" ", string.Empty, StringComparison.Ordinal).All(char.IsAsciiDigit);
    }

    /// <summary>Merchant name used as description: equal after normalization, or Jaro-Winkler at least 0.92.</summary>
    public static bool IsMerchantName(string? normalizedDescription, string? normalizedMerchant) =>
        !string.IsNullOrEmpty(normalizedDescription) && !string.IsNullOrEmpty(normalizedMerchant)
        && (normalizedDescription == normalizedMerchant || JaroWinkler.Similarity(normalizedDescription, normalizedMerchant) >= MerchantNameSimilarity);

    /// <summary>
    /// Placeholder product code or unit: "0", "00", all zeros, "na", "n/a", "none", "sku", or the line's own sequence number.
    /// </summary>
    public static bool IsPlaceholderCode(string code, int? sequence)
    {
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        return trimmed.All(c => c == '0')
            || PlaceholderCodes.Contains(trimmed)
            || (sequence is { } s && trimmed == s.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Filler customer code or PO: 111111, 123456, 000000, po, none, n/a or na.</summary>
    public static bool IsFillerReference(string value) => FillerReferences.Contains(value.Trim());

    /// <summary>
    /// Generic or malformed commodity code. All zeros always fails. For UNSPSC (or an unknown scheme, the usual
    /// case): not 8 digits, a segment outside 10 to 95, or a code ending in 000000 (segment level) or 0000
    /// (family level).
    /// </summary>
    /// <returns>A resource key naming the reason, or <see langword="null"/> when the code looks specific.</returns>
    public static string? CommodityCodeProblem(string code, bool treatAsUnspsc)
    {
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.All(c => c == '0'))
        {
            return "CommodityAllZeros";
        }

        if (!treatAsUnspsc)
        {
            return null;
        }

        if (trimmed.Length != 8 || !trimmed.All(char.IsAsciiDigit))
        {
            return "CommodityNotEightDigits";
        }

        var segment = int.Parse(trimmed[..2], CultureInfo.InvariantCulture);
        if (segment is < 10 or > 95)
        {
            return "CommoditySegmentRange";
        }

        return trimmed.EndsWith("0000", StringComparison.Ordinal) ? "CommodityTooGeneric" : null;
    }
}
