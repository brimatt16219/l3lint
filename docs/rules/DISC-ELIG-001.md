# DISC-ELIG-001: No Discover Level 2 or 3 rate

| Property | Value |
| --- | --- |
| Severity | information |
| Confidence | processor-doc |
| Level | general |
| Brand | discover |
| Impact | noBenefit |
| Effective | always |
| Pack | l3lint.rules.discover 1.0.0 |

## Requirement

Discover has no Level 2 or Level 3 pricing; line items are accepted and passed through.

## Why

The data still reaches the buyer, but no rate benefit applies.

## Check

`custom (check: NoRateProgram)`

## Message

`Discover has no Level 2 or Level 3 rate, so enhanced data earns no lower rate.`

## Fix

None; no Level 2 or 3 rate exists.

## Sources

- <https://experienceleague.adobe.com/en/docs/commerce-merchant-services/payment-services/reporting/levels-card-payment-transactions>
- <https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984>

## Examples

### Passing example

A Visa transaction, so the Discover pack does not run.

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

Any Discover transaction reports this note.

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
