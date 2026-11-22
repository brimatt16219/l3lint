# VISA-L3-011: Summary discount, freight or duty missing

| Property | Value |
| --- | --- |
| Severity | error |
| Confidence | processor-doc |
| Level | L3 |
| Brand | visa |
| Impact | blocksLevel3 |
| Effective | from 2025-10-17 |
| Pack | l3lint.rules.visa 1.0.0 |

## Requirement

Product 3 needs the summary discount, freight and duty amounts, 0 for each that does not apply.

## Why

These are among the 12 Product 3 fields; an explicit 0 shows the value was considered.

## Check

`required /amounts/discount, /amounts/freight, /amounts/duty (allowZero: true; positiveOnly: false; report: each)`

## Applies to

- card products: business, corporate, purchasing, fleet
- skipped when the MCC is on: visaProduct3ExcludedMcc
- skipped when /card/isFederalGovernment equals true and /merchant/mcc inList visaFederalExcludedMcc

## Message

`{field} is missing; Visa needs it for Product 3, 0 when it does not apply.`

## Fix

Send 0 for each that does not apply.

## Sources

- <https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program>

## Examples

### Passing example

Discount, freight and duty are all sent.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-20417",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "visa",
    "product": "purchasing",
    "last4": "4242"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-77812"
  },
  "order": {
    "purchaseOrderNumber": "PO-77812",
    "invoiceNumber": "INV-20417"
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
```

### Failing example

Duty is missing.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-20417",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "visa",
    "product": "purchasing",
    "last4": "4242"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-77812"
  },
  "order": {
    "purchaseOrderNumber": "PO-77812",
    "invoiceNumber": "INV-20417"
  },
  "tax": {
    "amount": 17.36,
    "indicator": "taxable",
    "rate": 0.07
  },
  "amounts": {
    "discount": 0.00,
    "freight": 15.00
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
```
