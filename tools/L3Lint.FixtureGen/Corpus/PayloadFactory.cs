using System.Globalization;
using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen.Corpus;

/// <summary>What a generated payload was built from, so expectations can name its parts.</summary>
/// <param name="Brand">The canonical brand name.</param>
/// <param name="Product">The card product.</param>
/// <param name="Mcc">The merchant category code.</param>
/// <param name="Lines">The number of line items.</param>
/// <param name="Exponent">The currency exponent.</param>
internal sealed record PayloadShape(string Brand, string Product, string Mcc, int Lines, int Exponent);

/// <summary>
/// Builds valid canonical payloads: every brand requirement in the packs is met, and the arithmetic of Card brand
/// rules formulas 1 to 3 holds exactly (line total = quantity x unit cost - discount; amount = line totals + tax +
/// freight + duty - header discount; header tax = sum of line taxes).
/// </summary>
internal static class PayloadFactory
{
    public static (JsonObject Payload, PayloadShape Shape) Build(string brand, Prng random, int number, int lineCount, string currency = "USD")
    {
        var exponent = currency == "JPY" ? 0 : 2;
        var product = random.Pick(Catalog.Products(brand));
        var mcc = random.Pick(Catalog.Mccs);
        var merchant = random.Pick(Catalog.Merchants);
        var (from, to) = random.Pick(Catalog.PostalCodes);
        var rate = random.Pick(Catalog.TaxRates);
        var freight = Round(random.Pick(Catalog.Freight) * (exponent == 0 ? 100 : 1), exponent);
        var reference = "PO-" + (40000 + number * 37 % 50000).ToString(CultureInfo.InvariantCulture);
        var invoice = "INV-" + (60000 + number * 53 % 30000).ToString(CultureInfo.InvariantCulture);

        // Distinct items, so no line repeats another (GEN-DQ-008).
        var items = Catalog.Items.OrderBy(_ => random.Next()).Take(lineCount).ToList();
        var lines = new JsonArray();
        decimal sumTotals = 0, sumTax = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var quantity = random.Between(1, 25);
            var unitCost = Round(item.Price * (exponent == 0 ? 100 : 1), exponent);
            var gross = quantity * unitCost;

            // Line 1 always carries a discount, so the double-discount operator has one to repeat.
            var discount = i == 0 ? Round(Math.Max(gross * 0.05m, exponent == 0 ? 1m : 0.01m), exponent) : (random.Between(0, 3) == 0 ? Round(gross * 0.10m, exponent) : Round(0m, exponent));
            var total = gross - discount;
            var tax = Round(total * rate, exponent);
            sumTotals += total;
            sumTax += tax;
            lines.Add(Line(brand, i, item, quantity, unitCost, discount, tax, rate, total));
        }

        var amount = sumTotals + sumTax + freight;
        var payload = new JsonObject
        {
            ["schemaVersion"] = "1.0",
            ["id"] = invoice,
            ["amount"] = amount,
            ["currency"] = currency,
            ["transactionDate"] = "2026-10-19",
            ["channel"] = "cardNotPresent",
            ["card"] = new JsonObject { ["brand"] = brand, ["product"] = product, ["last4"] = Catalog.Last4(brand) },
            ["merchant"] = Merchant(brand, mcc, merchant),
            ["customer"] = new JsonObject { ["reference"] = reference },
            ["order"] = new JsonObject { ["purchaseOrderNumber"] = reference, ["invoiceNumber"] = invoice },
            ["tax"] = new JsonObject { ["amount"] = sumTax, ["indicator"] = "taxable", ["rate"] = rate },
            ["amounts"] = new JsonObject { ["discount"] = Round(0m, exponent), ["freight"] = freight, ["duty"] = Round(0m, exponent) },
            ["shipping"] = new JsonObject { ["fromPostalCode"] = from, ["toPostalCode"] = to, ["toCountry"] = "US" },
            ["billing"] = new JsonObject { ["postalCode"] = to },
        };
        if (brand == "amex")
        {
            var requestor = random.Pick(Catalog.Requestors);
            payload["amex"] = new JsonObject { ["requestorName"] = requestor, ["adviceText"] = new JsonArray(reference + " supplies order", "Requested by " + requestor) };
        }

        payload["lineItems"] = lines;
        return (payload, new PayloadShape(brand, product, mcc, lineCount, exponent));
    }

    /// <summary>Recomputes header tax and amount after lines changed, keeping formulas 2 and 3 exact.</summary>
    public static void Rebalance(JsonObject payload, int exponent)
    {
        var lines = payload["lineItems"]!.AsArray();
        decimal totals = 0, taxes = 0;
        foreach (var line in lines)
        {
            totals += (decimal)line!["total"]!;
            taxes += (decimal)line["taxAmount"]!;
        }

        payload["tax"]!["amount"] = taxes;
        var amounts = payload["amounts"]!;
        payload["amount"] = Round(totals + taxes + (decimal)amounts["freight"]! + (decimal)amounts["duty"]! - (decimal)amounts["discount"]!, exponent);
    }

    public static decimal Round(decimal value, int exponent) =>
        decimal.Round(value, exponent, MidpointRounding.AwayFromZero) + (exponent == 0 ? 0m : 0.00m);

    private static JsonObject Merchant(string brand, string mcc, string name)
    {
        var merchant = new JsonObject { ["mcc"] = mcc, ["name"] = name, ["country"] = "US" };
        if (brand != "discover")
        {
            merchant["taxId"] = "12-3456789";
        }

        return merchant;
    }

    private static JsonObject Line(string brand, int index, CatalogItem item, int quantity, decimal unitCost, decimal discount, decimal tax, decimal rate, decimal total)
    {
        var line = new JsonObject
        {
            ["sequence"] = index + 1,
            ["description"] = item.Description,
            ["productCode"] = item.ProductCode,
            ["commodityCode"] = item.CommodityCode,
        };
        if (brand is "visa" or "mastercard")
        {
            line["commodityScheme"] = "unspsc";
        }

        line["quantity"] = quantity;
        line["unitOfMeasure"] = brand == "mastercard" ? item.MastercardUnit : item.Unit;
        line["unitCost"] = unitCost;
        line["discountAmount"] = discount;
        if (brand == "mastercard")
        {
            line["discountIndicator"] = discount > 0;
        }

        line["taxAmount"] = tax;
        if (brand is "visa" or "mastercard")
        {
            line["taxRate"] = rate;
            line["taxable"] = true;
        }

        if (brand == "mastercard")
        {
            line["taxTypeCode"] = "01";
        }

        line["total"] = total;
        if (brand == "visa")
        {
            line["detailIndicator"] = "Y";
        }

        if (brand == "mastercard")
        {
            line["debitCreditIndicator"] = "debit";
        }

        return line;
    }
}
