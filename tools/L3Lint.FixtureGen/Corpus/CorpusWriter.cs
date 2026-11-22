using System.Globalization;
using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen.Corpus;

/// <summary>
/// Writes the fixture corpus (Test and delivery tab, "Fixture corpus"): the rule examples as canonical fixtures,
/// 400 seeded defects (40 valid bases x 10 operators), 200 known-good payloads and 10 PCI payloads, each with a
/// sibling <c>*.expect.yaml</c>. Expectations come from the operators and the spec, never from running l3lint.
/// </summary>
internal static class CorpusWriter
{
    private const string Today = "2026-10-19";
    private const ulong Seed = 20261019;

    private static readonly string[] Brands = ["visa", "mastercard", "amex", "discover"];

    /// <summary>Example contexts that differ from the payload, mirroring <c>examples.context</c> in the packs.</summary>
    private static readonly Dictionary<string, (string? AsOf, string? Processor, bool Rates)> ExampleContexts = new(StringComparer.Ordinal)
    {
        ["AMEX-FMT-002"] = (null, "stripe", false),
        ["VISA-L2-001"] = ("2026-03-01", null, false),
        ["VISA-L2-002"] = ("2026-03-01", null, false),
        ["VISA-L2-003"] = ("2026-03-01", null, false),
        ["VISA-LT-001"] = (null, null, true),
    };

    public static void Write(FileSetWriter writer)
    {
        WriteRuleExamples(writer);
        WriteSeeded(writer);
        WriteKnownGood(writer);
        WritePci(writer);
    }

    private static void WriteRuleExamples(FileSetWriter writer)
    {
        writer.WriteText("fixtures/canonical/visa/VISA-LT-001.rates.yaml", RuleExampleCatalog.LargeTicketRates + "\n");
        foreach (var definition in RuleExampleCatalog.All)
        {
            var folder = "fixtures/canonical/" + definition.Pack.ToLowerInvariant() + "/";
            var (asOf, processor, rates) = ExampleContexts.GetValueOrDefault(definition.RuleId);
            var ratesPath = rates ? "../VISA-LT-001.rates.yaml" : null;
            foreach (var (valid, payload) in new[] { (true, definition.Pass()), (false, definition.Fail()) })
            {
                var name = folder + (valid ? "valid/" : "invalid/") + definition.RuleId;
                // GEN-DQ-007 is the one batch rule: its fail example repeats 20 times, the default minimum batch
                // (Interfaces tab, config table), and its pass example is linted once, as in rules test.
                var context = ContextOf(payload, asOf, processor, ratesPath) with
                {
                    Only = definition.RuleId,
                    Copies = definition.RuleId == "GEN-DQ-007" && !valid ? 20 : null,
                };
                var expectation = valid
                    ? new Expectation($"{definition.RuleId} pass example: the rule must not fire.", context, null, [], [definition.RuleId])
                    : new Expectation($"{definition.RuleId} fail example: the rule must fire.", context, null, [new ExpectedFinding(definition.RuleId, "*")], []);
                writer.WriteJson(name + ".json", payload);
                writer.WriteText(name + ".expect.yaml", expectation.ToYaml());
            }
        }
    }

    private static void WriteSeeded(FileSetWriter writer)
    {
        var random = new Prng(Seed);
        foreach (var brand in Brands)
        {
            for (var b = 0; b < 10; b++)
            {
                var lineCount = random.Between(2, 6);
                var baseSeed = random.Next();
                for (var o = 0; o < Operators.All.Count; o++)
                {
                    // Every operator starts from the same valid base, rebuilt from its own seed.
                    var (payload, shape) = PayloadFactory.Build(brand, new Prng(baseSeed), 1000 + b + (Array.IndexOf(Brands, brand) * 10), lineCount);
                    var (name, apply) = Operators.All[o];
                    var mutation = apply(payload, shape, new Prng(baseSeed ^ (ulong)(o + 1)));
                    var context = new FixtureContext(brand, shape.Product, shape.Mcc, mutation.AsOf ?? Today, mutation.Processor);
                    var exitCode = mutation.Findings.Any(f => f.Severity == "error") ? 1 : (int?)null;
                    var expectation = new Expectation(mutation.Description, context, exitCode, mutation.Findings, mutation.Absent, Operator: name);
                    var file = string.Create(CultureInfo.InvariantCulture, $"fixtures/seeded/{brand}-{b + 1:00}-{o + 1:00}-{name}");
                    writer.WriteJson(file + ".json", payload);
                    writer.WriteText(file + ".expect.yaml", expectation.ToYaml());
                }
            }
        }
    }

    private static void WriteKnownGood(FileSetWriter writer)
    {
        var random = new Prng(Seed + 1);
        foreach (var brand in Brands)
        {
            for (var i = 0; i < 50; i++)
            {
                // Visa and Mastercard programs are US dollar programs; the other brands also get euro and yen payloads.
                var currency = brand is "amex" or "discover" ? (i % 10) switch { 3 => "EUR", 7 => "JPY", _ => "USD" } : "USD";
                var (payload, shape) = PayloadFactory.Build(brand, new Prng(random.Next()), 2000 + i + (Array.IndexOf(Brands, brand) * 100), random.Between(2, 9), currency);
                var tier = brand switch
                {
                    "visa" or "mastercard" => (brand, "meets"),
                    _ => (brand, "noBenefit"),
                };
                var context = new FixtureContext(brand, shape.Product, shape.Mcc, Today);
                var expectation = new Expectation(
                    $"A valid {brand} {shape.Product} payload with {shape.Lines} lines in {currency}: no error or warning may fire.",
                    context,
                    0,
                    [],
                    [],
                    tier);
                var file = string.Create(CultureInfo.InvariantCulture, $"fixtures/known-good/{brand}-{i + 1:00}");
                writer.WriteJson(file + ".json", payload);
                writer.WriteText(file + ".expect.yaml", expectation.ToYaml());
            }
        }
    }

    private static void WritePci(FileSetWriter writer)
    {
        var random = new Prng(Seed + 2);
        var cases = new (string Brand, string Pointer, JsonNode Value, string Severity, string Note)[]
        {
            ("visa", "/lineItems/0/description", "4111 1111 1111 1111", "error", "a line description"),
            ("mastercard", "/customer/reference", "5555-5555-5555-4444", "error", "the customer reference, with hyphens"),
            ("amex", "/order/purchaseOrderNumber", "378282246310005", "error", "the purchase order number"),
            ("discover", "/lineItems/1/description", "6011 1111 1111 1117", "error", "a line description"),
            ("visa", "/lineItems/1/productCode", "4111111111111111", "warning", "a product code, a code field where a match is a warning"),
            ("mastercard", "/lineItems/0/commodityCode", "5555555555554444", "warning", "a commodity code, a code field"),
            ("visa", "/order/invoiceNumber", JsonValue.Create(4111111111111111m), "error", "the invoice number, sent as a JSON number"),
            ("amex", "/amex/adviceText/0", "3782 822463 10005", "error", "the first advice text line"),
            ("visa", "/merchant/name", "4012 8888 8888 1881", "error", "the merchant name"),
            ("discover", "/customer/reference", "6011-1111-1111-1117", "error", "the customer reference"),
        };
        for (var i = 0; i < cases.Length; i++)
        {
            var (brand, pointer, value, severity, note) = cases[i];
            var (payload, shape) = PayloadFactory.Build(brand, new Prng(random.Next()), 3000 + i, 3);
            Edits.Set(pointer, value.DeepClone())(payload);
            var context = new FixtureContext(brand, shape.Product, shape.Mcc, Today);
            var expectation = new Expectation(
                $"A public test card number in {note}. It must be masked in every output format.",
                context,
                severity == "error" ? 1 : null,
                [new ExpectedFinding("GEN-PCI-001", pointer, severity)],
                []);
            var file = string.Create(CultureInfo.InvariantCulture, $"fixtures/pci/{i + 1:00}-{brand}");
            writer.WriteJson(file + ".json", payload);
            writer.WriteText(file + ".expect.yaml", expectation.ToYaml());
        }
    }

    private static FixtureContext ContextOf(JsonObject payload, string? asOf, string? processor, string? rates)
    {
        var card = payload["card"] as JsonObject;
        var merchant = payload["merchant"] as JsonObject;
        var date = asOf ?? (payload["transactionDate"] as JsonValue)?.ToString() ?? Today;
        return new FixtureContext(
            (card?["brand"] as JsonValue)?.ToString(),
            (card?["product"] as JsonValue)?.ToString(),
            (merchant?["mcc"] as JsonValue)?.ToString(),
            date,
            processor,
            rates);
    }
}
