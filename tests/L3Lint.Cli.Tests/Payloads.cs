using System.Text.Json.Nodes;

namespace L3Lint.Cli.Tests;

/// <summary>
/// Test payloads built from the spec's worked example (Card brand rules tab) by one named mutation each, so the
/// expected findings follow from the mutation and the spec, never from running l3lint.
/// </summary>
internal static class Payloads
{
    /// <summary>The public Visa test card number used in PCI tests, written with spaces as a person would type it.</summary>
    public const string TestCardNumber = "4111 1111 1111 1111";

    /// <summary>The same number without spaces; no output may contain either form.</summary>
    public const string TestCardDigits = "4111111111111111";

    /// <summary>How the scrubber masks <see cref="TestCardNumber"/>: asterisks plus the last four digits (Architecture tab, step 6).</summary>
    public const string MaskedTestCardNumber = "************1111";

    /// <summary>Gets the spec example unchanged. It lints with exactly one Visa warning, VISA-L3-010.</summary>
    public static string SpecExample => File.ReadAllText(Cli.SpecExample);

    /// <summary>The spec example with one mutation applied.</summary>
    public static string Mutate(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        mutation(root);
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Line 1 total raised from 120.00 to 125.00: GEN-ARITH-001 at /lineItems/0/total expects 120.00, and GEN-ARITH-002 flags the amount.</summary>
    public static string WrongLineTotal => Mutate(p => p["lineItems"]![0]!["total"] = 125.00m);

    /// <summary>
    /// Line 1 description is the public test card number: GEN-PCI-001 as an error, because a description is free
    /// text (Architecture tab, card-number detection step 5). The whole value is the number; numbers inside a
    /// sentence are a documented limitation (ADR-0023).
    /// </summary>
    public static string WithCardNumber => Mutate(p => p["lineItems"]![0]!["description"] = TestCardNumber);

    /// <summary>The second line has no commodity code: VISA-L3-004 error at /lineItems/1/commodityCode.</summary>
    public static string MissingCommodityCode => Mutate(p => p["lineItems"]![1]!.AsObject().Remove("commodityCode"));

    /// <summary>The spec example with a different invoice ID, for batches.</summary>
    public static string WithId(string id) => Mutate(p => p["id"] = id);

    /// <summary>Compact one-line JSON, for NDJSON files.</summary>
    public static string OneLine(string json) => JsonNode.Parse(json)!.ToJsonString();
}
