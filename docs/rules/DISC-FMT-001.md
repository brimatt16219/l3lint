# DISC-FMT-001: More than 98 line items

| Property | Value |
| --- | --- |
| Severity | error |
| Confidence | processor-doc |
| Level | L3 |
| Brand | discover |
| Impact | blocksLevel3 |
| Effective | always |
| Pack | l3lint.rules.discover 1.0.0 |

## Requirement

Discover transactions may hold at most 98 line items at one large processor.

## Why

More lines cause an error at that processor.

## Check

`maxCount /lineItems (max: 98; profileKey: none)`

## Applies to

- runs at error under profiles jpmorgan or with --strict-profiles, otherwise as a warning

## Message

`The transaction has {count} line items; Discover accepts at most {max}.`

## Fix

Consolidate lines.

## Sources

- <https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards>

## Examples

### Passing example

Two line items.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-52203",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "discover",
    "product": "corporate",
    "last4": "1117"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US"
  },
  "customer": {
    "reference": "PO-61190"
  },
  "order": {
    "purchaseOrderNumber": "PO-61190",
    "invoiceNumber": "INV-52203"
  },
  "tax": {
    "amount": 17.36,
    "indicator": "taxable",
    "rate": 0.07
  },
  "amounts": {
    "discount": 0.00,
    "freight": 15.00,
    "duty": 0.00
  },
  "shipping": {
    "fromPostalCode": "33602",
    "toPostalCode": "33431",
    "toCountry": "US"
  },
  "billing": {
    "postalCode": "33431"
  },
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
```

### Failing example

99 line items.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-52203",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "discover",
    "product": "corporate",
    "last4": "1117"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US"
  },
  "customer": {
    "reference": "PO-61190"
  },
  "order": {
    "purchaseOrderNumber": "PO-61190",
    "invoiceNumber": "INV-52203"
  },
  "tax": {
    "amount": 17.36,
    "indicator": "taxable",
    "rate": 0.07
  },
  "amounts": {
    "discount": 0.00,
    "freight": 15.00,
    "duty": 0.00
  },
  "shipping": {
    "fromPostalCode": "33602",
    "toPostalCode": "33431",
    "toCountry": "US"
  },
  "billing": {
    "postalCode": "33431"
  },
  "lineItems": [
    {
      "sequence": 1,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 2,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 3,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 4,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 5,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 6,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 7,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 8,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 9,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 10,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 11,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 12,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 13,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 14,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 15,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 16,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 17,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 18,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 19,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 20,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 21,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 22,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 23,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 24,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 25,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 26,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 27,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 28,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 29,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 30,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 31,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 32,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 33,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 34,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 35,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 36,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 37,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 38,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 39,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 40,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 41,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 42,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 43,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 44,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 45,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 46,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 47,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 48,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 49,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 50,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 51,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 52,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 53,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 54,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 55,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 56,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 57,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 58,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 59,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 60,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 61,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 62,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 63,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 64,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 65,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 66,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 67,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 68,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 69,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 70,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 71,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 72,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 73,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 74,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 75,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 76,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 77,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 78,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 79,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 80,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 81,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 82,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 83,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 84,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 85,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 86,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 87,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 88,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 89,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 90,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 91,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 92,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 93,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 94,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 95,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 96,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 97,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 98,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    },
    {
      "sequence": 99,
      "description": "Gauze pad 4x4",
      "quantity": 1,
      "unitCost": 0.10,
      "total": 0.10
    }
  ]
}
```
