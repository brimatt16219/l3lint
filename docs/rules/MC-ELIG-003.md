# MC-ELIG-003: MCC excluded from Mastercard Level 2 or Level 3

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

Some MCCs are excluded from Mastercard enhanced-data rates; Level 3 excludes more than Level 2.

## Why

At these MCCs enhanced data earns no Data Rate II or III.

## Check

`inList /merchant/mcc (list: mastercardLevel2ExcludedMcc, mastercardLevel3ExcludedMcc; mode: deny; caseInsensitive: false; allowMissing: true)`

## Message

`MCC {actual} is excluded from Mastercard enhanced-data rates at Level 3, and at Level 2 when it is on the Level 2 list.`

## Fix

None.

## Sources

- <https://docs.stripe.com/payments/payment-line-items>

## Examples

### Passing example

MCC 5047 is eligible.

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

MCC 5812 is excluded.

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
    "mcc": "5812",
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
