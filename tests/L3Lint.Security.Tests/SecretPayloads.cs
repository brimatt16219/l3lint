using System.Text.Json.Nodes;

namespace L3Lint.Security.Tests;

/// <summary>A public test card number and the three ways a person might type it.</summary>
/// <param name="Digits">The bare digits, the form a scrubber might leave behind.</param>
/// <param name="Groups">How the number is grouped on the card: 4-4-4-4, or 4-6-5 for American Express.</param>
internal sealed record TestCard(string Digits, int[] Groups)
{
    public string Spaced => Group(" ");

    public string Hyphenated => Group("-");

    /// <summary>The masked form the spec prescribes: asterisks plus the last four digits (Architecture tab, step 6).</summary>
    public string Masked => new string('*', Digits.Length - 4) + Digits[^4..];

    /// <summary>Every raw form that must never reach an output.</summary>
    public IEnumerable<string> RawForms => [Digits, Spaced, Hyphenated];

    private string Group(string separator)
    {
        var parts = new List<string>();
        var at = 0;
        foreach (var length in Groups)
        {
            parts.Add(Digits.Substring(at, length));
            at += length;
        }

        return string.Join(separator, parts);
    }
}

/// <summary>
/// Payloads built from the spec's worked example with distinctive values, so a leak is easy to see. Card numbers are
/// the well-known public test numbers only.
/// </summary>
internal static class SecretPayloads
{
    public const string MerchantName = "ZEBRA-UNIQUE-7731";
    public const string CustomerReference = "PO-QUOKKA-5512";
    public const string Description = "Narwhal gasket kit 8842";

    public static TestCard Visa { get; } = new("4111111111111111", [4, 4, 4, 4]);

    public static TestCard Mastercard { get; } = new("5555555555554444", [4, 4, 4, 4]);

    public static TestCard Amex { get; } = new("378282246310005", [4, 6, 5]);

    public static TestCard Discover { get; } = new("6011111111111117", [4, 4, 4, 4]);

    public static IReadOnlyList<TestCard> Cards { get; } = [Visa, Mastercard, Amex, Discover];

    public static string SpecExample => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    /// <summary>The spec example with distinctive merchant, reference and description values and no card number.</summary>
    public static string Distinctive => Mutate(p =>
    {
        p["merchant"]!["name"] = MerchantName;
        p["customer"]!["reference"] = CustomerReference;
        p["lineItems"]![1]!["description"] = Description;
    });

    /// <summary>
    /// The distinctive payload carrying all four public test card numbers: in free text (description), in reference
    /// fields (customer reference, invoice number) and in a code field (product code), in spaced, hyphenated and
    /// bare forms.
    /// </summary>
    public static string WithCards => Mutate(p =>
    {
        p["merchant"]!["name"] = MerchantName;
        p["lineItems"]![0]!["description"] = Visa.Spaced;
        p["customer"]!["reference"] = Mastercard.Hyphenated;
        p["order"]!["invoiceNumber"] = Amex.Digits;
        p["lineItems"]![1]!["productCode"] = Discover.Digits;
    });

    public static string Mutate(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        mutation(root);
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static string OneLine(string json) => JsonNode.Parse(json)!.ToJsonString();
}
