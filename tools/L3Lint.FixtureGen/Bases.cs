using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen;

/// <summary>
/// Valid canonical payloads that rule examples and fixtures start from. Each base meets every documented
/// requirement of its brand as of 2026-10-19, so one edit isolates one problem. All data is synthetic; the
/// Visa base is the spec's own example payload plus the detail indicator and a merchant tax ID.
/// </summary>
internal static class Bases
{
    public static JsonObject Visa() => Parse("""
        {
          "schemaVersion": "1.0",
          "id": "INV-20417",
          "amount": 280.36,
          "currency": "USD",
          "transactionDate": "2026-10-19",
          "channel": "cardNotPresent",
          "card": { "brand": "visa", "product": "purchasing", "last4": "4242" },
          "merchant": { "mcc": "5047", "name": "Gulfside Medical Supply", "country": "US", "taxId": "12-3456789" },
          "customer": { "reference": "PO-77812" },
          "order": { "purchaseOrderNumber": "PO-77812", "invoiceNumber": "INV-20417" },
          "tax": { "amount": 17.36, "indicator": "taxable", "rate": 0.07 },
          "amounts": { "discount": 0.00, "freight": 15.00, "duty": 0.00 },
          "shipping": { "fromPostalCode": "33602", "toPostalCode": "33431", "toCountry": "US" },
          "billing": { "postalCode": "33431" },
          "lineItems": [
            {
              "sequence": 1,
              "description": "Nitrile exam gloves L 100",
              "productCode": "GLV-NTR-L100",
              "commodityCode": "42132203",
              "commodityScheme": "unspsc",
              "quantity": 10,
              "unitOfMeasure": "BX",
              "unitCost": 12.50,
              "discountAmount": 5.00,
              "taxAmount": 8.40,
              "taxRate": 0.07,
              "taxable": true,
              "total": 120.00,
              "detailIndicator": "Y"
            },
            {
              "sequence": 2,
              "description": "Isolation gown blue cs 50",
              "productCode": "GWN-ISO-BL50",
              "commodityCode": "42131606",
              "commodityScheme": "unspsc",
              "quantity": 2,
              "unitOfMeasure": "CA",
              "unitCost": 64.00,
              "discountAmount": 0.00,
              "taxAmount": 8.96,
              "taxRate": 0.07,
              "taxable": true,
              "total": 128.00,
              "detailIndicator": "Y"
            }
          ]
        }
        """);

    public static JsonObject Mastercard() => Parse("""
        {
          "schemaVersion": "1.0",
          "id": "INV-30881",
          "amount": 280.36,
          "currency": "USD",
          "transactionDate": "2026-10-19",
          "channel": "cardNotPresent",
          "card": { "brand": "mastercard", "product": "corporate", "last4": "4444" },
          "merchant": { "mcc": "5047", "name": "Gulfside Medical Supply", "country": "US", "taxId": "12-3456789" },
          "customer": { "reference": "PO-90214" },
          "order": { "purchaseOrderNumber": "PO-90214", "invoiceNumber": "INV-30881" },
          "tax": { "amount": 17.36, "indicator": "taxable", "rate": 0.07 },
          "amounts": { "discount": 0.00, "freight": 15.00, "duty": 0.00 },
          "shipping": { "fromPostalCode": "33602", "toPostalCode": "33431", "toCountry": "US" },
          "billing": { "postalCode": "33431" },
          "lineItems": [
            {
              "sequence": 1,
              "description": "Nitrile exam gloves L 100",
              "productCode": "GLV-NTR-L100",
              "commodityCode": "42132203",
              "commodityScheme": "unspsc",
              "quantity": 10,
              "unitOfMeasure": "EAC",
              "unitCost": 12.50,
              "discountAmount": 5.00,
              "discountIndicator": true,
              "taxAmount": 8.40,
              "taxRate": 0.07,
              "taxable": true,
              "taxTypeCode": "01",
              "total": 120.00,
              "debitCreditIndicator": "debit"
            },
            {
              "sequence": 2,
              "description": "Isolation gown blue cs 50",
              "productCode": "GWN-ISO-BL50",
              "commodityCode": "42131606",
              "commodityScheme": "unspsc",
              "quantity": 2,
              "unitOfMeasure": "PCE",
              "unitCost": 64.00,
              "discountAmount": 0.00,
              "discountIndicator": false,
              "taxAmount": 8.96,
              "taxRate": 0.07,
              "taxable": true,
              "taxTypeCode": "01",
              "total": 128.00,
              "debitCreditIndicator": "debit"
            }
          ]
        }
        """);

    public static JsonObject Amex() => Parse("""
        {
          "schemaVersion": "1.0",
          "id": "INV-41077",
          "amount": 280.36,
          "currency": "USD",
          "transactionDate": "2026-10-19",
          "channel": "cardNotPresent",
          "card": { "brand": "amex", "product": "corporate", "last4": "0005" },
          "merchant": { "mcc": "5047", "name": "Gulfside Medical Supply", "country": "US", "taxId": "12-3456789" },
          "customer": { "reference": "PO-55120" },
          "order": { "purchaseOrderNumber": "PO-55120", "invoiceNumber": "INV-41077" },
          "tax": { "amount": 17.36, "indicator": "taxable", "rate": 0.07 },
          "amounts": { "discount": 0.00, "freight": 15.00, "duty": 0.00 },
          "shipping": { "fromPostalCode": "33602", "toPostalCode": "33431", "toCountry": "US" },
          "billing": { "postalCode": "33431" },
          "amex": { "requestorName": "Dana Ortiz", "adviceText": ["PO-55120 gloves and gowns", "Requested by Dana Ortiz"] },
          "lineItems": [
            {
              "sequence": 1,
              "description": "Nitrile exam gloves L 100",
              "productCode": "GLV-NTR-L100",
              "commodityCode": "42132203",
              "quantity": 10,
              "unitOfMeasure": "BX",
              "unitCost": 12.50,
              "discountAmount": 5.00,
              "taxAmount": 8.40,
              "total": 120.00
            },
            {
              "sequence": 2,
              "description": "Isolation gown blue cs 50",
              "productCode": "GWN-ISO-BL50",
              "commodityCode": "42131606",
              "quantity": 2,
              "unitOfMeasure": "CA",
              "unitCost": 64.00,
              "discountAmount": 0.00,
              "taxAmount": 8.96,
              "total": 128.00
            }
          ]
        }
        """);

    public static JsonObject Discover() => Parse("""
        {
          "schemaVersion": "1.0",
          "id": "INV-52203",
          "amount": 280.36,
          "currency": "USD",
          "transactionDate": "2026-10-19",
          "channel": "cardNotPresent",
          "card": { "brand": "discover", "product": "corporate", "last4": "1117" },
          "merchant": { "mcc": "5047", "name": "Gulfside Medical Supply", "country": "US" },
          "customer": { "reference": "PO-61190" },
          "order": { "purchaseOrderNumber": "PO-61190", "invoiceNumber": "INV-52203" },
          "tax": { "amount": 17.36, "indicator": "taxable", "rate": 0.07 },
          "amounts": { "discount": 0.00, "freight": 15.00, "duty": 0.00 },
          "shipping": { "fromPostalCode": "33602", "toPostalCode": "33431", "toCountry": "US" },
          "billing": { "postalCode": "33431" },
          "lineItems": [
            {
              "sequence": 1,
              "description": "Nitrile exam gloves L 100",
              "productCode": "GLV-NTR-L100",
              "commodityCode": "42132203",
              "quantity": 10,
              "unitOfMeasure": "BX",
              "unitCost": 12.50,
              "discountAmount": 5.00,
              "taxAmount": 8.40,
              "total": 120.00
            },
            {
              "sequence": 2,
              "description": "Isolation gown blue cs 50",
              "productCode": "GWN-ISO-BL50",
              "commodityCode": "42131606",
              "quantity": 2,
              "unitOfMeasure": "CA",
              "unitCost": 64.00,
              "discountAmount": 0.00,
              "taxAmount": 8.96,
              "total": 128.00
            }
          ]
        }
        """);

    public static JsonObject ForBrand(string brand) => brand switch
    {
        "visa" => Visa(),
        "mastercard" => Mastercard(),
        "amex" => Amex(),
        "discover" => Discover(),
        _ => Visa(),
    };

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;
}
