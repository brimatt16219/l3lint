# AMEX-L3-001: Unit of measure not an ANSI X12 code

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

Amex units of measure are 2-character codes from the ANSI X12 unit list, for example EA, BX, HR or CA.

## Why

Units outside the list are rejected or dropped by acquirers that send structured Amex Level 3 data.

## Check

`inList /lineItems/*/unitOfMeasure (list: amexUom; mode: allow; caseInsensitive: false; allowMissing: true)`

## Message

`Line {index} unit of measure {actual} is not an ANSI X12 code.`

## Fix

Map to a code such as EA.

## Sources

- <https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/>
- <https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html>

## Replaces at the same path

GEN-FMT-006

## Examples

### Passing example

Units are BX and CA.

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

Line 1 unit is EACH, which is not a 2-character code.

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
      "unitOfMeasure": "EACH",
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
