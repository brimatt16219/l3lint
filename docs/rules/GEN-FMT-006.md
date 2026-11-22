# GEN-FMT-006: Field longer than the brand or processor maximum

| Property | Value |
| --- | --- |
| Severity | warning |
| Confidence | processor-doc |
| Level | general |
| Brand | all brands |
| Impact | truncation |
| Effective | always |
| Pack | l3lint.rules.general 1.0.0 |

## Requirement

Each field must fit the maximum length in the canonical field dictionary, or the selected processor profile.

## Why

Longer values are truncated in transit, so the buyer sees a cut-off reference or description.

## Check

`maxLength /id, /card/productCode, /merchant/taxId, /customer/reference, /order/purchaseOrderNumber, /order/invoiceNumber, /shipping/fromPostalCode, /shipping/toPostalCode, /billing/postalCode, /amex/requestorName, /lineItems/*/productCode, /lineItems/*/commodityCode, /lineItems/*/unitOfMeasure, /lineItems/*/description, /lineItems/*/taxTypeCode (/id: 64; /card/productCode: 3; /merchant/taxId: 15 (profile key merchantTaxIdMax); /customer/reference: 25 (profile key customerReferenceMax); /order/purchaseOrderNumber: 25 (profile key purchaseOrderNumberMax); /order/invoiceNumber: 25 (profile key invoiceNumberMax); /shipping/fromPostalCode: 10 (profile key postalCodeMax); /shipping/toPostalCode: 10 (profile key postalCodeMax); /billing/postalCode: 10 (profile key postalCodeMax); /amex/requestorName: 40 (profile key requestorNameMax); /lineItems/*/productCode: 12 (profile key productCodeMax); /lineItems/*/commodityCode: 12 (profile key commodityCodeMax); /lineItems/*/unitOfMeasure: 12 (profile key unitOfMeasureMax); /lineItems/*/description: (profile key descriptionMax); /lineItems/*/taxTypeCode: (profile key taxTypeCodeMax))`

## Message

`{field} is {length} characters; the maximum is {max}.`

## Fix

Shorten it; otherwise it is truncated.

## Sources

- <https://docs.stripe.com/payments/payment-line-items>
- <https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards>

## Notes

Default maxima come from the canonical field dictionary; a processor profile can set its own limit for each field.

## Examples

### Passing example

The customer reference is 8 characters.

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

The customer reference is 30 characters, over the maximum of 25.

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
    "reference": "PO-77812-2026-SOUTHEAST-REGION"
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
