using System.Collections.Frozen;

namespace L3Lint.Core.Checks;

/// <summary>
/// Documented mappings from free-text units to the Mastercard and Amex code lists (Card brand rules tab:
/// "each to EAC, piece or pc to PCE, lb to LBR, gal to GLI"; anything ambiguous is left for a person).
/// </summary>
/// <remarks>
/// <see cref="Certain"/> maps are applied mechanically. <see cref="Proposed"/> maps (packaging units such as
/// BX or CA, which have no exact equivalent) are only proposals a person must confirm (US-06).
/// </remarks>
internal static class UnitOfMeasureMaps
{
    public const string MastercardList = "mastercardUom";
    public const string AmexList = "amexUom";

    /// <summary>Unambiguous unit words and abbreviations, keyed by list name, compared ignoring case.</summary>
    public static readonly FrozenDictionary<string, FrozenDictionary<string, string>> Certain =
        new Dictionary<string, FrozenDictionary<string, string>>(StringComparer.Ordinal)
        {
            [MastercardList] = Map(new()
            {
                ["each"] = "EAC",
                ["ea"] = "EAC",
                ["piece"] = "PCE",
                ["pieces"] = "PCE",
                ["pc"] = "PCE",
                ["pcs"] = "PCE",
                ["lb"] = "LBR",
                ["lbs"] = "LBR",
                ["pound"] = "LBR",
                ["pounds"] = "LBR",
                ["gal"] = "GLI",
                ["gallon"] = "GLI",
                ["gallons"] = "GLI",
                ["kg"] = "KGM",
                ["kilogram"] = "KGM",
                ["g"] = "GRM",
                ["gram"] = "GRM",
                ["l"] = "LTR",
                ["liter"] = "LTR",
                ["litre"] = "LTR",
                ["ml"] = "MLI",
                ["milliliter"] = "MLI",
                ["m"] = "MTR",
                ["meter"] = "MTR",
                ["metre"] = "MTR",
                ["cm"] = "CMT",
                ["mm"] = "MMT",
                ["km"] = "KMT",
                ["ft"] = "FOT",
                ["foot"] = "FOT",
                ["feet"] = "FOT",
                ["in"] = "INH",
                ["inch"] = "INH",
                ["inches"] = "INH",
                ["yd"] = "YRD",
                ["yard"] = "YRD",
                ["oz"] = "ONZ",
                ["ounce"] = "ONZ",
                ["pt"] = "PTI",
                ["pint"] = "PTI",
                ["qt"] = "QTI",
                ["quart"] = "QTI",
                ["acre"] = "ACR",
                ["hectare"] = "HAR",
                ["mile"] = "NMI",
                ["miles"] = "NMI",
            }),
            [AmexList] = Map(new()
            {
                ["each"] = "EA",
                ["box"] = "BX",
                ["case"] = "CA",
                ["hour"] = "HR",
                ["hours"] = "HR",
                ["hr"] = "HR",
                ["dozen"] = "DZ",
                ["pound"] = "LB",
                ["pounds"] = "LB",
                ["lbs"] = "LB",
                ["gallon"] = "GA",
                ["gal"] = "GA",
                ["foot"] = "FT",
                ["feet"] = "FT",
                ["inch"] = "IN",
                ["kilogram"] = "KG",
                ["kg"] = "KG",
                ["piece"] = "PC",
                ["pieces"] = "PC",
                ["pcs"] = "PC",
                ["pack"] = "PK",
                ["package"] = "PK",
                ["pair"] = "PR",
                ["roll"] = "RL",
                ["set"] = "SE",
                ["yard"] = "YD",
                ["meter"] = "MR",
                ["liter"] = "LT",
                ["day"] = "DA",
                ["month"] = "MO",
            }),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Count-like packaging units with no exact equivalent: proposals only.</summary>
    public static readonly FrozenDictionary<string, FrozenDictionary<string, string>> Proposed =
        new Dictionary<string, FrozenDictionary<string, string>>(StringComparer.Ordinal)
        {
            [MastercardList] = Map(new()
            {
                ["bx"] = "PCE",
                ["box"] = "PCE",
                ["ca"] = "PCE",
                ["cs"] = "PCE",
                ["case"] = "PCE",
                ["pk"] = "PCE",
                ["pack"] = "PCE",
                ["dz"] = "PCE",
                ["dozen"] = "PCE",
                ["set"] = "PCE",
                ["kit"] = "PCE",
                ["rl"] = "PCE",
                ["roll"] = "PCE",
                ["bg"] = "PCE",
                ["bag"] = "PCE",
                ["pr"] = "PCE",
                ["pair"] = "PCE",
                ["hr"] = "EAC",
                ["hour"] = "EAC",
                ["unit"] = "EAC",
                ["units"] = "EAC",
            }),
            [AmexList] = Map(new()
            {
                ["unit"] = "EA",
                ["units"] = "EA",
                ["eac"] = "EA",
                ["pce"] = "PC",
            }),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static FrozenDictionary<string, string> Map(Dictionary<string, string> entries) =>
        entries.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
