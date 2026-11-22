# MC-ELIG-002: Card product code MBS or MEF

| Property | Value |
| --- | --- |
| Severity | information |
| Confidence | processor-doc |
| Level | general |
| Brand | mastercard |
| Impact | noBenefit |
| Effective | always |
| Pack | l3lint.rules.mastercard 1.0.0 |

## Requirement

Mastercard product codes MBS (B2B Product 1) and MEF (Electronic Payment Account) are not Level 3 eligible.

## Why

One processor lists these product codes as ineligible for Data Rate III.

## Check

`inList /card/productCode (values: MBS, MEF; mode: deny; caseInsensitive: true; allowMissing: true)`

## Message

`Card product code {actual} is not eligible for Mastercard Level 3.`

## Fix

None; not Level 3 eligible.

## Sources

- <https://resource.payrix.com/docs/interchange-data-requirements>

## Examples

### Passing example

No product code is sent.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-30881",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "mastercard",
    "product": "corporate",
    "last4": "4444"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-90214"
  },
  "order": {
    "purchaseOrderNumber": "PO-90214",
    "invoiceNumber": "INV-30881"
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
```

### Failing example

The product code is MBS.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-30881",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": {
    "brand": "mastercard",
    "product": "corporate",
    "last4": "4444",
    "productCode": "MBS"
  },
  "merchant": {
    "mcc": "5047",
    "name": "Gulfside Medical Supply",
    "country": "US",
    "taxId": "12-3456789"
  },
  "customer": {
    "reference": "PO-90214"
  },
  "order": {
    "purchaseOrderNumber": "PO-90214",
    "invoiceNumber": "INV-30881"
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
```
