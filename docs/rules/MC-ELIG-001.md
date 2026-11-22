# MC-ELIG-001: Business card earns Level 2 only

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

Mastercard Data Rate III needs a corporate or purchasing card; business cards reach Data Rate II at most.

## Why

Mastercard Level 3 covers purchasing and corporate cards only.

## Check

`inList /card/product (values: business; mode: deny; caseInsensitive: false; allowMissing: true)`

## Message

`This is a business card, so Data Rate II is the best available tier.`

## Fix

None; Data Rate III needs corporate or purchasing.

## Sources

- <https://docs.stripe.com/payments/payment-line-items>

## Examples

### Passing example

The card is a corporate card.

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

The card is a business card.

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
    "product": "business",
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
