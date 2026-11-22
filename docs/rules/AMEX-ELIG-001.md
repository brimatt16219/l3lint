# AMEX-ELIG-001: No public Amex Level 3 rate

| Property | Value |
| --- | --- |
| Severity | information |
| Confidence | processor-doc |
| Level | general |
| Brand | amex |
| Impact | noBenefit |
| Effective | always |
| Pack | l3lint.rules.amex 1.0.0 |

## Requirement

American Express publishes no Level 3 rate program; Level 2 pricing needs a direct agreement with Amex.

## Why

Amex data still matters for the buyer's reconciliation, but no rate benefit should be expected without an agreement.

## Check

`custom (check: NoRateProgram)`

## Message

`American Express has no public Level 3 rate, and Level 2 pricing needs an Amex agreement.`

## Fix

None; no public Level 3 rate, Level 2 pricing needs an Amex agreement.

## Sources

- <https://docs.stripe.com/payments/payment-line-items>
- <https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards>
- <https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984>
- <https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview.md>

## Examples

### Passing example

A Visa transaction, so the Amex pack does not run.

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

Any Amex transaction reports this note.

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
