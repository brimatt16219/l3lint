using System.Text.Json.Nodes;
using static L3Lint.FixtureGen.Edits;

namespace L3Lint.FixtureGen;

/// <summary>A rule's passing and failing example, built from a base payload and the edit that breaks it.</summary>
/// <param name="RuleId">The rule ID.</param>
/// <param name="Pack">The pack folder: General, Visa, Mastercard, Amex or Discover.</param>
/// <param name="Pass">Builds the passing payload.</param>
/// <param name="Fail">Builds the failing payload.</param>
internal sealed record ExampleDefinition(string RuleId, string Pack, Func<JsonObject> Pass, Func<JsonObject> Fail);

/// <summary>
/// Pass and fail examples for all 78 rules (ADR-0040), written from the spec's rule catalog. Each failing
/// example changes exactly what the rule's check describes; expectations never come from running l3lint.
/// </summary>
internal static class RuleExampleCatalog
{
    public static IReadOnlyList<ExampleDefinition> All { get; } =
    [
        // GEN (23)
        Gen("GEN-PCI-001", Set("/lineItems/0/description", "4111 1111 1111 1111")),
        Gen("GEN-FMT-001", Set("/lineItems/0/quantity", "ten")),
        Gen("GEN-FMT-002", Set("/currency", "usd")),
        Gen("GEN-FMT-003", Set("/amounts/freight", 15.005m)),
        Gen("GEN-FMT-004", Set("/lineItems/0/description", "Gloves & liners L 100")),
        Gen("GEN-FMT-005", Lines(999, MinimalLine)),
        Gen("GEN-FMT-006", Set("/customer/reference", "PO-77812-2026-SOUTHEAST-REGION")),
        Gen("GEN-FMT-007", Set("/lineItems/1/sequence", 3m)),
        Gen("GEN-ARITH-001", Set("/lineItems/0/total", 125.00m)),
        Gen("GEN-ARITH-002", Set("/amount", 290.36m)),
        Gen("GEN-ARITH-003", Set("/tax/amount", 20.00m)),
        Gen("GEN-ARITH-004", Set("/lineItems/1/quantity", -2m)),
        Gen("GEN-ARITH-005", Set("/amounts/discount", 5.00m)),
        Gen("GEN-ELIG-001", Set("/card/product", "consumer")),
        Gen("GEN-ELIG-002", Remove("/card/product")),
        Gen("GEN-DQ-001", Set("/lineItems/1/description", "Item 2")),
        Gen("GEN-DQ-002", Set("/lineItems/0/description", "Gulfside Medical Supply Inc")),
        Gen("GEN-DQ-003", Set("/lineItems/0/productCode", "000")),
        Gen("GEN-DQ-004", Set("/customer/reference", "111111")),
        Gen("GEN-DQ-005", Lines(1, _ => new JsonObject
        {
            ["sequence"] = 1,
            ["description"] = "Medical supplies order",
            ["productCode"] = "MED-SUP-ORD",
            ["commodityCode"] = "42132203",
            ["quantity"] = 1,
            ["unitOfMeasure"] = "EA",
            ["unitCost"] = 248.00m,
            ["discountAmount"] = 0.00m,
            ["total"] = 248.00m,
            ["detailIndicator"] = "Y",
        })),
        Gen("GEN-DQ-006", Set("/lineItems/0/commodityCode", "42000000")),
        Gen("GEN-DQ-007"),
        Gen("GEN-DQ-008", CopyLine(0, 1)),

        // VISA (26)
        Visa("VISA-ELIG-001", Set("/card/product", "consumer")),
        Visa("VISA-ELIG-002", Set("/merchant/mcc", "5812")),
        Visa("VISA-ELIG-003", Set("/card/product", "commercialPrepaid")),
        new("VISA-ELIG-004", "Visa", () => With(Bases.Visa(), Set("/card/isFederalGovernment", true)), () => With(Bases.Visa(), Set("/card/isFederalGovernment", true), Set("/merchant/mcc", "8062"))),
        Visa("VISA-ELIG-005", Remove("/billing")),
        Visa("VISA-L2-001", Set("/tax/amount", 0.10m)),
        Visa("VISA-L2-002", Remove("/tax/indicator")),
        new("VISA-L2-003", "Visa", () => With(Bases.Visa(), Set("/merchant/mcc", "5541")), () => With(Bases.Visa(), Set("/merchant/mcc", "5541"), Remove("/customer/reference"))),
        Visa("VISA-L3-001", Set("/lineItems", new JsonArray())),
        Visa("VISA-L3-002", Remove("/lineItems/1/description")),
        Visa("VISA-L3-003", Remove("/lineItems/1/productCode")),
        Visa("VISA-L3-004", Remove("/lineItems/1/commodityCode")),
        Visa("VISA-L3-005", Set("/lineItems/1/quantity", 0m)),
        Visa("VISA-L3-006", Remove("/lineItems/1/unitOfMeasure")),
        Visa("VISA-L3-007", Remove("/lineItems/1/unitCost")),
        Visa("VISA-L3-008", Remove("/lineItems/1/total")),
        Visa("VISA-L3-009", Remove("/lineItems/1/discountAmount")),
        Visa("VISA-L3-010", Remove("/lineItems/0/detailIndicator"), Remove("/lineItems/1/detailIndicator")),
        Visa("VISA-L3-011", Remove("/amounts/duty")),
        Visa("VISA-L3-012", Set("/tax/amount", 0.00m)),
        Visa("VISA-L3-013", Remove("/shipping/fromPostalCode")),
        Visa("VISA-L3-014", Set("/lineItems/0/description", "Nitrile exam gloves large box of 100")),
        Visa("VISA-L3-015", Set("/lineItems/0/commodityCode", "4213 2203")),
        Visa("VISA-DQ-001", Set("/lineItems/1/description", "Item")),
        Visa("VISA-DQ-002", Set("/lineItems/0/productCode", "0")),
        Visa("VISA-LT-001", Set("/amount", 25000.00m)),

        // MC (17)
        Mc("MC-ELIG-001", Set("/card/product", "business")),
        Mc("MC-ELIG-002", Set("/card/productCode", "MBS")),
        Mc("MC-ELIG-003", Set("/merchant/mcc", "5812")),
        Mc("MC-L2-001", Set("/tax/amount", 100.00m)),
        Mc("MC-L2-002", Remove("/customer/reference"), Remove("/order/purchaseOrderNumber")),
        Mc("MC-L2-003", Remove("/tax/indicator")),
        Mc("MC-L2-004", Remove("/merchant/taxId")),
        Mc("MC-L3-001", Set("/lineItems", new JsonArray())),
        Mc("MC-L3-002", Remove("/lineItems/1/description")),
        Mc("MC-L3-003", Remove("/lineItems/1/productCode")),
        Mc("MC-L3-004", Set("/lineItems/1/quantity", 0m)),
        Mc("MC-L3-005", Set("/lineItems/0/unitOfMeasure", "BX")),
        Mc("MC-L3-006", Remove("/lineItems/1/unitCost")),
        Mc("MC-L3-007", Remove("/lineItems/1/debitCreditIndicator")),
        Mc("MC-L3-008", Remove("/lineItems/0/taxTypeCode")),
        Mc("MC-L3-009", Remove("/shipping/toPostalCode")),
        Mc("MC-FMT-001", Set("/amount", 12345678.90m)),

        // AMEX (9)
        new("AMEX-ELIG-001", "Amex", Bases.Visa, Bases.Amex),
        Amex("AMEX-FMT-001", Set("/amex/adviceText", new JsonArray("Line one", "Line two", "Line three", "Line four", "Line five"))),
        Amex("AMEX-FMT-002", Lines(5, i => Renumber((JsonObject)Bases.Amex()["lineItems"]![i % 2]!.DeepClone(), i))),
        Amex("AMEX-L2-001", Remove("/shipping/toPostalCode")),
        Amex("AMEX-L2-002", Set("/order/purchaseOrderNumber", "PO-55120-SOUTHEAST-2026")),
        Amex("AMEX-L2-003", Set("/customer/reference", "PO-55120-SOUTHEAST-26")),
        Amex("AMEX-L2-004", Set("/tax/indicator", "exempt")),
        Amex("AMEX-L3-001", Set("/lineItems/0/unitOfMeasure", "EACH")),
        Amex("AMEX-L3-002", Remove("/amex/requestorName")),

        // DISC (3)
        new("DISC-ELIG-001", "Discover", Bases.Visa, Bases.Discover),
        Disc("DISC-FMT-001", Lines(99, MinimalLine)),
        Disc("DISC-L2-001", Remove("/tax/indicator")),
    ];

    /// <summary>Rate table for VISA-LT-001: Large Ticket breaks even with Product 3 at 11,633.33 USD.</summary>
    public const string LargeTicketRates = """
        # Synthetic rate table for the VISA-LT-001 examples. Not real interchange rates.
        name: Example Large Ticket schedule
        effective: 2026-04-18
        currency: USD
        programFees:
          - brand: visa
            when: enhancedDataSent
            percent: 0.05
        categories:
          - brand: visa
            products: [corporate, purchasing]
            tier: product3
            percent: 1.75
            fixed: 0.10
          - brand: visa
            products: [corporate, purchasing]
            tier: largeTicket
            percent: 1.45
            fixed: 35.00
          - brand: visa
            products: [corporate, purchasing]
            tier: cardNotPresent
            percent: 2.70
            fixed: 0.10
        """;

    private static JsonObject Renumber(JsonObject line, int index)
    {
        line["sequence"] = index + 1;
        return line;
    }

    private static ExampleDefinition Gen(string id, params Edit[] fail) => new(id, "General", Bases.Visa, () => With(Bases.Visa(), fail));

    private static ExampleDefinition Visa(string id, params Edit[] fail) => new(id, "Visa", Bases.Visa, () => With(Bases.Visa(), fail));

    private static ExampleDefinition Mc(string id, params Edit[] fail) => new(id, "Mastercard", Bases.Mastercard, () => With(Bases.Mastercard(), fail));

    private static ExampleDefinition Amex(string id, params Edit[] fail) => new(id, "Amex", Bases.Amex, () => With(Bases.Amex(), fail));

    private static ExampleDefinition Disc(string id, params Edit[] fail) => new(id, "Discover", Bases.Discover, () => With(Bases.Discover(), fail));
}
