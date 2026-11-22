using System.Text.Json.Nodes;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Tests;

/// <summary>
/// Format detection (FR-005, US-02): gateway adapters never claim canonical payloads, and each gateway's distinctive
/// keys win clearly over the others.
/// </summary>
public sealed class DetectionTests
{
    private static readonly string[] Gateways = ["stripe", "adyen", "braintree", "cybersource", "authorizenet", "checkoutcom"];

    /// <summary>Every canonical payload in the repository: the spec example and the rule pack examples.</summary>
    public static TheoryData<string> CanonicalPayloads()
    {
        var files = new List<string> { Repo.PathOf("tests", "L3Lint.Core.Tests", "SpecExamples", "canonical-example.json") };
        files.AddRange(Directory.EnumerateFiles(Repo.PathOf("src"), "*.json", SearchOption.AllDirectories)
            .Where(f => f.Contains(Path.DirectorySeparatorChar + "examples" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)));
        return [.. files.Select(f => Path.GetRelativePath(Repo.Root, f)).Order(StringComparer.Ordinal)];
    }

    [Theory]
    [MemberData(nameof(CanonicalPayloads))]
    public void Gateway_adapters_do_not_claim_canonical_payloads(string relativePath)
    {
        var text = File.ReadAllText(Path.Combine(Repo.Root, relativePath));
        var withVersion = RawDocument.ParseJson(text, relativePath);
        var root = JsonNode.Parse(text)!.AsObject();
        root.Remove("schemaVersion");
        var withoutVersion = RawDocument.ParseJson(root.ToJsonString(), relativePath);

        foreach (var name in Gateways)
        {
            Linters.Adapter(name).Detect(withVersion).ShouldBe(0.0, name);
            Linters.Adapter(name).Detect(withoutVersion).ShouldBeLessThan(AdapterRegistry.MinimumScore, name);
        }

        Linters.Registry.Resolve(null, withVersion).Name.ShouldBe("canonical");
    }

    [Fact]
    public void There_are_canonical_payloads_to_check() =>
        CanonicalPayloads().Count.ShouldBeGreaterThan(20);

    [Theory]
    [InlineData("""{ "createTransactionRequest": { "merchantAuthentication": {}, "transactionRequest": {} } }""", "authorizenet")]
    [InlineData("""{ "transactionRequest": { "transactionType": "authCaptureTransaction" } }""", "authorizenet")]
    [InlineData("""{ "orderInformation": {}, "clientReferenceInformation": {} }""", "cybersource")]
    [InlineData("""{ "merchantAccount": "M", "amount": { "value": 1, "currency": "USD" }, "lineItems": [ { "unitAmount": "1" } ] }""", "adyen")]
    [InlineData("""{ "amount": 100, "currency": "usd", "payment_details": { "order_reference": "A" } }""", "stripe")]
    [InlineData("""{ "amount": 100, "currency": "USD", "processing": { "order_id": "A" }, "source": {} }""", "checkoutcom")]
    [InlineData("""{ "amount": "1.00", "paymentMethodNonce": "fake-valid-nonce", "lineItems": [ { "kind": "debit", "unitAmount": "1.00" } ] }""", "braintree")]
    [InlineData("""{ "amount": "1.00", "taxAmount": "0.07", "purchaseOrderNumber": "PO-1" }""", "braintree")]
    public void Distinctive_keys_pick_one_adapter(string json, string expected) =>
        Linters.Registry.Resolve(null, RawDocument.ParseJson(json, "probe.json")).Name.ShouldBe(expected);

    [Fact]
    public void A_Braintree_body_with_only_canonical_top_level_keys_is_braintree()
    {
        // amount and lineItems are also canonical key names, but kind and unitAmount are not canonical line keys,
        // so the canonical detector does not claim the body and Braintree wins without a tie.
        var document = RawDocument.ParseJson("""{ "amount": "1.00", "lineItems": [ { "kind": "debit", "unitAmount": "1.00" } ] }""", "probe.json");

        Linters.Registry.Resolve(null, document).Name.ShouldBe("braintree");
        Linters.Registry.Adapters.Single(a => a.Name == "canonical").Detect(document).ShouldBe(0.0);
    }

    [Theory]
    [InlineData("""{ "amount": 100 }""")]
    [InlineData("""{ "hello": "world" }""")]
    [InlineData("""{ "lineItems": [ { "name": "x" } ] }""")]
    public void Unknown_shapes_are_not_claimed(string json)
    {
        var document = RawDocument.ParseJson(json, "probe.json");

        foreach (var name in Gateways)
        {
            Linters.Adapter(name).Detect(document).ShouldBeLessThan(AdapterRegistry.MinimumScore, name);
        }
    }

    [Fact]
    public void Csv_documents_are_claimed_only_by_the_csv_adapter()
    {
        var document = RawDocument.FromCsv("a,b\n1,2\n"u8.ToArray(), "x.csv");

        Linters.Adapter("csv").Detect(document).ShouldBe(1.0);
        Linters.Adapter("csv").Detect(RawDocument.ParseJson("{}", "x.json")).ShouldBe(0.0);
        Linters.Registry.Resolve(null, document).Name.ShouldBe("csv");
    }
}
