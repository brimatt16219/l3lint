# GEN-DQ-005: One-line invoice that mirrors the total

| Property | Value |
| --- | --- |
| Severity | warning |
| Confidence | inferred |
| Level | L3 |
| Brand | all brands |
| Impact | riskVisaValidation |
| Effective | always |
| Pack | l3lint.rules.general 1.0.0 |

## Requirement

Line items should be the invoice's real lines, not one line restating the total.

## Why

A single line of quantity 1 equal to the amount is the auto-filled pattern that CEDP flags.

## Check

`custom (check: OneLineInvoice)`

## Message

`The only line has quantity 1 and restates the transaction amount, which looks auto-filled.`

## Fix

Send the invoice's real lines.

## Sources

- <https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program>
- <https://www.versapay.com/cedp>

## Examples

### Passing example

The invoice has 2 real lines.

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

One line of quantity 1 whose total equals the amount less tax and freight.

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
      "description": "Medical supplies order",
      "productCode": "MED-SUP-ORD",
      "commodityCode": "42132203",
      "quantity": 1,
      "unitOfMeasure": "EA",
      "unitCost": 248.00,
      "discountAmount": 0.00,
      "total": 248.00,
      "detailIndicator": "Y"
    }
  ]
}
```
