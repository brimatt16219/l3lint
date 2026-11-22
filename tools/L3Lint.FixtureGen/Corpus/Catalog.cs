namespace L3Lint.FixtureGen.Corpus;

/// <summary>A synthetic catalog item. Descriptions fit Visa's 26 characters; units are on each brand's list.</summary>
/// <param name="Description">Item description, at most 26 characters, never a placeholder term.</param>
/// <param name="ProductCode">Merchant product code, at most 12 characters.</param>
/// <param name="CommodityCode">An 8-digit UNSPSC commodity code.</param>
/// <param name="Unit">Unit of measure for Visa, Amex and Discover (ANSI X12 element 355 codes, on the Amex list).</param>
/// <param name="MastercardUnit">Unit of measure on the Mastercard list.</param>
/// <param name="Price">Unit cost in major units.</param>
internal sealed record CatalogItem(string Description, string ProductCode, string CommodityCode, string Unit, string MastercardUnit, decimal Price);

/// <summary>The data the corpus generator draws from. All of it is synthetic.</summary>
internal static class Catalog
{
    public static readonly IReadOnlyList<CatalogItem> Items =
    [
        new("Nitrile exam gloves L 100", "GLV-NTR-L100", "42132203", "BX", "EAC", 12.50m),
        new("Isolation gown blue cs 50", "GWN-ISO-BL50", "42131606", "CA", "PCE", 64.00m),
        new("Copy paper letter 10 ream", "PPR-LTR-10R", "14111507", "CT", "PCE", 48.90m),
        new("Ballpoint pen black dozen", "PEN-BLK-DZ", "44121704", "DZ", "PCE", 6.75m),
        new("Safety glasses clear lens", "SFG-CLR-01", "46181802", "EA", "EAC", 4.20m),
        new("Hex bolt M8 x 40 zinc", "BLT-M8-40Z", "31161501", "BX", "PCE", 18.30m),
        new("Shop towels roll blue", "TWL-SHP-BLU", "47131502", "RL", "EAC", 3.95m),
        new("Surgical mask level 2 bx", "MSK-L2-50", "42131713", "BX", "PCE", 15.60m),
        new("Toner cartridge black 26X", "TNR-BLK-26X", "44103103", "EA", "EAC", 89.00m),
        new("Cable ties 8 in pack 100", "CTI-8IN-100", "39131711", "PK", "PCE", 7.40m),
        new("Hand sanitizer gallon", "SAN-HND-1GL", "53131626", "GL", "GLI", 21.80m),
        new("Packing tape clear 6 rolls", "TPE-PCK-6R", "31201610", "PK", "PCE", 14.25m),
        new("LED tube light 4 ft", "LED-T8-4FT", "39101628", "EA", "EAC", 11.90m),
        new("Leather work gloves L", "GLV-LTH-L", "46181504", "EA", "EAC", 9.50m),
        new("Disinfectant wipes case", "WIP-DIS-CS", "47131829", "CA", "PCE", 38.40m),
        new("Shipping box 12 in bundle", "BOX-1212-25", "24112404", "BX", "PCE", 26.00m),
    ];

    public static readonly IReadOnlyList<string> Merchants =
    [
        "Gulfside Medical Supply", "Northbay Office Works", "Ridgeline Industrial Co", "Harborview Facility Supply", "Summit Lab Products",
    ];

    /// <summary>Merchant category codes that no brand pack excludes (checked against the pack lists).</summary>
    public static readonly IReadOnlyList<string> Mccs = ["5047", "5111", "5065", "5085", "5943", "5198"];

    public static readonly IReadOnlyList<(string From, string To)> PostalCodes =
    [
        ("33602", "33431"), ("30303", "30309"), ("60601", "60616"), ("75201", "75231"), ("98101", "98109"),
    ];

    public static readonly IReadOnlyList<decimal> TaxRates = [0.07m, 0.0825m, 0.06m, 0.0475m];

    public static readonly IReadOnlyList<decimal> Freight = [0.00m, 9.95m, 15.00m, 22.50m];

    public static readonly IReadOnlyList<string> Requestors = ["Dana Ortiz", "Lee Park", "Sam Rivera", "Jordan Ellis"];

    /// <summary>Placeholder descriptions from the general pack's placeholder list (Card brand rules tab, "Placeholder list").</summary>
    public static readonly IReadOnlyList<string> Placeholders = ["Item", "Product", "Goods", "Merchandise", "Services"];

    /// <summary>Well-known public test card numbers, written as people type them.</summary>
    public static readonly IReadOnlyList<string> TestCards = ["4111 1111 1111 1111", "5555 5555 5555 4444", "3782 822463 10005", "6011 1111 1111 1117"];

    public static IReadOnlyList<string> Products(string brand) => brand switch
    {
        "visa" => ["purchasing", "corporate", "business", "fleet"],
        "mastercard" => ["corporate", "purchasing"],
        "amex" => ["corporate", "purchasing", "business"],
        _ => ["corporate", "business", "purchasing"],
    };

    public static string Last4(string brand) => brand switch
    {
        "visa" => "1111",
        "mastercard" => "4444",
        "amex" => "0005",
        _ => "1117",
    };
}
