using System.Globalization;
using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen.Corpus;

/// <summary>The result of applying one mutation operator to a valid base payload.</summary>
/// <param name="Description">What changed, in plain words.</param>
/// <param name="Findings">Findings the change must cause, derived from the rule packs and the spec.</param>
/// <param name="Absent">Rules that must stay quiet.</param>
/// <param name="AsOf">The as-of date, when the rule only applies before today's date.</param>
/// <param name="Processor">The processor profile that sets the limit, when the operator needs one.</param>
internal sealed record Mutation(string Description, IReadOnlyList<ExpectedFinding> Findings, IReadOnlyList<string> Absent, string? AsOf = null, string? Processor = null);

/// <summary>
/// The ten mutation operators of the seeded set (Test and delivery tab, "The ten mutation operators"). Each one
/// states the findings it must cause for each brand, from the rule packs: the check that reads the changed field,
/// its severity, and ADR-0016 (rules tied to a processor profile run capped at warning under the generic profile).
/// Where a brand has no rule for the change (Visa and Discover keep no unit list; Amex and Discover have no tax ratio
/// rule), the fixture expects no finding and lists the other brands' rules as absent, so it still tests precision
/// (ADR-0057).
/// </summary>
internal static class Operators
{
    public static readonly IReadOnlyList<(string Name, Func<JsonObject, PayloadShape, Prng, Mutation> Apply)> All =
    [
        ("drop-required", DropRequired),
        ("zero-required", ZeroRequired),
        ("placeholder-description", PlaceholderDescription),
        ("unlisted-unit", UnlistedUnit),
        ("line-total-off-by-one-cent", ShiftLineTotal),
        ("double-discount", DoubleDiscount),
        ("tax-ratio-out-of-range", TaxRatio),
        ("card-number-in-text", CardNumber),
        ("field-too-long", Overflow),
        ("too-many-lines", TooManyLines),
    ];

    private static Mutation DropRequired(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        var line = Line(payload, k);
        switch (shape.Brand)
        {
            case "visa":
                line.Remove("commodityCode");
                line.Remove("commodityScheme");
                return new($"Line {k + 1} has no commodity code.", [new("VISA-L3-004", $"/lineItems/{k}/commodityCode", "error")], []);
            case "mastercard":
                line.Remove("productCode");
                return new($"Line {k + 1} has no product code.", [new("MC-L3-003", $"/lineItems/{k}/productCode", "error")], []);
            case "amex":
                payload["shipping"]!.AsObject().Remove("toPostalCode");
                return new("The ship-to postal code is missing.", [new("AMEX-L2-001", "/shipping/toPostalCode", "warning")], []);
            default:
                payload["tax"]!.AsObject().Remove("indicator");
                return new("The tax indicator is missing.", [new("DISC-L2-001", "/tax/indicator", "warning")], []);
        }
    }

    private static Mutation ZeroRequired(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        Line(payload, k)["quantity"] = 0;

        // Formula 1 no longer holds, because the line total was left as it was.
        var arithmetic = new ExpectedFinding("GEN-ARITH-001", $"/lineItems/{k}/total", "error");
        return shape.Brand switch
        {
            "visa" => new($"Line {k + 1} quantity is zero.", [new("VISA-L3-005", $"/lineItems/{k}/quantity", "error"), arithmetic], []),
            "mastercard" => new($"Line {k + 1} quantity is zero.", [new("MC-L3-004", $"/lineItems/{k}/quantity", "error"), arithmetic], []),
            _ => new($"Line {k + 1} quantity is zero.", [arithmetic], []),
        };
    }

    private static Mutation PlaceholderDescription(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        var placeholder = random.Pick(Catalog.Placeholders);
        Line(payload, k)["description"] = placeholder;
        var path = $"/lineItems/{k}/description";

        // VISA-DQ-001 supersedes GEN-DQ-001 at the same path (visa pack, supersedes).
        return shape.Brand == "visa"
            ? new($"Line {k + 1} description is the placeholder \"{placeholder}\".", [new("VISA-DQ-001", path, "error")], ["GEN-DQ-001"])
            : new($"Line {k + 1} description is the placeholder \"{placeholder}\".", [new("GEN-DQ-001", path, "warning")], []);
    }

    private static Mutation UnlistedUnit(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        Line(payload, k)["unitOfMeasure"] = "QQQ";
        var path = $"/lineItems/{k}/unitOfMeasure";
        return shape.Brand switch
        {
            "mastercard" => new($"Line {k + 1} unit QQQ is not on the Mastercard list.", [new("MC-L3-005", path, "error")], []),
            "amex" => new($"Line {k + 1} unit QQQ is not on the Amex list.", [new("AMEX-L3-001", path, "error")], []),
            _ => new($"Line {k + 1} unit QQQ; {shape.Brand} keeps no unit list, so no rule may fire for it.", [], ["MC-L3-005", "AMEX-L3-001", "GEN-FMT-001"]),
        };
    }

    private static Mutation ShiftLineTotal(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        var line = Line(payload, k);

        // The default tolerance is 0.01 (Card brand rules, arithmetic); one cent beyond it is 0.02.
        line["total"] = (decimal)line["total"]! + 0.02m;
        return new(
            $"Line {k + 1} total is 0.02 above quantity x unit cost - discount; the amount was not changed.",
            [new("GEN-ARITH-001", $"/lineItems/{k}/total", "error"), new("GEN-ARITH-002", "/amount", "error")],
            []);
    }

    private static Mutation DoubleDiscount(JsonObject payload, PayloadShape shape, Prng random)
    {
        decimal lineDiscounts = 0;
        foreach (var line in payload["lineItems"]!.AsArray())
        {
            lineDiscounts += (decimal)line!["discountAmount"]!;
        }

        // The header repeats the line discounts and the amount subtracts them again, so formula 2 still holds.
        payload["amounts"]!["discount"] = lineDiscounts;
        payload["amount"] = (decimal)payload["amount"]! - lineDiscounts;
        return new(
            $"The header discount {lineDiscounts.ToString(CultureInfo.InvariantCulture)} repeats the line discounts and the amount subtracts it twice.",
            [new("GEN-ARITH-005", "/amounts/discount", "warning")],
            ["GEN-ARITH-002"]);
    }

    private static Mutation TaxRatio(JsonObject payload, PayloadShape shape, Prng random)
    {
        // Ratio = tax / amount. Visa's Level 2 range ends at 22% and Mastercard's at 30%; these rates push past them.
        var (rate, maximum) = shape.Brand switch
        {
            "visa" => (0.40m, 0.22m),
            "mastercard" => (0.60m, 0.30m),
            _ => (0.60m, 0.30m),
        };
        foreach (var node in payload["lineItems"]!.AsArray())
        {
            var line = node!.AsObject();
            line["taxAmount"] = PayloadFactory.Round((decimal)line["total"]! * rate, shape.Exponent);
            if (line.ContainsKey("taxRate"))
            {
                line["taxRate"] = rate;
            }
        }

        payload["tax"]!["rate"] = rate;
        PayloadFactory.Rebalance(payload, shape.Exponent);
        var ratio = (decimal)payload["tax"]!["amount"]! / (decimal)payload["amount"]!;
        if (ratio <= maximum + 0.01m)
        {
            throw new InvalidOperationException($"Tax ratio {ratio} does not exceed {maximum}; adjust the operator.");
        }

        var description = $"Tax is {decimal.Round(ratio * 100, 1).ToString(CultureInfo.InvariantCulture)}% of the amount; line taxes and the amount were rebalanced.";
        string[] arithmetic = ["GEN-ARITH-002", "GEN-ARITH-003"];
        return shape.Brand switch
        {
            // VISA-L2-001 applies until the Level 2 sunset (last day 2026-04-17), so the fixture lints as of that day.
            "visa" => new(description, [new("VISA-L2-001", "/tax/amount", "error")], arithmetic, AsOf: "2026-04-17"),
            "mastercard" => new(description, [new("MC-L2-001", "/tax/amount", "error")], arithmetic),
            _ => new(description + $" {shape.Brand} has no tax ratio rule, so none may fire.", [], [.. arithmetic, "VISA-L2-001", "MC-L2-001"]),
        };
    }

    private static Mutation CardNumber(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        var card = random.Pick(Catalog.TestCards);
        Line(payload, k)["description"] = card;

        // A description is free text, so a card number there is an error (Architecture tab, detection step 5).
        return new($"Line {k + 1} description is the public test card number {card}.", [new("GEN-PCI-001", $"/lineItems/{k}/description", "error")], []);
    }

    private static Mutation Overflow(JsonObject payload, PayloadShape shape, Prng random)
    {
        var k = random.Between(0, shape.Lines);
        var code = "PRD-OVERFLOW-" + random.Between(1000, 9999).ToString(CultureInfo.InvariantCulture);
        Line(payload, k)["productCode"] = code;

        // The canonical field dictionary caps product codes at 12 characters (GEN-FMT-006 limits).
        return new($"Line {k + 1} product code {code} has {code.Length} characters; the maximum is 12.", [new("GEN-FMT-006", $"/lineItems/{k}/productCode", "warning")], []);
    }

    private static Mutation TooManyLines(JsonObject payload, PayloadShape shape, Prng random)
    {
        // Limits from the processor profiles and packs: Authorize.net 30, Checkout.com 50, Stripe forwards 4 lines to
        // Amex, and J.P. Morgan takes 98 Discover lines.
        var (processor, count, finding) = shape.Brand switch
        {
            "visa" => ("authorizenet", 31, new ExpectedFinding("GEN-FMT-005", "/lineItems", "error")),
            "mastercard" => ("checkoutcom", 51, new ExpectedFinding("GEN-FMT-005", "/lineItems", "error")),
            "amex" => ("stripe", 5, new ExpectedFinding("AMEX-FMT-002", "/lineItems", "warning")),
            _ => ("jpmorgan", 99, new ExpectedFinding("DISC-FMT-001", "/lineItems", "error")),
        };
        var lines = payload["lineItems"]!.AsArray();
        var template = lines[0]!.AsObject();
        for (var i = lines.Count; i < count; i++)
        {
            var item = Catalog.Items[i % Catalog.Items.Count];
            var copy = template.DeepClone().AsObject();
            var quantity = random.Between(1, 10);
            var unitCost = PayloadFactory.Round(item.Price * (shape.Exponent == 0 ? 100 : 1), shape.Exponent);
            var total = quantity * unitCost;
            copy["sequence"] = i + 1;
            copy["description"] = item.Description;
            copy["productCode"] = item.ProductCode;
            copy["commodityCode"] = item.CommodityCode;
            copy["quantity"] = quantity;
            copy["unitOfMeasure"] = shape.Brand == "mastercard" ? item.MastercardUnit : item.Unit;
            copy["unitCost"] = unitCost;
            copy["discountAmount"] = PayloadFactory.Round(0m, shape.Exponent);
            if (copy.ContainsKey("discountIndicator"))
            {
                copy["discountIndicator"] = false;
            }

            copy["taxAmount"] = PayloadFactory.Round(total * (decimal)payload["tax"]!["rate"]!, shape.Exponent);
            copy["total"] = total;
            lines.Add(copy);
        }

        PayloadFactory.Rebalance(payload, shape.Exponent);
        return new($"{count} line items under the {processor} profile, one more than it accepts.", [finding], [], Processor: processor);
    }

    private static JsonObject Line(JsonObject payload, int index) => payload["lineItems"]![index]!.AsObject();
}
