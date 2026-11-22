# Adapters and field mappings

l3lint checks one canonical payload shape. Gateway request bodies reach it through adapters: each adapter reads one
format, builds the canonical transaction, and keeps a path map so every finding also points at the field in your own
format (for example `/amount_details/line_items/1/payment_method_options/card/commodity_code` for Stripe, or
`row 14, column F (unit_price)` for CSV).

Seven adapters ship in the `L3Lint.Adapters` package:

| Name (`--format`) | Reads | Kind |
| --- | --- | --- |
| `stripe` | Stripe PaymentIntent create request (JSON form) | JSON |
| `adyen` | Adyen Checkout `/payments` or capture request | JSON |
| `braintree` | Braintree transaction sale request (server SDK, camelCase) | JSON |
| `cybersource` | Cybersource REST payments request | JSON |
| `authorizenet` | Authorize.net `createTransactionRequest` (wrapped or bare `transactionRequest`) | JSON |
| `checkoutcom` | Checkout.com payment or capture request | JSON |
| `csv` | CSV with a YAML column map | CSV |

In code, add them with `StandardAdapters.All`:

```csharp
var builder = new LinterBuilder();
foreach (var adapter in StandardAdapters.All)
{
    builder.AddAdapter(adapter);
}
```

## How every adapter behaves

- **Tolerant.** An adapter never stops on payload content. A value of the wrong type (a string where a number belongs,
  an object that is a string) becomes one GEN-FMT-001 finding at that field, the value is left out, and every rule that
  needs it skips it instead of reporting it missing. An object of the wrong type counts once, not once per field inside it.
- **Units.** Stripe, Adyen and Checkout.com send integers in minor units. They are divided by 10 to the power of the
  currency's ISO 4217 exponent: 1250 is 12.50 USD, 1250 JPY (exponent 0), and 1.250 BHD (exponent 3). When the currency
  is missing or unknown, 2 decimal places are assumed and a note says so. Braintree, Cybersource and Authorize.net send
  major units (decimal strings or numbers) and are read as sent.
- **Line numbers.** Lines are numbered 1, 2, 3 from their order in the request. A gateway's own line number or item ID
  is never used instead.
- **Derived values.** When a format has no field for a value that follows from other values, the adapter computes it
  and lists it as derived. Arithmetic checks skip derived values instead of trivially passing them. Derived values
  are listed per adapter below.
- **No guessing.** Card brand, card product and MCC are usually not in gateway requests; pass them with `--brand`,
  `--card-product` and `--mcc`. A tax exemption is never inferred from a zero tax amount. Where a source field's meaning
  is unclear the value is mapped and a note explains the choice (shown with `--verbose`).
- **Ignored fields.** With `--verbose`, every source field that maps to no canonical field is listed as ignored.
- **Card numbers.** JSON documents are scrubbed before an adapter reads them; a card number in any field is masked to
  its last four digits and reported as GEN-PCI-001 at the canonical field it maps to. The CSV adapter masks cells
  itself and reports the line and column of the cell.
- **Fix patches.** Patches from `l3lint fix` are written in the source format's paths and units: minor-unit integers for
  Stripe, Adyen and Checkout.com, decimal strings for Braintree, Cybersource and Authorize.net, and each gateway's own
  flag for the tax indicator. A canonical value the format cannot express (for example tax indicator `notProvided`)
  produces no patch.

The status column in the tables below uses the result of the October 2026 check of each gateway's public
documentation (ADR-0026):

- **Confirmed**: the public documentation shows this field.
- **Corrected**: the spec named a different path or "not supported"; the documented path is used.
- **Unconfirmed**: no public page settled the question; the behavior chosen is described and covered by tests built
  from the shape of the gateway's published examples.

Fields a format does not carry are listed after each table. Their findings point at no source path.

## Stripe (`stripe`)

Sources: [payment line items guide](https://docs.stripe.com/payments/payment-line-items),
[PaymentIntent create, amount_details](https://docs.stripe.com/api/payment_intents/create?query=amount_details),
[payment_details](https://docs.stripe.com/api/payment_intents/create?query=payment_details),
[amount and currency](https://docs.stripe.com/api/payment_intents/create).

| Canonical field | Stripe path | Units | Status |
| --- | --- | --- | --- |
| `amount` | `amount` | integer, minor units | Confirmed |
| `currency` | `currency` | lowercase ISO code, read as uppercase | Confirmed |
| `tax.amount` | `amount_details.tax.total_tax_amount`; else the sum of line tax | integer, minor units | Confirmed |
| `tax.indicator` | derived: taxable when tax is above 0 | | Confirmed (no flag exists) |
| `customer.reference` | `payment_details.customer_reference` | text | Confirmed |
| `order.invoiceNumber` | `payment_details.order_reference` | text | Confirmed |
| `amounts.freight` | `amount_details.shipping.amount` | integer, minor units | Confirmed |
| `amounts.discount` | `amount_details.discount_amount` | integer, minor units | Confirmed |
| `shipping.fromPostalCode` | `amount_details.shipping.from_postal_code` | text | Confirmed |
| `shipping.toPostalCode` | `amount_details.shipping.to_postal_code` | text | Confirmed |
| line items | `amount_details.line_items[]` | | Confirmed |
| `description` | `product_name` | text | Confirmed |
| `productCode` | `product_code` | text | Confirmed |
| `commodityCode` | `payment_method_options.card.commodity_code` | text | Confirmed |
| `quantity` | `quantity` | integer | Confirmed |
| `unitOfMeasure` | `unit_of_measure` | text | Confirmed |
| `unitCost` | `unit_cost` | integer, minor units | Confirmed |
| `discountAmount` | `discount_amount` | integer, minor units | Confirmed |
| `taxAmount` | `tax.total_tax_amount` | integer, minor units | Confirmed |
| `total` | derived: round(quantity x unit cost) minus line discount | | Confirmed (no field exists) |

Not carried: `amounts.duty`, `amounts.shippingTax`, `shipping.toCountry`, `order.orderDate`, `billing.postalCode`, line
`amountIncludesTax` (Stripe adds line tax on top) and `debitCreditIndicator` (always debit).

Detection: an `amount_details` or `payment_details` object; an integer `amount` and a lowercase `currency` raise the score.

Derived: `/lineItems/*/total` always; `/tax/amount` when only lines carry tax (Stripe accepts tax at one level only);
`/tax/indicator` when tax is above 0.

Limitations:

- Stripe only accepts discounts above 0, so a line or order with no discount cannot send the 0 Visa asks for; Visa
  reports the missing discount.
- The top-level `shipping.address.country` exists on a PaymentIntent, but the Level 2 and 3 guide does not say Stripe
  forwards it as the destination country, so it is not mapped (unconfirmed).

## Adyen (`adyen`)

Sources: [Level 2 and 3 guide](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3),
[/payments API explorer](https://docs.adyen.com/api-explorer/Checkout/71/post/payments),
[AdditionalDataLevel23 model](https://raw.githubusercontent.com/Adyen/adyen-java-api-library/main/src/main/java/com/adyen/model/checkout/AdditionalDataLevel23.java),
[classic HPP example](https://docs.adyen.com/online-payments/classic-integrations/hosted-payment-pages/hpp-payments-with-l2l3-data).

Adyen has two request forms and the adapter reads both:

- the structured object `enhancedSchemeData.levelTwoThree` (current guide), with JSON integers and an
  `itemDetailLines` array;
- the legacy flat `additionalData` keys `enhancedSchemeData.*`, with string values and lines numbered in the key
  (`enhancedSchemeData.itemDetailLine1.unitPrice`).

In the table, S is `enhancedSchemeData.levelTwoThree` and L is `additionalData["enhancedSchemeData.` ... `"]`.

| Canonical field | Structured path | Legacy key | Units | Status |
| --- | --- | --- | --- | --- |
| `amount` | `amount.value` | same | integer, minor units | Confirmed |
| `currency` | `amount.currency` | same | ISO code | Confirmed |
| `order.invoiceNumber` | `reference` | same | text | Confirmed |
| `customer.reference` | S `customerReferenceNumber` | L `customerReference` | text | Confirmed |
| `tax.amount` | S `totalTaxAmount` | L `totalTaxAmount` | minor units (legacy as text) | Confirmed |
| `tax.indicator` | derived: taxable when tax is above 0 | | | Confirmed (no flag exists) |
| `amounts.freight` | S `freightAmount` | L `freightAmount` | minor units | Confirmed |
| `amounts.duty` | S `dutyAmount` | L `dutyAmount` | minor units | Confirmed |
| `shipping.fromPostalCode` | S `shipFromPostalCode` | L `shipFromPostalCode` | text | Confirmed |
| `shipping.toPostalCode` | S `destination.postalCode` | L `destinationPostalCode` | text | Confirmed |
| `shipping.toCountry` | S `destination.countryCode` | L `destinationCountryCode` | alpha-2 or alpha-3, read as alpha-2 | Confirmed |
| `order.orderDate` | S `orderDate` | L `orderDate` | yyyy-MM-dd (structured), ddMMyy (legacy) | Confirmed |
| line items | S `itemDetailLines[]` | L `itemDetailLine1.*`, `itemDetailLine2.*` | | Confirmed |
| `description` | `description` | same | text | Confirmed |
| `productCode` | `productCode` | same | text | Confirmed |
| `commodityCode` | `commodityCode` | same | text | Confirmed |
| `quantity` | `quantity` | same | integer (legacy as text) | Confirmed |
| `unitOfMeasure` | `unitOfMeasure` | same | text | Confirmed |
| `unitCost` | `unitPrice` | same | minor units | Confirmed |
| `discountAmount` | `discountAmount` | same | minor units | Confirmed |
| `total` | `totalAmount` | same | minor units | Confirmed |

Not carried: `amounts.discount` (only line discounts exist), `amounts.shippingTax`, line `taxAmount`,
`billing.postalCode`, line `amountIncludesTax` (Adyen's line total excludes tax) and `debitCreditIndicator` (always debit).

Detection: a top-level `merchantAccount` string; an `amount` object with `value` and Level 2 and 3 data in either form
raise the score. Adyen also uses a top-level `lineItems` array for other payment methods, so `merchantAccount` decides.

Derived: `/tax/indicator` when tax is above 0.

Limitations:

- **Unconfirmed**: whether the two forms may be mixed and which wins. When both are present the structured object is
  used and the legacy keys are not read; a note says so.
- **Unconfirmed**: the base and maximum of legacy line numbers. Lines are read in ascending number order (the only
  published example starts at 1). A gap such as lines 1 and 3 adds a note; the lines are numbered 1 and 2 by order.
- Adyen leaves `dutyAmount` out of the payment amount, while l3lint's total formula adds it. A duty above 0 adds a note,
  and the transaction total check may report it as a difference.

## Braintree (`braintree`)

Sources: [Level 2 and 3 required fields (.NET)](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/),
[same page (Node)](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/node),
[transaction sale reference](https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node).

| Canonical field | Braintree path | Units | Status |
| --- | --- | --- | --- |
| `amount` | `amount` | decimal string | Confirmed |
| `currency` | none: the default currency option (`AdapterOptions.DefaultCurrency`) | | Confirmed (from the merchant account) |
| `tax.amount` | `taxAmount` | decimal string | Confirmed |
| `tax.indicator` | `taxExempt` (true is exempt, false is taxable) | boolean | Confirmed |
| `customer.reference` | `purchaseOrderNumber` | text | Confirmed |
| `order.invoiceNumber` | `orderId` | text | Unconfirmed (see below) |
| `amounts.freight` | `shippingAmount` | decimal string | Confirmed |
| `amounts.discount` | `discountAmount` | decimal string | Confirmed |
| `amounts.shippingTax` | `shippingTaxAmount` | decimal string | Confirmed |
| `shipping.fromPostalCode` | `shipsFromPostalCode` | text | Confirmed |
| `shipping.toPostalCode` | `shipping.postalCode` | text | Confirmed |
| `shipping.toCountry` | `shipping.countryCodeAlpha2`, else `shipping.countryCodeAlpha3` | alpha-3 read as alpha-2 | Confirmed |
| line items | `lineItems[]` | | Confirmed |
| `description` | `name` | text | Confirmed |
| `productCode` | `productCode` | text | Confirmed |
| `commodityCode` | `commodityCode` | text | Confirmed |
| `quantity` | `quantity` | decimal string | Confirmed |
| `unitOfMeasure` | `unitOfMeasure` | text | Confirmed |
| `unitCost` | `unitAmount` | decimal string, up to 4 decimals | Confirmed |
| `discountAmount` | `discountAmount` | decimal string | Confirmed |
| `taxAmount` | `taxAmount` | decimal string | Confirmed |
| `total` | `totalAmount` | decimal string | Confirmed |
| `debitCreditIndicator` | `kind` (`debit` or `credit`) | text | Confirmed |

Not carried: `currency`, `amounts.duty`, `order.orderDate`, line `amountIncludesTax` (Braintree's line total excludes tax).

Detection: a top-level `lineItems` array whose items carry `unitAmount` or `kind`, or at least two of `taxAmount`,
`taxExempt`, `purchaseOrderNumber`, `shipsFromPostalCode`, `shippingAmount`, `shippingTaxAmount`, `paymentMethodNonce`,
`paymentMethodToken`, `merchantAccountId`; never when a key of another gateway is present.

Derived: none. A missing `taxExempt` stays missing, because Braintree can send it.

Limitations:

- Braintree has no currency field. Set the default currency with `--currency USD` (CLI), `inputs.defaultCurrency` in
  `.l3lint.yaml`, `context.currency` in an MCP tool call, or `AdapterOptions.DefaultCurrency` in the library (ADR-0056);
  without it the currency is absent and GEN-FMT-002 reports it.
- **Unconfirmed**: Braintree does not say `orderId` reaches the networks as the invoice number; the spec's mapping is kept.
- A body with only `amount` and `lineItems` at the top level also looks like a canonical payload without
  `schemaVersion`, so detection reports a tie. Real sale requests carry a nonce or token, which settles it; otherwise
  pass `--format braintree`.
- The GraphQL API nests the same data differently and is not read.

## Cybersource (`cybersource`)

Sources: [Visa Platform Connect Level 2 and 3 guide](https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3.html),
[Level III sale example](https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-processing-ctv/l23-l3-processing-sale-intro/l23-l3-processing-sale-ex-rest-ctv.html),
[taxable field](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-taxable.md),
[ship-from postal code](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-from-postal-code.md),
[line item model](https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationLineItems.md).

O is `orderInformation`.

| Canonical field | Cybersource path | Units | Status |
| --- | --- | --- | --- |
| `amount` | O `amountDetails.totalAmount` | decimal string | Confirmed |
| `currency` | O `amountDetails.currency` | ISO code | Confirmed |
| `tax.amount` | O `amountDetails.taxAmount`; else the sum of line tax | decimal string | Confirmed |
| `tax.indicator` | O `invoiceDetails.taxable` (true is taxable, false is exempt) | boolean or "true"/"false" | Corrected (spec: derived from tax) |
| `customer.reference` | O `invoiceDetails.purchaseOrderNumber` | text | Confirmed |
| `order.invoiceNumber` | `clientReferenceInformation.code` | text | Confirmed |
| `amounts.freight` | O `amountDetails.freightAmount` | decimal string | Confirmed |
| `amounts.duty` | O `amountDetails.dutyAmount` | decimal string | Confirmed |
| `amounts.discount` | O `amountDetails.discountAmount` | decimal string | Confirmed |
| `shipping.fromPostalCode` | O `shippingDetails.shipFromPostalCode` | text | Corrected (spec: to confirm) |
| `shipping.toPostalCode` | O `shipTo.postalCode` | text | Confirmed |
| `shipping.toCountry` | O `shipTo.country` | alpha-2 | Confirmed |
| `billing.postalCode` | O `billTo.postalCode` | text | Confirmed (published example) |
| line items | O `lineItems[]` | | Confirmed |
| `description` | `productName` | text | Confirmed |
| `productCode` | `productCode`, else `productSku` or `productSKU` | text | Unconfirmed (see below) |
| `commodityCode` | `commodityCode` | text | Confirmed |
| `quantity` | `quantity` | integer or text | Confirmed |
| `unitOfMeasure` | `unitOfMeasure` | text | Confirmed |
| `unitCost` | `unitPrice` | decimal string | Confirmed |
| `discountAmount` | `discountAmount` | decimal string | Confirmed |
| `taxAmount` | `taxAmount` | decimal string | Confirmed |
| `total` | `totalAmount` | decimal string | Confirmed |
| `amountIncludesTax` | `amountIncludesTax` | boolean, "true"/"false" or "Y"/"N" | Confirmed (both types accepted) |

Not carried: `amounts.shippingTax`, `order.orderDate`, line `debitCreditIndicator` (credits use a separate endpoint).

Detection: an `orderInformation` object; `clientReferenceInformation`, `processingInformation` or
`paymentInformation`, and `amountDetails` or `lineItems`, raise the score.

Derived: `/tax/amount` when the header tax is absent and lines carry tax. The order-level tax field applies to few
processors; the Visa Platform Connect guide sends tax per line.

Limitations:

- `taxable` false with a tax above 0 is mapped as exempt (as Cybersource defines it) with a note.
- **Unconfirmed**: `productCode` is documented as a product category but reaches the network product code for most
  processors; `productSku` identifies the item. `productCode` is used when present, else the SKU, with a note.
  The field reference writes `productSKU`, the SDK `productSku`; both are read.
- `purchaseOrderDate` is not mapped: the spec lists the order date as not supported for Cybersource.

## Authorize.net (`authorizenet`)

Sources: [API reference, Charge a Credit Card](https://developer.authorize.net/api/reference/index.html),
[public XML schema](https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd),
[Ruby SDK LineItemType](https://rubydoc.info/gems/authorizenet/AuthorizeNet/API/LineItemType).

T is `createTransactionRequest.transactionRequest`, or a bare top-level `transactionRequest`.

| Canonical field | Authorize.net path | Units | Status |
| --- | --- | --- | --- |
| `amount` | T `amount` | decimal, number or quoted | Confirmed |
| `currency` | T `currencyCode`, else the default currency option | ISO code | Corrected (spec: from config) |
| `tax.amount` | T `tax.amount` | decimal | Confirmed |
| `tax.indicator` | T `taxExempt` (true is exempt, false is taxable) | boolean or quoted | Confirmed |
| `customer.reference` | T `poNumber` | text | Confirmed |
| `order.invoiceNumber` | T `order.invoiceNumber` | text | Confirmed |
| `amounts.freight` | T `shipping.amount` | decimal | Confirmed |
| `amounts.duty` | T `duty.amount` | decimal | Confirmed |
| `amounts.discount` | T `discount.amount` | decimal | Corrected (spec: not supported) |
| `shipping.fromPostalCode` | T `shipFrom.zip` | text | Corrected (spec: not supported) |
| `shipping.toPostalCode` | T `shipTo.zip` | text | Confirmed |
| `shipping.toCountry` | T `shipTo.country` | free text; alpha-3 read as alpha-2 | Confirmed |
| `billing.postalCode` | T `billTo.zip` | text | Confirmed (published example) |
| line items | T `lineItems.lineItem` (one object or an array) | | Confirmed; array form unconfirmed |
| `description` | `name`, else `description` | text | Confirmed |
| `productCode` | `productCode`, else `productSKU` | text | Confirmed |
| `commodityCode` | `commodityCode` | text | Confirmed |
| `quantity` | `quantity` | decimal | Confirmed |
| `unitOfMeasure` | `unitOfMeasure` | text | Confirmed |
| `unitCost` | `unitPrice` | decimal | Confirmed |
| `discountAmount` | `discountAmount` | decimal | Confirmed |
| `taxAmount` | `taxAmount` | decimal | Confirmed |
| `total` | `totalAmount` | decimal | Confirmed |
| `taxable` (line) | `taxable` | boolean or quoted | Confirmed (schema) |
| `amountIncludesTax` | `taxIncludedInTotal` | boolean or quoted | Unconfirmed (schema only) |

Not carried: `amounts.shippingTax`, `order.orderDate`, line `debitCreditIndicator` (always debit).

Detection: a root `createTransactionRequest` object (with `transactionRequest` and `merchantAuthentication` it scores
1.0), or a root `transactionRequest` object.

Derived: none.

Limitations:

- The published JSON example quotes every value; numbers and booleans are read either way and fix patches are written
  as quoted strings, like the example.
- **Unconfirmed**: several items as a JSON array are implied by the 30-item cap but not shown; both one object and an
  array are read. A `lineItems` array without the `lineItem` level is reported as one GEN-FMT-001 finding.
- **Unconfirmed**: the currency used when `currencyCode` is absent. The default currency option is used, else the
  currency stays absent.
- `shipTo.country` is free text. A value that is neither an alpha-2 nor an alpha-3 code is copied with a note and the
  schema check reports it.

## Checkout.com (`checkoutcom`)

Sources: [submit Level 2 or Level 3 data](https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data),
[format the amount value](https://www.checkout.com/docs/payments/accept-payments/format-the-amount-value),
[terminology (reference)](https://www.checkout.com/docs/business-operations/terminology),
[.NET SDK ProcessingSettings](https://raw.githubusercontent.com/checkout/checkout-sdk-net/master/src/CheckoutSdk/Payments/ProcessingSettings.cs).

| Canonical field | Checkout.com path | Units | Status |
| --- | --- | --- | --- |
| `amount` | `amount` | integer, minor units | Confirmed |
| `currency` | `currency` | ISO code | Confirmed |
| `tax.amount` | `processing.tax_amount` | integer, minor units | Confirmed |
| `tax.indicator` | derived: taxable when tax is above 0 | | Confirmed (no order-level flag) |
| `customer.reference` | `processing.order_id` | text | Confirmed |
| `order.invoiceNumber` | `reference` | text | Confirmed |
| `amounts.freight` | `processing.shipping_amount` | integer, minor units | Confirmed |
| `amounts.duty` | `processing.duty_amount` | integer, minor units | Confirmed |
| `amounts.discount` | `processing.discount_amount` | integer, minor units | Corrected (spec: not supported) |
| `amounts.shippingTax` | `processing.shipping_tax_amount` | integer, minor units | Corrected (spec: not supported) |
| `shipping.fromPostalCode` | `shipping.from_address_zip` | text | Corrected (spec: to confirm) |
| `shipping.toPostalCode` | `shipping.address.zip` | text | Confirmed |
| `shipping.toCountry` | `shipping.address.country` | alpha-2 | Confirmed |
| line items | `items[]` | | Confirmed |
| `description` | `name` | text | Confirmed |
| `productCode` | `reference` | text | Confirmed |
| `commodityCode` | `commodity_code` | text | Confirmed |
| `quantity` | `quantity` | integer | Confirmed |
| `unitOfMeasure` | `unit_of_measure` | text | Confirmed |
| `unitCost` | `unit_price` | integer, minor units | Confirmed |
| `discountAmount` | `discount_amount` | integer, minor units | Confirmed |
| `taxAmount` | `tax_amount` | integer, minor units | Confirmed |
| `total` | `total_amount` | integer, minor units | Confirmed |
| `taxable` (line) | `tax_exempt`, inverted | boolean | Corrected (line-level flag found) |

Not carried: `order.orderDate`, `billing.postalCode`, line `amountIncludesTax` (Checkout.com's line total excludes tax)
and `debitCreditIndicator` (always debit).

Detection: a `processing` object and an `items` array with snake_case keys; `source` or `processing_channel_id` and an
integer `amount` raise the score.

Derived: `/tax/indicator` when tax is above 0. All items marked `tax_exempt` with zero tax do not make the sale exempt;
Visa then reports the zero tax.

Limitations:

- Checkout.com leaves duty and shipping tax out of the payment amount, while l3lint's total formula adds them. Either
  above 0 adds a note, and the transaction total check may report it.
- Checkout.com asks for a trailing 0 on three-decimal currency amounts; l3lint converts with exponent 3 and does not
  check that rule.

## CSV (`csv`)

CSV files need a column map: `l3lint lint invoices.csv --format csv --map erp-map.yaml`. Rows that share the
`groupBy` value form one transaction, in the order each value first appears; each row is one line item.

```yaml
format: csv
delimiter: ","            # optional, one character; a comma by default
groupBy: invoice_number   # the column that identifies the transaction
amountUnits: major        # optional: major (12.50, the default) or minor (1250)
transaction:
  id: invoice_number
  amount: invoice_total
  currency: { const: USD }
  card.brand: card_brand
  card.product: { const: purchasing }
  merchant.mcc: { const: "5047" }
  merchant.name: { const: Example Medical Supply }
  customer.reference: po_number
  tax.amount: tax_total
  tax.indicator: { from: taxable_flag, map: { Y: taxable, N: exempt } }
  amounts.freight: { from: freight, default: 0 }
lineItems:
  description: item_description
  productCode: sku
  commodityCode: unspsc
  quantity: qty
  unitOfMeasure: uom
  unitCost: unit_price
  discountAmount: { from: line_discount, default: 0 }
  total: line_total
```

Each entry maps a canonical field (dotted names under `transaction`, line field names under `lineItems`) to one of:

| Form | Meaning |
| --- | --- |
| `column_name` | The cell of that column. |
| `{ const: value }` | The same value for every transaction. |
| `{ from: column, map: { A: x }, default: value }` | The cell, translated through `map` when it matches a key; `default` when the cell is empty. A cell that matches no key passes through unchanged. |

Rules:

- Values convert to the field's type with the invariant culture: numbers like `12.50`, dates `yyyy-MM-dd`, booleans
  `true`, `false`, `Y`, `N`, `yes`, `no`, `1` or `0`, and enum values as the canonical names (`visa`, `purchasing`,
  `taxable`, `credit` and so on). `amex.adviceText` takes all advice lines from one cell, separated by `|`.
- With `amountUnits: minor`, amount columns (amounts, taxes, discounts, freight, duty, totals and unit cost) must be
  whole numbers and are divided by the transaction's currency exponent.
- Transaction-level values must agree across a transaction's rows. An empty cell does not count as a disagreement.
  When two rows send different values the field is left out and reported once as GEN-FMT-001 at the second row's cell.
- Without a `sequence` column, lines are numbered by row order within the transaction.
- Every cell is checked for card numbers before mapping and masked; GEN-PCI-001 points at the cell's line and column.
- Source paths are labels like `row 14, column F (unit_price)`, where the row is the file line where the record starts.
  Fix patches for CSV input target the canonical document, because CSV has no pointer syntax.
- A missing map, a map that is not valid YAML, a map that does not match `csvmap.schema.json` (the message names the
  map file and the JSON Pointer of the entry), a map that names a column the file lacks, and a file without a header
  row are input errors (exit code 2).
- `pci.allowFields` applies to CSV through the map: a column is exempt when the canonical field it maps to matches an
  allowed pointer (for example `/lineItems/*/description` exempts the column mapped to `description`). Cells of every
  other column are checked, masked and reported.

## Fixtures

`fixtures/adapters/<format>/` holds ten fixtures per adapter. Each has the input, a hand-written expected canonical
transaction and an `expect.yaml` with the detected format, the lint context and the expected findings. The
expectations were written from the mapping tables above and the rule catalog, not copied from l3lint's output.
