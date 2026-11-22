# MC-FMT-001: Amount longer than 9 digits

| Property | Value |
| --- | --- |
| Severity | warning |
| Confidence | processor-doc |
| Level | general |
| Brand | mastercard |
| Impact | truncation |
| Effective | always |
| Pack | l3lint.rules.mastercard 1.0.0 |

## Requirement

Amount fields hold 9 digits; longer values are truncated from the right at one large processor.

## Why

A truncated amount no longer matches the invoice.

## Check

`custom (check: AmountDigits)`

## Applies to

- runs at warning under profiles jpmorgan or with --strict-profiles, otherwise as a warning

## Message

`{field} is {actual}, which is {length} digits in minor units; at most {max} fit.`

## Fix

Check the value; it may be truncated.

## Sources

- <https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards>

## Examples

### Passing example

280.36 is 5 digits in minor units.

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

12345678.90 is 10 digits in minor units.

```json
{
  "schemaVersion": "1.0",
  "id": "INV-30881",
  "amount": 12345678.90,
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
