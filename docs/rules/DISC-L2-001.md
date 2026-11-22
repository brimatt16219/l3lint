# DISC-L2-001: Taxable flag missing

| Property | Value |
| --- | --- |
| Severity | warning |
| Confidence | processor-doc |
| Level | L2 |
| Brand | discover |
| Impact | blocksLevel2 |
| Effective | always |
| Pack | l3lint.rules.discover 1.0.0 |

## Requirement

One large processor marks the taxable flag as required for Discover Level 2.

## Why

Required by J.P. Morgan; every other Discover Level 2 field is optional there.

## Check

`required /tax/indicator (allowZero: false; positiveOnly: false; report: each)`

## Applies to

- runs at warning under profiles jpmorgan or with --strict-profiles, otherwise as a warning

## Message

`The tax indicator is missing.`

## Fix

Send `tax.indicator`.

## Sources

- <https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards>

## Examples

### Passing example

The indicator is taxable.

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

The indicator is missing.

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
