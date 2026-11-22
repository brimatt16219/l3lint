# Gateway adapter field research

Retrieved: 2026-10-25. Public documentation only. Field names and short facts are recorded in our own words; request examples are described by shape, not copied.

Legend for findings:

- **Confirmed**: the spec path (or "Not supported" / "Derived" / "From config") matches what the public docs show.
- **Corrected**: the docs show a different or additional path; the correct current path is given.
- **Unconfirmed**: no public page clearly showed it. These are also listed in the final "Unconfirmed" section.

Cross gateway note: two sources say Visa Level 2 is gone for US domestic. Stripe says Visa Level 2 was sunset in April 2026 and Visa savings now come through Product 3 (CEDP) only [ST1]. Adyen says that from 1 April 2026 Visa supports Level 3 only while Mastercard supports Level 2 and 3 [AD1].

## Sources consulted

Stripe

- [ST1] https://docs.stripe.com/payments/payment-line-items : L2/L3 field table, formats, line item cap, sample requests, arithmetic validation.
- [ST2] https://docs.stripe.com/api/payment_intents/create?query=amount_details : API reference for every `amount_details` child (types, max lengths, mutual exclusions).
- [ST3] https://docs.stripe.com/api/payment_intents/create?query=payment_details : `customer_reference`, `order_reference`.
- [ST4] https://docs.stripe.com/api/payment_intents/create?query=shipping : top level `shipping.address.*` (country, postal_code).
- [ST0] https://docs.stripe.com/api/payment_intents/create : `amount` (integer, minor units, up to 8 digits), `currency`.

Adyen

- [AD1] https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 (read via the `.md` variant of the same URL) : current L2/L3 field table, `enhancedSchemeData.levelTwoThree` request object, examples, amount rules, drop behaviour.
- [AD2] https://docs.adyen.com/api-explorer/Checkout/71/post/payments : API explorer; `levelTwoThree` fields with limits and the matching legacy `additionalData` key for each.
- [AD3] https://raw.githubusercontent.com/Adyen/adyen-java-api-library/main/src/main/java/com/adyen/model/checkout/AdditionalDataLevel23.java : Adyen's public generated SDK model for the legacy flat `additionalData` L2/L3 keys (exact key strings, orderDate format, minor units).
- [AD4] https://docs.adyen.com/online-payments/classic-integrations/hosted-payment-pages/hpp-payments-with-l2l3-data : end of life classic page; only literal example of flat keys with `itemDetailLine1`.
- [AD5] https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data.md : ESD overview; says ESD parameters change when upgrading to Checkout API v72.

Braintree

- [BT1] https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ : L2/L3 required fields, amount validation rules (.NET).
- [BT2] https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/node : same page for Node; used for the camelCase request shape.
- [BT3] https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node : transaction sale parameter reference (types, max lengths, line item cap, `kind` values, currency source).
- [BT4] https://raw.githubusercontent.com/braintree/graphql-api/refs/heads/master/schema.graphql : GraphQL schema linked from https://developer.paypal.com/braintree/graphql/reference/ ; used for `TransactionLineItemInput`, `TransactionTaxInput`, `TransactionShippingInput`.

Cybersource

- [CY1] https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview.md : L2/L3 overview (Visa Platform Connect guide).
- [CY2] https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3.html : full Visa Platform Connect REST L2/L3 guide; required and optional field lists per transaction type; example shapes; truncation rule.
- [CY3] https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-processing-ctv/l23-l3-processing-sale-intro/l23-l3-processing-sale-ex-rest-ctv.html : Level III sale request example.
- [CY4] https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/barclays/rest/level-2-3/l23-intro-overview/l23-l3-data/l23-processors-transactions-l3-info.html : line item cap (Barclays).
- [CY5] https://developer.cybersource.com/docs/cybs/en-us/payments/developer/ctv/rest/payments/payments-processing-basic-intro/payments-processing-basic-auth-lineitem-intro.html : authorization with line items; `totalAmount` may be replaced by line items.
- [CY6] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-gross-net-indicator.md : `orderInformation.lineItems[].amountIncludesTax`.
- [CY7] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-code.md : `orderInformation.lineItems[].productCode`.
- [CY8] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-sku.html : `orderInformation.lineItems[].productSKU`.
- [CY9] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/purchase-totals-tax-amount.md : `orderInformation.amountDetails.taxAmount`.
- [CY10] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-user-po.md : `orderInformation.invoiceDetails.purchaseOrderNumber`.
- [CY11] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-taxable.md : `orderInformation.invoiceDetails.taxable`.
- [CY12] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-from-postal-code.md : `orderInformation.shippingDetails.shipFromPostalCode`.
- [CY13] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-postal-code.md : `orderInformation.shipTo.postalCode`.
- [CY14] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-country.md : `orderInformation.shipTo.country`.
- [CY15] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/merchant-reference-code.md : `clientReferenceInformation.code`.
- [CY16] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-invoice-number.md : `orderInformation.lineItems[].invoiceNumber`.
- [CY17] https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-sign.md : `item_#_sign` (no REST equivalent).
- [CY18] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationLineItems.md : public .NET SDK docs, line item model.
- [CY19] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationAmountDetails.md : amount details model.
- [CY20] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationInvoiceDetails.md : invoice details model.
- [CY21] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationShippingDetails.md : shipping details model (ship from).
- [CY22] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationShipTo.md : ship to model.
- [CY23] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationAmountDetailsTaxDetails.md : tax details model.
- [CY24] https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-node/master/src/model/Ptsv2paymentsOrderInformationLineItems.js : public Node SDK model; exact JSON key casing.
- [CY25] https://developer.cybersource.com/api-reference-assets/index.html : landing page only; the interactive reference did not render for a text fetch.

Authorize.net

- [AN1] https://developer.authorize.net/api/reference/index.html : "Charge a Credit Card" request field table, L2/L3/CEDP notes, 30 line item cap, JSON example, element order rule.
- [AN2] https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd : public API schema; `lineItemType`, `orderType`, `extendedAmountType`, `nameAndAddressType`, `transactionRequestType` element order and restrictions.
- [AN3] https://rubydoc.info/gems/authorizenet/AuthorizeNet/API/LineItemType : public Ruby SDK docs; `LineItemType` attribute list including the L2/L3 extension.

Checkout.com

- [CK1] https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data : L2/L3 field table, limits, item cap, examples, amount rules. (Search also shows this page at `/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data`.)
- [CK2] https://www.checkout.com/docs/business-operations/terminology : top level payment `reference` (merchant identifier such as an order number, 80 chars).
- [CK3] https://www.checkout.com/docs/payments/accept-payments/format-the-amount-value : amount value rules (whole number, max 9 digits).
- [CK4] https://api-reference.checkout.com/ and https://api-reference.checkout.com/tag/Payments/ : interactive reference did not render for a text fetch; not usable.
- [CK5] https://raw.githubusercontent.com/checkout/checkout-sdk-net/master/src/CheckoutSdk/Payments/ProcessingSettings.cs : public .NET SDK `processing` model (field names, `order_id` 15 chars).

---

## Stripe (PaymentIntent)

### Field mapping

| Canonical field | Spec mapping | Finding | Source |
|---|---|---|---|
| amount | `amount` (minor) | Confirmed. Integer, smallest currency unit, up to 8 digits. | ST0 |
| currency | `currency` | Confirmed. 3 letter ISO code, lowercase. | ST0 |
| tax.amount | `amount_details.tax.total_tax_amount` | Confirmed. Integer minor, >= 0. Mutually exclusive with line level `tax.total_tax_amount`. | ST1, ST2 |
| tax.indicator | Derived from tax | Confirmed. No tax exempt or taxable flag exists in `amount_details` or `payment_details`. | ST2, ST3 |
| customer.reference | `payment_details.customer_reference` | Confirmed. String; cards only. | ST3 |
| order.invoiceNumber | `payment_details.order_reference` | Confirmed. String; required for L2 and L3 rates. | ST3 |
| amounts.freight | `amount_details.shipping.amount` | Confirmed. Integer minor, >= 0. | ST2 |
| amounts.duty | Not supported | Confirmed. No duty field. | ST2 |
| amounts.discount | `amount_details.discount_amount` | Confirmed. Integer minor, > 0. Mutually exclusive with line `discount_amount`. | ST2 |
| amounts.shippingTax | Not supported | Confirmed. No shipping tax field. | ST2 |
| shipping.fromPostalCode | `amount_details.shipping.from_postal_code` | Confirmed. Max 10. | ST2 |
| shipping.toPostalCode | `amount_details.shipping.to_postal_code` | Confirmed. Max 10. | ST2 |
| shipping.toCountry | Not supported | Confirmed for the L2/L3 block (no country in `amount_details.shipping`). Note: the general top level `shipping.address.country` (ISO alpha-2) exists, but the L2/L3 guide does not list it. | ST2, ST4 |
| order.orderDate | Not supported | Confirmed. | ST2 |
| Line items array | `amount_details.line_items[]` | Confirmed. Max 200 items. | ST1, ST2 |
| description | `product_name` | Confirmed. Required on every line item. | ST2 |
| productCode | `product_code` | Confirmed. Max 12. | ST2 |
| commodityCode | `payment_method_options.card.commodity_code` | Confirmed. Per line item, full path `amount_details.line_items[].payment_method_options.card.commodity_code`. Not request level. A parallel `card_present.commodity_code` also exists. | ST1, ST2 |
| quantity | `quantity` | Confirmed. Integer > 0. Required on every line item. | ST2 |
| unitOfMeasure | `unit_of_measure` | Confirmed. Max 12. | ST2 |
| unitCost | `unit_cost` (minor) | Confirmed. Integer minor, >= 0. Required on every line item. | ST2 |
| discountAmount | `discount_amount` | Confirmed. Integer minor, > 0. | ST2 |
| taxAmount (line) | `tax.total_tax_amount` | Confirmed. Integer minor, >= 0. | ST2 |
| total (line) | Derived | Confirmed. No line total field. | ST2 |
| amountIncludesTax | false | Confirmed. Line tax is a separate field and the documented Klarna line formula adds it on top of `unit_cost x quantity - discount_amount`. | ST1 |
| debitCreditIndicator | debit | Confirmed. No line credit indicator. | ST2 |

### Units

- Every amount is an integer in the smallest currency unit: `amount`, `amount_details.tax.total_tax_amount`, `amount_details.shipping.amount`, `amount_details.discount_amount`, and per line `unit_cost`, `discount_amount`, `tax.total_tax_amount` [ST2].
- `quantity` is an integer > 0 [ST2].
- Line items nest under `amount_details.line_items[]`. Card specific line data nests under each line's `payment_method_options.card` [ST2].
- Requests are form encoded (`amount_details[line_items][0][product_code]=...`); l3lint will see the JSON equivalent of the same tree.

### Limits

| Field | Limit | Source |
|---|---|---|
| line items | max 200; American Express only receives the first 4 | ST1, ST2 |
| `product_name` | max 1024; cards truncate to 26 alphanumeric chars | ST2 |
| `product_code` | max 12 | ST2 |
| `unit_of_measure` | max 12, alphanumeric | ST1, ST2 |
| `commodity_code` | max 12, alphanumeric without spaces | ST1, ST2 |
| `from_postal_code`, `to_postal_code` | max 10; alphanumeric, hyphens and spaces allowed | ST2 |
| `customer_reference`, `order_reference` | no hard max in the reference; truncated to 25 alphanumeric chars (spaces excluded) before going to card networks | ST3 |
| `amount` | up to 8 digits | ST0 |
| L2 sales tax rate | Mastercard 0.1% to 30%, Visa 0.1% to 22% (some MCC exceptions); L3 allows 0 | ST1 |

Arithmetic: by default Stripe rejects (HTTP 400) line data that does not add up to `amount`. `amount_details.enforce_arithmetic_validation=false` lets the request through, returns the problem in `amount_details.error`, and Stripe then does not send the line data to networks [ST1, ST2]. The published examples satisfy: `amount = sum(unit_cost x quantity - line discount + line tax) + shipping.amount - discount_amount + tax.total_tax_amount`.

### Example shape (ST1, "Sample request (Level 3 and Product 3 data)")

POST `/v1/payment_intents`, form encoded. JSON equivalent tree:

- `amount`: integer (minor)
- `currency`: string, lowercase ("usd")
- `automatic_payment_methods.enabled`: boolean
- `payment_details.customer_reference`: string
- `payment_details.order_reference`: string
- `amount_details.shipping.from_postal_code`: string (5 digit ZIP)
- `amount_details.shipping.to_postal_code`: string (5 digit ZIP)
- `amount_details.shipping.amount`: integer (minor)
- `amount_details.line_items`: array of 2 objects, each with
  - `product_code`: string (SKU style)
  - `product_name`: string
  - `unit_cost`: integer (minor)
  - `quantity`: integer
  - `tax.total_tax_amount`: integer (minor)
  - `unit_of_measure`: string (a plain word in the example, such as a length or volume unit name)
  - `payment_method_options.card.commodity_code`: string of digits
- `expand`: array with `amount_details.line_items` (only affects the response)

The example's `amount` equals the sum of each line's `unit_cost x quantity + tax.total_tax_amount` plus `shipping.amount`. A second sample ("Using top-level discount or tax") uses order level `amount_details.discount_amount` and `amount_details.tax.total_tax_amount` with lines that carry no tax or discount.

### Detection keys

- `amount_details` and/or `payment_details` objects (Stripe only).
- Top level `amount` is an integer and `currency` is lowercase.
- Other Stripe only keys: `automatic_payment_methods`, `payment_method`, `confirm`, `capture_method`.
- Caution: Stripe also has a top level `shipping.address` (keys `postal_code`, `country`), which overlaps with Checkout.com's `shipping.address` (keys `zip`, `country`).

### Notes on ambiguity

- `unit_of_measure` in Stripe's own examples is a free word, not a code. Adyen caps the same field at 3 chars, so cross gateway rules on unit codes should not assume Stripe sends codes.
- `customer_reference` and `order_reference` have no documented hard max; only the 25 char network truncation.

---

## Adyen (/payments)

### Structural finding

The current L2/L3 guide [AD1] sends Level 2/3 data as a structured top level body object, not as `additionalData` keys:

- `enhancedSchemeData.levelTwoThree.{customerReferenceNumber, totalTaxAmount, freightAmount, dutyAmount, shipFromPostalCode, orderDate, destination.{postalCode, stateOrProvince, countryCode}, itemDetailLines[].{...}}`.
- Values are JSON numbers for amounts and quantity, and `itemDetailLines` is a real JSON array.

The v71 API explorer [AD2] lists this object and, for each field, the matching legacy flat `additionalData` key. The legacy keys (the spec's "ESD" keys) are still defined in Adyen's public generated library model `AdditionalDataLevel23` [AD3], with string values. The ESD overview [AD5] says ESD parameters change when upgrading to Checkout API v72. The relation between the two forms (which wins, whether they can be mixed) is not documented.

Recommendation for the adapter: accept both shapes. The table below confirms the spec's legacy keys and gives the new path for each.

### Field mapping

| Canonical field | Spec mapping | Finding (legacy key / new path) | Source |
|---|---|---|---|
| amount | `amount.value` (minor) | Confirmed. Integer, minor units. | AD1, AD2 |
| currency | `amount.currency` | Confirmed. | AD1 |
| tax.amount | ESD `totalTaxAmount` | Confirmed: `enhancedSchemeData.totalTaxAmount`. New: `enhancedSchemeData.levelTwoThree.totalTaxAmount`. | AD2, AD3 |
| tax.indicator | Derived from tax | Confirmed. No tax exempt flag in either form. | AD1, AD3 |
| customer.reference | ESD `customerReference` | Confirmed: `enhancedSchemeData.customerReference`. New name differs: `levelTwoThree.customerReferenceNumber`. | AD2, AD3 |
| order.invoiceNumber | `reference` | Confirmed. Top level merchant `reference` is present in every example. It is the payment reference, not a dedicated L2/L3 invoice field. | AD1 |
| amounts.freight | ESD `freightAmount` | Confirmed: `enhancedSchemeData.freightAmount`. New: `levelTwoThree.freightAmount`. | AD2, AD3 |
| amounts.duty | ESD `dutyAmount` | Confirmed: `enhancedSchemeData.dutyAmount`. New: `levelTwoThree.dutyAmount`. | AD2, AD3 |
| amounts.discount | Not supported | Confirmed. Only line level `discountAmount` exists. | AD1, AD3 |
| amounts.shippingTax | Not supported | Confirmed. | AD1, AD3 |
| shipping.fromPostalCode | ESD `shipFromPostalCode` | Confirmed: `enhancedSchemeData.shipFromPostalCode`. New: `levelTwoThree.shipFromPostalCode`. | AD2, AD3 |
| shipping.toPostalCode | ESD `destinationPostalCode` | Confirmed: `enhancedSchemeData.destinationPostalCode`. New: `levelTwoThree.destination.postalCode`. | AD2, AD3 |
| shipping.toCountry | ESD `destinationCountryCode` | Confirmed: `enhancedSchemeData.destinationCountryCode` (ISO alpha-2 or alpha-3). New: `levelTwoThree.destination.countryCode`. | AD2, AD3 |
| order.orderDate | ESD `orderDate` (DDMMYY) | Confirmed for legacy: `enhancedSchemeData.orderDate`, format `ddMMyy`, 6 chars. New `levelTwoThree.orderDate` uses `yyyy-MM-dd` (10 chars). | AD3, AD1, AD2 |
| Line items array | ESD `itemDetailLine1.*`, `itemDetailLine2.*` | Confirmed for legacy. Documented as `enhancedSchemeData.itemDetailLine[itemNr].<field>` [AD3] or `[N]` [AD2]; the only literal example uses `itemDetailLine1` [AD4]. New: `levelTwoThree.itemDetailLines[]` array. | AD2, AD3, AD4 |
| description | `description` | Confirmed. Max 26. | AD2, AD3 |
| productCode | `productCode` | Confirmed. Max 12. | AD2, AD3 |
| commodityCode | `commodityCode` | Confirmed. Max 12. | AD2, AD3 |
| quantity | `quantity` | Confirmed. Integer > 0, max 9999. | AD2, AD3 |
| unitOfMeasure | `unitOfMeasure` | Confirmed. Max 3. | AD2, AD3 |
| unitCost | `unitPrice` (minor) | Confirmed. Minor units; must not be all zeros (legacy). | AD2, AD3 |
| discountAmount | `discountAmount` | Confirmed. Minor units; optional. | AD2, AD3 |
| taxAmount (line) | Not supported | Confirmed. No line tax key in either form. | AD2, AD3 |
| total (line) | `totalAmount` | Confirmed. Minor units; must not be all zeros (legacy). | AD2, AD3 |
| amountIncludesTax | false | Confirmed for the current guide: line `totalAmount = quantity x unitPrice - discountAmount` (Visa). See ambiguity note on the classic example. | AD1 |
| debitCreditIndicator | debit | Confirmed. No line credit indicator. | AD2, AD3 |

### Units

- `amount.value`: integer, minor units [AD2].
- Legacy `additionalData` ESD amounts (`totalTaxAmount`, `freightAmount`, `dutyAmount`, line `unitPrice`, `discountAmount`, `totalAmount`): minor units, sent as numeric strings, max 12 chars [AD3]. `additionalData` is a string to string map, so every value is a JSON string.
- New `levelTwoThree` amounts: JSON integers in minor units, max value 10000000000 [AD1, AD2]. `quantity` is a JSON integer.

### Limits

| Field | Limit | Source |
|---|---|---|
| customerReference / customerReferenceNumber | max 25 ASCII; no leading space; not all spaces or zeros | AD1, AD2 |
| totalTaxAmount | L2: must not be all zeros; L3: may be zero | AD2, AD3 |
| destinationPostalCode / destination.postalCode | max 10 ASCII; US 5 or 9 digits; Canada 6 chars | AD1, AD2 |
| destinationStateProvinceCode / destination.stateOrProvince | max 3 ASCII | AD1, AD2 |
| destinationCountryCode / destination.countryCode | 2 or 3 chars (ISO 3166-1 alpha-2 or alpha-3) | AD2 |
| shipFromPostalCode | max 10 ASCII | AD1, AD2 |
| orderDate | legacy `ddMMyy` (6 chars); new `yyyy-MM-dd` (10 chars) | AD3, AD2 |
| line description | max 26 ASCII; not one char, blank or only special chars; for Visa must not look like the merchant name or be generic words | AD1, AD2 |
| line productCode, commodityCode | max 12 ASCII | AD2, AD3 |
| line unitOfMeasure | max 3 ASCII | AD2, AD3 |
| line quantity | integer > 0, max 9999 | AD1, AD2 |
| number of item lines | not documented | (Unconfirmed) |
| L2 tax rate | min 0.1% (not for L3); max 30% Mastercard, 22% Visa | AD1 |

Line numbering: the documented placeholder is `itemDetailLine[itemNr]`, and the one literal example uses `itemDetailLine1`, which implies numbering starts at 1. No page states the base or a maximum count.

Validation and drop behaviour [AD1]:

- Visa: line `totalAmount = quantity x unitPrice - discountAmount`, and `amount.value = sum(line totalAmount) + freightAmount + totalTaxAmount`. `dutyAmount` is not in the formula.
- A missing or bad required field drops all L2/3 data. L3 only errors fall back to L2. A malformed optional field is dropped alone.
- Send L2/3 data again in the capture when the capture amount differs from the payment amount.

### Example shape (AD1, /payments, Level 3)

- `amount.currency`: string; `amount.value`: integer (minor)
- `reference`: string
- `paymentMethod`: object (`type`, `number`, `expiryMonth`, `expiryYear`, `cvc`, `holderName`, all strings)
- `returnUrl`: string
- `merchantAccount`: string
- `enhancedSchemeData.levelTwoThree`:
  - `customerReferenceNumber`: string
  - `totalTaxAmount`, `freightAmount`, `dutyAmount`: integers (minor)
  - `shipFromPostalCode`: string
  - `orderDate`: string `yyyy-MM-dd`
  - `destination`: object with `postalCode`, `stateOrProvince`, `countryCode` strings
  - `itemDetailLines`: array of 2 objects, each with `description`, `productCode`, `commodityCode` (strings), `quantity` (integer), `unitOfMeasure` (string, 3 chars), `unitPrice`, `discountAmount`, `totalAmount` (integers, minor)

Arithmetic in the example: each line `totalAmount = quantity x unitPrice - discountAmount`; `amount.value = sum(totalAmount) + freightAmount + totalTaxAmount`; `dutyAmount` is present but not added. The `/payments/{paymentPspReference}/captures` example uses the same object with `merchantAccount`, `amount`, `reference` at top level and no `paymentMethod`.

Legacy flat shape (for building the spec's ESD form): put the same data inside `additionalData` as string values under keys `enhancedSchemeData.customerReference`, `enhancedSchemeData.totalTaxAmount`, `enhancedSchemeData.freightAmount`, `enhancedSchemeData.dutyAmount`, `enhancedSchemeData.orderDate` (`ddMMyy`), `enhancedSchemeData.shipFromPostalCode`, `enhancedSchemeData.destinationPostalCode`, `enhancedSchemeData.destinationStateProvinceCode`, `enhancedSchemeData.destinationCountryCode`, and per line `enhancedSchemeData.itemDetailLine1.{commodityCode, description, productCode, quantity, unitOfMeasure, unitPrice, discountAmount, totalAmount}` [AD3, AD4]. The only published literal example of this form is the end of life HPP page [AD4], which shows one line, a 6 digit `orderDate`, a 2 letter destination country, and every value as a string.

### Detection keys

- `merchantAccount` (string) at top level, and `amount` as an object with `value` and `currency`.
- L2/L3 is in either `additionalData` keys starting with `enhancedSchemeData.` or the body object `enhancedSchemeData.levelTwoThree`.
- Caution: Adyen also has a top level `lineItems` array (used for open invoice and BNPL methods), so `lineItems` alone must not be taken as Braintree.

### Notes on ambiguity

- Two request forms exist (legacy flat keys and the new structured object) with different names for the customer reference (`customerReference` vs `customerReferenceNumber`) and different `orderDate` formats (`ddMMyy` vs `yyyy-MM-dd`).
- The classic HPP example [AD4] has a single line whose `totalAmount` equals `unitPrice` plus `totalTaxAmount`, which suggests tax inside the line total. The current guide's formula excludes tax. Treat the current guide as authoritative.
- The current capture example in [AD1] has an internal arithmetic error on line 2 (discount changed, total not changed). Do not copy it as a passing fixture.
- AD1 front matter shows a 2023 last modified date but the content mentions April 2026 changes; the content is current.

---

## Braintree (transaction sale)

The spec column matches the server SDK request (camelCase, as in the Node SDK). The GraphQL API uses a different nesting; see notes.

### Field mapping

| Canonical field | Spec mapping | Finding | Source |
|---|---|---|---|
| amount | `amount` | Confirmed. Decimal string, greater than 0, in the merchant account currency format. | BT3 |
| currency | From config | Confirmed. No currency parameter; currency comes from `merchantAccountId` (or the default merchant account). | BT3 |
| tax.amount | `taxAmount` | Confirmed. Decimal string, not negative; L2 field. Does not add to the charged amount. | BT3 |
| tax.indicator | `taxExempt` | Confirmed. Boolean; L2 field. | BT3, BT2 |
| customer.reference | `purchaseOrderNumber` | Confirmed. String; L2 field. | BT3 |
| order.invoiceNumber | `orderId` | Confirmed as a field (string, max 255, 127 for PayPal). The docs do not call it an L2/L3 field. | BT3 |
| amounts.freight | `shippingAmount` | Confirmed. Decimal string; L3 field. | BT3 |
| amounts.duty | Not supported | Confirmed. No duty parameter in the sale reference or GraphQL inputs. | BT3, BT4 |
| amounts.discount | `discountAmount` | Confirmed. Decimal string; L3 field. | BT3 |
| amounts.shippingTax | `shippingTaxAmount` | Confirmed. Decimal string; L3 field. | BT3 |
| shipping.fromPostalCode | `shipsFromPostalCode` | Confirmed. String; L3 field. | BT3 |
| shipping.toPostalCode | `shipping.postalCode` | Confirmed. Object key is `shipping` (not `shippingAddress`). | BT3 |
| shipping.toCountry | `shipping.countryCodeAlpha2` | Confirmed. Also accepted: `shipping.countryCodeAlpha3`, `shipping.countryCodeNumeric`, `shipping.countryName`. The L2/L3 examples use `countryCodeAlpha3`. | BT3, BT2 |
| order.orderDate | Not supported | Confirmed. | BT3 |
| Line items array | `lineItems[]` | Confirmed. Up to 249 items. | BT3 |
| description | `name` | Confirmed. `name` is the L3 item name (max 35). A separate `description` (max 127) also exists. | BT1, BT3 |
| productCode | `productCode` | Confirmed. Max 12 (127 for PayPal). | BT3 |
| commodityCode | `commodityCode` | Confirmed. Max 12. | BT3 |
| quantity | `quantity` | Confirmed. Decimal string, up to 4 decimals, > 0. | BT3 |
| unitOfMeasure | `unitOfMeasure` | Confirmed. Max 12. | BT3 |
| unitCost | `unitAmount` | Confirmed. Decimal string, up to 4 decimals (2 for PayPal), > 0. | BT3 |
| discountAmount | `discountAmount` | Confirmed. Decimal string, up to 2 decimals. | BT3 |
| taxAmount (line) | `taxAmount` | Confirmed. Decimal string, up to 2 decimals. | BT3 |
| total (line) | `totalAmount` | Confirmed. Decimal string, up to 2 decimals. | BT3 |
| amountIncludesTax | false | Confirmed. Line `totalAmount` is quantity x unit amount (minus line discount for US Visa CEDP); tax is separate. | BT3, BT1 |
| debitCreditIndicator | `kind` | Confirmed. `kind` is `"debit"` (sale) or `"credit"` (refund). GraphQL enum `DEBIT` / `CREDIT`. | BT3, BT4 |

### Units

- All amounts are decimal strings in major units, for example `"10.00"`: `amount`, `taxAmount`, `shippingAmount`, `shippingTaxAmount`, `discountAmount`, and line `unitAmount`, `totalAmount`, `taxAmount`, `discountAmount`, `unitTaxAmount` [BT3].
- Line `quantity` is also a decimal string (up to 4 decimals) [BT3].
- `taxExempt` is a JSON boolean [BT2, BT3].
- Line items nest directly under top level `lineItems[]` [BT3].

### Limits

| Field | Limit | Source |
|---|---|---|
| lineItems | max 249 | BT3 |
| `purchaseOrderNumber` | max 17 ASCII (12 for the AIB processor) | BT3 |
| `orderId` | max 255 (127 for PayPal) | BT3 |
| `shipsFromPostalCode` | US formats `10001`, `100010000`, `10001-0000`; Canada `ANA NAN` | BT3 |
| `shipping.postalCode` | 4 to 9 alphanumerics, optional dash or space | BT3 |
| line `name` | max 35 (127 for PayPal) | BT3 |
| line `description` | max 127 | BT3 |
| line `productCode` | max 12 (127 for PayPal) | BT3 |
| line `commodityCode`, `unitOfMeasure` | max 12 | BT3 |
| line `upcCode` | max 17; needs `upcType` | BT3 |
| line `quantity` | up to 4 decimals, > 0 | BT3 |
| line `unitAmount` | up to 4 decimals (2 for PayPal), > 0 | BT3 |
| line `totalAmount`, `taxAmount`, `discountAmount`, `unitTaxAmount` | up to 2 decimals, not negative | BT3 |
| name, unitOfMeasure, productCode, commodityCode characters | letters, digits, apostrophes, periods, hyphens, spaces (others become spaces on statements) | BT1 |
| US L2 tax rate | Visa 0.1% to 22%, Mastercard 0.1% to 30% | BT1 |

Validation [BT1, BT2]:

- Visa: `amount = sum(line totalAmount) + taxAmount + shippingAmount + shippingTaxAmount - discountAmount`, with line `totalAmount = quantity x unitAmount - line discountAmount`.
- Mastercard: line total may simply be `quantity x unitAmount`.
- A mismatch may cause a decline.
- L2/L3 data can also be sent at `submitForSettlement`, where it overrides data from the sale.

### Example shape (BT2, Node, Level 3 "amount validation" sale)

- `amount`: decimal string (2 dp)
- `paymentMethodNonce`: string
- `purchaseOrderNumber`: string
- `taxAmount`: decimal string (2 dp)
- `taxExempt`: boolean
- `shippingAmount`: decimal string (2 dp)
- `shippingTaxAmount`: decimal string (1 dp in the example)
- `discountAmount`: decimal string (2 dp)
- `shipsFromPostalCode`: string
- `shipping`: object with `firstName`, `lastName`, `streetAddress`, `extendedAddress`, `locality`, `region`, `postalCode`, `countryCodeAlpha3` (all strings)
- `lineItems`: array of 1 object with `name`, `kind` (`"debit"`), `quantity` (string, 4 dp), `unitAmount` (string, 4 dp), `unitOfMeasure`, `totalAmount` (string, 2 dp), `taxAmount` (string, 2 dp), `discountAmount` (string, 2 dp), `productCode`, `commodityCode`

### Detection keys

- Top level `lineItems` array whose items carry `kind` and `unitAmount`.
- Top level `amount` is a decimal string.
- Any of `taxAmount`, `purchaseOrderNumber`, `shipsFromPostalCode`, `taxExempt`, `orderId`, `paymentMethodNonce` / `paymentMethodToken`, `merchantAccountId`.
- No `merchantAccount`, no `amount_details`, no `processing`.

### Notes on ambiguity

- GraphQL (BT4) nests the same data differently. In the `transaction` input: `tax.taxAmount`, `tax.taxExempt`, `shipping.shippingAddress`, `shipping.shippingAmount`, `shipping.shippingTaxAmount`, `shipping.shipsFromPostalCode`, plus top level `discountAmount`, `purchaseOrderNumber`, `orderId`, `lineItems[]` (with `kind` as enum `DEBIT` / `CREDIT`). An adapter for the spec column will not parse GraphQL payloads.
- The .NET example uses both `ShippingAddress` and `Shipping` for the address property name [BT1]; the Node reference uses `shipping` [BT3].
- `orderId` is the merchant order id. Braintree does not label it as the L2/L3 invoice number.

---

## Cybersource (REST payments)

### Field mapping

| Canonical field | Spec mapping | Finding | Source |
|---|---|---|---|
| amount | `orderInformation.amountDetails.totalAmount` | Confirmed. Decimal string, not negative. For authorizations it may be omitted when line items are sent. | CY19, CY5 |
| currency | `orderInformation.amountDetails.currency` | Confirmed. 3 char ISO code. | CY19, CY2 |
| tax.amount | `orderInformation.amountDetails.taxAmount` | Confirmed as a field (total tax for all items). The field reference says it applies only to the American Express Direct and Barclays processors. The Visa Platform Connect guide sends tax per line in `lineItems[].taxAmount` and optionally `amountDetails.taxDetails[].amount`. | CY9, CY19, CY2 |
| tax.indicator | Derived from tax | Corrected. Explicit flag `orderInformation.invoiceDetails.taxable` (true = taxable). It is the inverse of "exempt". Visa Platform Connect requires `true` when line tax > 0. Related: `lineItems[].taxStatusIndicator`, `amountDetails.taxDetails[].exemptionCode`. | CY11, CY20, CY2 |
| customer.reference | `orderInformation.invoiceDetails.purchaseOrderNumber` | Confirmed. Processors label it "Customer Reference Number" or "customerCode". | CY10 |
| order.invoiceNumber | `clientReferenceInformation.code` | Confirmed as the merchant reference code. Alternatives exist: `orderInformation.invoiceDetails.invoiceNumber` [CY20] and L3 optional `orderInformation.lineItems[].invoiceNumber` [CY16, CY2]. | CY15, CY2 |
| amounts.freight | `orderInformation.amountDetails.freightAmount` | Confirmed. | CY19, CY2 |
| amounts.duty | `orderInformation.amountDetails.dutyAmount` | Confirmed. | CY19, CY2 |
| amounts.discount | `orderInformation.amountDetails.discountAmount` | Confirmed. | CY19, CY2 |
| amounts.shippingTax | Not supported | Confirmed. No shipping tax field in amount details, tax details types, or the L2/L3 field lists. | CY19, CY23, CY2 |
| shipping.fromPostalCode | To confirm | Corrected: `orderInformation.shippingDetails.shipFromPostalCode`. String, max 10. | CY12, CY21, CY2 |
| shipping.toPostalCode | `orderInformation.shipTo.postalCode` | Confirmed. Max 10. | CY13, CY2 |
| shipping.toCountry | `orderInformation.shipTo.country` | Confirmed. 2 char ISO code. Field reference lists it for only two processors (one of them Worldpay VAP). | CY14, CY22 |
| order.orderDate | Not supported | Corrected (candidate). `orderInformation.invoiceDetails.purchaseOrderDate` exists, format `YYYY-MM-DD`, listed as an L3 optional field. It is a purchase order date, not strictly an order date. | CY20, CY2, CY3 |
| Line items array | `orderInformation.lineItems[]` | Confirmed. | CY2, CY18 |
| description | `productName` | Confirmed. | CY2, CY18 |
| productCode | `productCode` or `productSku` | Confirmed, with caveat. `productCode` is documented as a product category (`default`, `electronic_good`, ...; max 255) but maps to the processor "Product Code" for most L3 processors. `productSku` identifies the item; Barclays maps SKU to its "Product Code". Casing: field reference writes `productSKU`, SDK and examples use `productSku`. | CY7, CY8, CY24, CY5 |
| commodityCode | `commodityCode` | Confirmed (line). An order level `orderInformation.invoiceDetails.commodityCode` also exists. | CY2, CY18, CY20 |
| quantity | `quantity` | Confirmed. Integer in the SDK model; shown as a string in doc examples. | CY18, CY24, CY3 |
| unitOfMeasure | `unitOfMeasure` | Confirmed. | CY2, CY18 |
| unitCost | `unitPrice` | Confirmed. Decimal string. | CY2, CY18 |
| discountAmount | `discountAmount` | Confirmed. | CY2, CY18 |
| taxAmount (line) | `taxAmount` | Confirmed. Required for L2 and L3 in the Visa Platform Connect guide. | CY2, CY18 |
| total (line) | `totalAmount` | Confirmed. | CY2, CY18 |
| amountIncludesTax | `amountIncludesTax` | Confirmed, with type caveat. SDK model: boolean. Field reference: string, length 1, `Y` or `N` (maps to the processor "Gross/Net Indicator"). | CY6, CY18, CY24 |
| debitCreditIndicator | debit | Confirmed. No line level debit/credit indicator in the REST line item model. The Simple Order `item_#_sign` (sign of line tax, Visa Platform Connect only) has no REST field. Credits use a separate endpoint (`/pts/v2/credits`). | CY18, CY17, CY2 |

### Units

- All amounts are decimal strings in major units, for example `"7.77"`: `amountDetails.totalAmount`, `taxAmount`, `freightAmount`, `dutyAmount`, `discountAmount`, and line `unitPrice`, `totalAmount`, `taxAmount`, `discountAmount` [CY2, CY19, CY18].
- `processingInformation.purchaseLevel` is a string (`"3"` for Level III) [CY2, CY3].
- `invoiceDetails.taxable` is a JSON boolean in examples and the SDK; the field reference calls it a string of max 5 (`true` / `false`) [CY11, CY20].
- Line items nest under `orderInformation.lineItems[]` (field reference and SDK). Order level tax detail can also go in `orderInformation.amountDetails.taxDetails[]` (`type`, `amount`, `rate`, `code`, `taxId`, `applied`, `exemptionCode`) [CY23].

### Limits

| Field | Limit | Source |
|---|---|---|
| line items per request | processor and card specific; Barclays 99 (Visa) and 99 (Mastercard); no general REST cap found | CY4 |
| `purchaseOrderNumber` | processor specific: Visa Platform Connect 17; Chase Paymentech and Barclays 25; GPN 17 (Mastercard) / 25 (Visa); Worldpay VAP 17; American Express Direct 17 | CY10 |
| `shipTo.postalCode` | max 10 | CY13 |
| `shipTo.country` | 2 chars | CY14 |
| `shipTo.administrativeArea` | 2 chars (US and Canada codes) | CY22 |
| `shippingDetails.shipFromPostalCode` | max 10; 5 to 9 digits (US), `A1B 2C3` (Canada) | CY12 |
| `amountDetails.taxAmount` | 7 chars (CAD), 9 chars (USD) | CY9 |
| `amountDetails.totalAmount` | 12 chars for Visa Platform Connect and Chase Paymentech (processor specific) | CY19 |
| `clientReferenceInformation.code` | 50 by default; processor specific lower limits; some processors truncate it to 15 or 17 chars for L2/L3 | CY15 |
| `lineItems[].invoiceNumber` | 15 (Visa Platform Connect), 23 (others) | CY16 |
| `lineItems[].productCode` | 255 | CY7 |
| `lineItems[].taxRate` | Visa 0.01 to 0.99; Mastercard 0.00001 to 0.99999 | CY18 |
| over length values | Cybersource truncates to the network limit, keeping left most chars | CY2 |

### Example shape (CY3, Visa Platform Connect, Level III sale)

POST `/pts/v2/payments` (inferred from response links):

- `processingInformation.capture`: boolean
- `processingInformation.purchaseLevel`: string `"3"`
- `orderInformation.billTo`: object of strings (`country` 2 letters, `firstName`, `lastName`, `address1`, `locality`, `administrativeArea`, `postalCode`, `email`)
- `orderInformation.invoiceDetails.purchaseOrderNumber`: string
- `orderInformation.invoiceDetails.purchaseOrderDate`: string `YYYY-MM-DD`
- `orderInformation.invoiceDetails.taxable`: boolean
- `orderInformation.amountDetails.totalAmount`: decimal string (2 dp)
- `orderInformation.amountDetails.currency`: string
- `orderInformation.lineItems[]`: objects with `productCode`, `productName`, `quantity` (integer as string), `unitPrice` (decimal string), `unitOfMeasure`, `totalAmount` (decimal string), `taxAmount` (decimal string)
- `paymentInformation.card`: `number`, `expirationMonth`, `expirationYear`, `type`, `securityCode` (strings)

The page renders `lineItems` inside `billTo`. That is a doc formatting error; the field reference path is `orderInformation.lineItems[]`. The L2/L3 capture examples in CY2 also carry `clientReferenceInformation.code` (string). No published arithmetic rule ties line totals to `totalAmount`.

### Detection keys

- `orderInformation` object plus `clientReferenceInformation` and/or `processingInformation` and/or `paymentInformation`.
- Amounts sit under `orderInformation.amountDetails`.

### Notes on ambiguity

- Order level `amountDetails.taxAmount` is processor limited (American Express Direct, Barclays). For other processors the total tax is effectively the sum of `lineItems[].taxAmount`. The adapter should fall back to the line sum when `amountDetails.taxAmount` is absent.
- `productCode` has two meanings (Cybersource product category vs network product code). Prefer `productSku` when both exist? Not documented; see Unconfirmed.
- Type mismatches between the field reference (Y/N strings) and the SDK and examples (booleans) for `amountIncludesTax` and `taxable`. The adapter should accept both.
- `totalAmount` may be absent on authorizations that send line items; the authorized amount is then derived from lines [CY5].

---

## Authorize.net (createTransactionRequest)

### Field mapping

All paths below are under `createTransactionRequest.transactionRequest`.

| Canonical field | Spec mapping | Finding | Source |
|---|---|---|---|
| amount | `transactionRequest.amount` | Confirmed. Decimal; total including tax, shipping and other charges; up to 15 digits. | AN1 |
| currency | From config | Corrected. `transactionRequest.currencyCode` exists (ISO 4217, 3 letters, optional in the schema). Fall back to config only when absent. | AN1, AN2 |
| tax.amount | `transactionRequest.tax.amount` | Confirmed. Decimal, up to 4 dp; must be included in `amount`; conditional for L2/L3/CEDP. Siblings `tax.name` (31), `tax.description` (255). | AN1, AN2 |
| tax.indicator | `transactionRequest.taxExempt` | Confirmed. Boolean. | AN1, AN2 |
| customer.reference | `transactionRequest.poNumber` | Confirmed. String, max 25 (reference), alphanumeric only; conditional for L2/L3/CEDP. | AN1 |
| order.invoiceNumber | `transactionRequest.order.invoiceNumber` | Confirmed. Max 20. | AN1, AN2 |
| amounts.freight | `transactionRequest.shipping.amount` | Confirmed. Decimal, up to 4 dp; included in `amount`. | AN1, AN2 |
| amounts.duty | `transactionRequest.duty.amount` | Confirmed. Decimal, up to 4 dp; included in `amount`. | AN1, AN2 |
| amounts.discount | Not supported | Corrected. `transactionRequest.discount.amount` exists (extended amount: `amount`, `name`, `description`). The schema also has `transactionRequest.order.discountAmount` (schema only, not in the reference table). | AN1, AN2 |
| amounts.shippingTax | Not supported | Confirmed. No shipping tax element. | AN1, AN2 |
| shipping.fromPostalCode | Not supported | Corrected. `transactionRequest.shipFrom.zip` exists (`shipFrom` is a name and address block; zip max 20). | AN1, AN2 |
| shipping.toPostalCode | `transactionRequest.shipTo.zip` | Confirmed. Max 20; conditional for L2/L3/CEDP. | AN1, AN2 |
| shipping.toCountry | `transactionRequest.shipTo.country` | Confirmed. String, max 60 (free text, not restricted to ISO codes). | AN1, AN2 |
| order.orderDate | Not supported | Corrected (candidate, schema only). `transactionRequest.order.purchaseOrderDateUTC` (xs:date) is in the schema but not in the API reference table. | AN2 |
| Line items array | `transactionRequest.lineItems.lineItem[]` | Confirmed. Max 30 per the reference. | AN1, AN2 |
| description | `name` and `description` | Confirmed. `name` max 31 (required by schema), `description` max 255. | AN1, AN2 |
| productCode | `productCode` | Confirmed. Max 255; conditional for L3/CEDP. `productSKU` (max 255) also exists. | AN1, AN2 |
| commodityCode | `commodityCode` | Confirmed. Max 12. | AN1, AN2 |
| quantity | `quantity` | Confirmed. Decimal, up to 4 dp, >= 0. | AN1, AN2 |
| unitOfMeasure | `unitOfMeasure` | Confirmed. Max 12. | AN1, AN2 |
| unitCost | `unitPrice` | Confirmed. Decimal, up to 4 dp, >= 0; excludes tax, freight, duty. | AN1, AN2 |
| discountAmount | `discountAmount` | Confirmed. Decimal, up to 4 dp. | AN1, AN2, AN3 |
| taxAmount (line) | `taxAmount` | Confirmed. Decimal, up to 4 dp. | AN1, AN2, AN3 |
| total (line) | `totalAmount` | Confirmed. Decimal, up to 4 dp. | AN1, AN2, AN3 |
| amountIncludesTax | `taxIncludedInTotal` | Confirmed in the schema and Ruby SDK (boolean). Not shown in the API reference request table. | AN2, AN3 |
| debitCreditIndicator | debit | Confirmed. No line credit indicator. | AN2, AN3 |

### Line item type (L2/L3 extension), schema order

`itemId` (string 1 to 31, required), `name` (string 1 to 31, required), `description` (255), `quantity` (decimal, 4 dp, required), `unitPrice` (decimal, 4 dp, required), `taxable` (boolean), `unitOfMeasure` (12), `typeOfSupply` (2), `taxRate` (5 digits, 5 dp), `taxAmount` (4 dp), `nationalTax`, `localTax`, `vatRate` (5,5), `alternateTaxId` (20), `alternateTaxType` (4), `alternateTaxTypeApplied` (4), `alternateTaxRate` (5,5), `alternateTaxAmount`, `totalAmount` (4 dp), `commodityCode` (12), `productCode` (255), `productSKU` (255), `discountRate` (5,5), `discountAmount` (4 dp), `taxIncludedInTotal` (boolean), `taxIsAfterDiscount` (boolean) [AN2, AN3].

### Units

- All amounts are decimals in major units, up to 4 decimal places (schema `fractionDigits 4`) [AN2].
- In the published JSON example every value is a quoted string, including amounts (`"45.00"`), quantity and booleans (`"true"`) [AN1]. The adapter must parse numbers and booleans from strings.

### Limits

| Field | Limit | Source |
|---|---|---|
| `lineItems.lineItem` count | max 30 (reference); schema says unbounded | AN1, AN2 |
| `order.invoiceNumber` | 20 | AN1, AN2 |
| `order.description` | 255 | AN1, AN2 |
| `poNumber` | 25, alphanumeric only (no schema restriction) | AN1, AN2 |
| `shipTo.*` / `shipFrom.*` | firstName, lastName, company 50; address 60; city 40; state 40; zip 20; country 60 | AN2 |
| `tax` / `duty` / `shipping` / `discount` | `amount` decimal >= 0, 4 dp; `name` 31; `description` 255 | AN2 |
| `purchaseLevel` | 1 char; `3` for L3/CEDP | AN1, AN2 |
| `currencyCode` | 3 letters | AN1 |
| line fields | see line item type above | AN2 |

Element order: the reference says JSON elements should be ordered to match the API reference (XML requires strict order). Schema order inside `transactionRequest` (relevant part): `transactionType`, `amount`, `currencyCode`, `payment`, ..., `order`, `lineItems`, `tax`, `duty`, `shipping`, `taxExempt`, `poNumber`, `customer`, `billTo`, `shipTo`, `customerIP`, ..., `otherTax`, `shipFrom`, ..., `purchaseLevel`, ..., `discount` [AN1, AN2].

### Example shape (AN1, "Charge a Credit Card" JSON)

- `createTransactionRequest.merchantAuthentication`: `name`, `transactionKey` (strings)
- `createTransactionRequest.refId`: string
- `createTransactionRequest.transactionRequest`:
  - `transactionType`: string
  - `amount`: string (decimal)
  - `payment.creditCard`: `cardNumber`, `expirationDate`, `cardCode` (strings)
  - `lineItems.lineItem`: a single object in the example (not an array) with `itemId`, `name`, `description`, `quantity` (string), `unitPrice` (decimal string)
  - `tax`, `duty`, `shipping`: objects each with `amount` (decimal string), `name`, `description`
  - `poNumber`: string
  - `customer`: `id`
  - `billTo`, `shipTo`: name and address objects (`firstName`, `lastName`, `company`, `address`, `city`, `state`, `zip`, `country`)
  - then `customerIP`, `transactionSettings`, `userFields`, `processingOptions`, `subsequentAuthInformation`, `authorizationIndicatorType`
- The JSON example omits the `order` block that the XML example has. To rebuild an L3 example, add `order.invoiceNumber` before `lineItems`, put `taxExempt` after `shipping`, and add `purchaseLevel` `"3"` near the end, following schema order.
- No published rule ties line totals to `amount`. The reference only says `amount` includes tax, shipping and duty.

### Detection keys

- Single root key `createTransactionRequest`, containing `merchantAuthentication` and `transactionRequest`.

### Notes on ambiguity

- `lineItems.lineItem` is an object in the single item JSON example. Whether multiple items must be a JSON array is implied by the 30 item cap but not shown. The adapter should accept object or array.
- `taxIncludedInTotal`, `taxIsAfterDiscount`, `order.discountAmount`, `order.purchaseOrderDateUTC` are in the public schema but not in the reference request table.
- `transactionRequest.discount` and `shipFrom` are documented in both the reference and the schema, so the spec's "Not supported" for discount and ship from is out of date.

---

## Checkout.com (payments)

### Field mapping

| Canonical field | Spec mapping | Finding | Source |
|---|---|---|---|
| amount | `amount` (minor) | Confirmed. Whole number, minor units, > 0, max 9 digits. | CK3, CK1 |
| currency | `currency` | Confirmed. | CK1 |
| tax.amount | `processing.tax_amount` | Confirmed. Total sales tax on the purchase; minor units. | CK1 |
| tax.indicator | Derived from tax | Corrected (partial). No order level flag, but a line level boolean `items[].tax_exempt` exists (Mastercard only; required on all items when tax is 0 for Level 2). | CK1 |
| customer.reference | `processing.order_id` | Confirmed. Described as the cardholder's invoice or PO number; max 15 (17 for Amex). | CK1, CK5 |
| order.invoiceNumber | `reference` | Confirmed as a field. Top level `reference` is the merchant identifier such as an order number, max 80. Not described on the L2/L3 page. | CK2 |
| amounts.freight | `processing.shipping_amount` | Confirmed. Freight, shipping and handling; minor units. | CK1 |
| amounts.duty | `processing.duty_amount` | Confirmed. Minor units. | CK1 |
| amounts.discount | Not supported | Corrected: `processing.discount_amount` (minor units; L3 CEDP minimum). | CK1, CK5 |
| amounts.shippingTax | Not supported | Corrected: `processing.shipping_tax_amount` (minor units; optional). | CK1, CK5 |
| shipping.fromPostalCode | To confirm | Corrected: `shipping.from_address_zip` (max 10). | CK1 |
| shipping.toPostalCode | `shipping.address.zip` | Confirmed. Max 10 (50 for Amex). | CK1 |
| shipping.toCountry | `shipping.address.country` | Confirmed. ISO alpha-2, 2 chars. | CK1 |
| order.orderDate | Not supported | Confirmed. No order date field. | CK1 |
| Line items array | `items[]` | Confirmed. | CK1 |
| description | `name` | Confirmed. Max 26 (12 for Amex); must not match or contain the merchant name. | CK1 |
| productCode | `reference` | Confirmed. `items[].reference`, product id or SKU, max 12; not blank or all zeros. | CK1 |
| commodityCode | `commodity_code` | Confirmed. Max 12. | CK1 |
| quantity | `quantity` | Confirmed. Integer (Amex: number). | CK1 |
| unitOfMeasure | `unit_of_measure` | Confirmed. Max 12; must be a code from the page's measure code list. | CK1 |
| unitCost | `unit_price` (minor) | Confirmed. Minor units. | CK1 |
| discountAmount | `discount_amount` | Confirmed. Minor units. | CK1 |
| taxAmount (line) | `tax_amount` | Confirmed. Minor units. | CK1 |
| total (line) | `total_amount` | Confirmed. Minor units; excludes tax. | CK1 |
| amountIncludesTax | false | Confirmed. `total_amount = quantity x unit_price - discount`, tax excluded. | CK1 |
| debitCreditIndicator | debit | Confirmed. No line debit/credit indicator or line type for L2/L3. | CK1 |

### Units

- `amount` and every L2/L3 money field are whole numbers in the minor currency unit; decimal points are not supported [CK1, CK3].
- Three decimal currencies need a trailing 0 in `amount` [CK3].
- `items[].quantity` is an integer [CK1].
- The public .NET SDK types the `processing` amounts as nullable decimals with no unit note [CK5]; the docs are the authority (minor units).
- L2/L3 money fields sit in `processing` (order level) and in `items[]` (line level). Ship to and ship from sit in `shipping` [CK1].

### Limits

| Field | Limit | Source |
|---|---|---|
| items | max 50 (Visa and Mastercard L3); max 4 for Amex; extras ignored | CK1 |
| `processing.order_id` | 15 (Amex 17) | CK1 |
| `customer.tax_number` | 13 | CK1 |
| `shipping.address.zip` | 10 (Amex 50) | CK1 |
| `shipping.address.country` | 2 | CK1 |
| `shipping.from_address_zip` | 10 | CK1 |
| `items[].name` | 26 (Amex 12) | CK1 |
| `items[].reference`, `commodity_code`, `unit_of_measure` | 12 | CK1 |
| top level `reference` | 80 | CK2 |
| `amount` | whole number, max 9 digits | CK3 |
| tax rate | 0, or between 0.1% and 22% of `amount` | CK1 |

Validation [CK1]:

- `items[].total_amount = quantity x unit_price - discount` (no tax).
- `amount = sum(items[].total_amount) - processing.discount_amount + processing.tax_amount + processing.shipping_amount`. `duty_amount` and `shipping_tax_amount` are not in the formula.
- Amex supports Level 2 only.
- L2/L3 can be sent in the payment or the capture request; capture values override; partial captures must carry the data themselves.

### Example shape (CK1, Level 3 payment request)

- `source`: `type` (`"card"`), `number`, `expiry_month`, `expiry_year`, `cvv`
- `amount`: integer (minor); `currency`: string
- `customer.tax_number`: string
- `processing`: `order_id` (string); `tax_amount`, `discount_amount`, `duty_amount`, `shipping_amount`, `shipping_tax_amount` (integers, minor)
- `shipping.address`: `zip`, `country` (strings); `shipping.from_address_zip`: string
- `items`: array of 1 object with `name`, `reference`, `commodity_code`, `unit_of_measure` (strings), `quantity` (integer), `unit_price`, `total_amount`, `tax_amount`, `discount_amount` (integers, minor)
- `processing_channel_id`: string

Arithmetic in the example: one item, quantity 1, no discount, so `total_amount = unit_price`. `amount = total_amount + tax_amount + shipping_amount`; duty and shipping tax are 0. The Level 2 example has only `source`, `amount`, `currency`, `customer.tax_number`, `processing.order_id`, `processing.tax_amount`, `processing_channel_id`. The capture example uses the same L2/L3 structure plus `capture_type` and `reference`, and no `source`.

### Detection keys

- `processing` object and/or `items` array with snake_case keys (`unit_price`, `total_amount`).
- Top level integer `amount`.
- Checkout.com only keys: `source`, `processing_channel_id`.
- `shipping.address.zip` (Stripe uses `postal_code`).

### Notes on ambiguity

- `processing.order_id` is described as the "invoice or purchase order number", so it could serve as either `customer.reference` or `order.invoiceNumber`. The spec's choice (order_id to customer reference, top level `reference` to invoice) is reasonable but not stated by Checkout.com.
- The interactive API reference [CK4] could not be read as text, so field types come from the docs guide and the public SDK only.

---

## Auto detection summary

| Format | Distinctive keys |
|---|---|
| Authorize.net | single root `createTransactionRequest` (with `merchantAuthentication`, `transactionRequest`) |
| Cybersource | `orderInformation` plus `clientReferenceInformation` / `processingInformation` / `paymentInformation` |
| Adyen | `merchantAccount` plus object `amount {value, currency}`; L2/L3 in `additionalData["enhancedSchemeData.*"]` or `enhancedSchemeData.levelTwoThree` |
| Stripe | `amount_details` and/or `payment_details`; integer `amount`; lowercase `currency` |
| Checkout.com | `processing` and/or `items` (snake_case line keys); `source`, `processing_channel_id`; integer `amount` |
| Braintree | `lineItems` with `kind` / `unitAmount`; decimal string `amount`; `taxAmount`, `purchaseOrderNumber`, `shipsFromPostalCode`, `paymentMethodNonce` |

Suggested check order: Authorize.net root, then Cybersource, then Adyen (`merchantAccount` first, because Adyen also has a `lineItems` array), then Stripe, then Checkout.com, then Braintree.

---

## Counts

| Gateway | Confirmed | Corrected | Unconfirmed |
|---|---|---|---|
| Stripe | 26 | 0 | 0 |
| Adyen | 26 (legacy keys; new structured shape also documented) | 0 | 0 |
| Braintree | 26 | 0 | 0 |
| Cybersource | 23 | 3 (tax.indicator, shipping.fromPostalCode, order.orderDate candidate) | 0 |
| Authorize.net | 22 | 4 (currency, amounts.discount, shipping.fromPostalCode, order.orderDate candidate) | 0 |
| Checkout.com | 22 | 4 (tax.indicator line level, amounts.discount, amounts.shippingTax, shipping.fromPostalCode) | 0 |

Every row has a finding, but several sub questions remain open (below).

## Unconfirmed

1. **Adyen item line numbering base and max count.** The docs write `itemDetailLine[itemNr]` or `[N]` and never state the base or a cap. The only literal example (classic HPP, end of life) uses `itemDetailLine1`. Tried: AD1, AD2 (both halves of the page), AD3, AD4, AD5, two web searches.
2. **Adyen legacy vs structured form.** It is not documented whether `additionalData` ESD keys and `enhancedSchemeData.levelTwoThree` may be mixed, which takes priority, or whether the legacy keys are deprecated in v72. Tried: AD1, AD2, AD5, search for the v72 upgrade guide (only a marketing post and SDK release notes found).
3. **Adyen `reference` max length.** Not reached in the API explorer text (the field list portion that was readable did not include it).
4. **Cybersource `productSku` casing.** The field reference shows `productSKU`; the SDK models and the REST example use `productSku`. Whether the API is case insensitive is not documented. Tried: CY8, CY24, CY5.
5. **Cybersource `productCode` vs `productSku` for L3 product code.** The docs give `productCode` as a category value but map it to the network product code for most processors. Which one to prefer when both are present is not stated. Tried: CY7, CY8, CY2.
6. **Cybersource REST line item cap.** Only Barclays (99 per card brand) found on a current REST page. Tried: CY4, a domain limited search.
7. **Cybersource types for `amountIncludesTax` and `taxable`.** Field reference says Y/N string and string(5); SDK and examples use booleans. Tried: CY6, CY11, CY18, CY20, CY3.
8. **Cybersource `clientReferenceInformation.code` per processor limits.** The fetched text was internally inconsistent (default 50, with several processors at 8). Tried: CY15.
9. **Cybersource interactive API reference** (CY25) could not be read as text; SDK docs generated from the same API were used instead.
10. **Authorize.net multi item JSON form.** The JSON example shows `lineItems.lineItem` as one object; an array for several items is implied by the 30 item cap but not shown. Tried: AN1.
11. **Authorize.net schema only fields** (`taxIncludedInTotal`, `taxIsAfterDiscount`, `order.discountAmount`, `order.purchaseOrderDateUTC`): in the public XSD and Ruby SDK but not in the reference request table. Tried: AN1 (first 100k chars and a targeted second read), AN2, AN3.
12. **Authorize.net `currencyCode` default** when omitted (assumed merchant account currency, not stated on the pages read). Tried: AN1, AN2.
13. **Checkout.com API reference.** The interactive reference (CK4) and a guessed `swagger.json` did not return content. Field names and limits rest on the L2/L3 guide (CK1), Terminology (CK2), amount format page (CK3) and the public .NET SDK (CK5).
14. **Braintree `orderId` as the L2/L3 invoice number.** Braintree does not say `orderId` is sent to networks as the invoice or order reference. Tried: BT1, BT2, BT3.
15. **Stripe top level `shipping.address.country`.** It exists on PaymentIntent, but the L2/L3 guide does not say Stripe forwards it as destination country. Tried: ST1, ST4.
