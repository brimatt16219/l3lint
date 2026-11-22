# AMEX-L3-002: Level 3 prerequisites missing

| Property | Value |
| --- | --- |
| Severity | error |
| Confidence | processor-doc |
| Level | L3 |
| Brand | amex |
| Impact | blocksLevel3 |
| Effective | always |
| Pack | l3lint.rules.amex 1.0.0 |

## Requirement

Amex Level 3 needs the requestor name, customer reference, transaction tax amount and ship-to postal code.

## Why

Acquirers that send structured Amex Level 3 data drop it when a prerequisite is missing.

## Check

`required /amex/requestorName, /customer/reference, /tax/amount, /shipping/toPostalCode (allowZero: true; positiveOnly: false; report: each)`

## Applies to

- only when /lineItems greaterThan 0
- runs at error under profiles mastercard-gateway-amex or with --strict-profiles, otherwise as a warning

## Message

`{field} is missing; Amex Level 3 needs it.`

## Fix

Send the missing fields.

## Sources

- <https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/>

## Examples

### Passing example

Requestor name, customer reference, tax and ship-to are all sent.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-41077",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "amex",
    "product": "corporate",
    "last4": "0005"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-55120"
  },
  "order": {
    "purchaseOrderNumber": "PO-55120",
    "invoiceNumber": "INV-41077"
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
  "amex": {
    "requestorName": "Dana Ortiz",
    "adviceText": [
      "PO-55120 gloves and gowns",
      "Requested by Dana Ortiz"
    ]
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

The requestor name is missing.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-41077",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "amex",
    "product": "corporate",
    "last4": "0005"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-55120"
  },
  "order": {
    "purchaseOrderNumber": "PO-55120",
    "invoiceNumber": "INV-41077"
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
  "amex": {
    "adviceText": [
      "PO-55120 gloves and gowns",
      "Requested by Dana Ortiz"
    ]
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
