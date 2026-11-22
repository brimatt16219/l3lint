# Verification of asterisked adapter fields

ADR-0026 and clarification 16. The Architecture tab's adapter mapping table marks 35 cells with an asterisk ("names to
confirm against current docs while building the adapter"). Each was checked against the gateway's public documentation
in `docs/dev/research/adapters.md` (retrieved 2026-10-25). Stripe and Adyen have no asterisked cells. Fields corrected
by the research but not asterisked in the spec (Authorize.net currency and ship-from, the order date candidates) are
listed after the table.

Status: **confirmed** (the spec's mapping matches the docs), **corrected** (the docs show another path, which is
implemented), **unconfirmed** (no public page settled it; the spec's mapping is kept and marked in
`docs/articles/adapters.md`).

| Field | Gateway | Status | Current path | Source | Retrieved | Behavior chosen |
| --- | --- | --- | --- | --- | --- | --- |
| `tax.indicator` | Braintree | confirmed | `taxExempt` (boolean) | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | true maps to exempt, false to taxable; an Ambiguous note when the flag disagrees with the tax amount; a missing flag stays missing |
| `customer.reference` | Braintree | confirmed | `purchaseOrderNumber` | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | mapped as text |
| `order.invoiceNumber` | Braintree | unconfirmed | `orderId` | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | the field exists, but Braintree does not say it reaches networks as the invoice number; spec mapping kept |
| `amounts.duty` | Braintree | confirmed | not supported | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | path map entry is null; Visa reports the missing duty |
| `shipping.toPostalCode` | Braintree | confirmed | `shipping.postalCode` | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | mapped as text |
| `shipping.toCountry` | Braintree | confirmed | `shipping.countryCodeAlpha2`, also `shipping.countryCodeAlpha3` | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/node | 2026-10-25 | alpha-2 when present, else alpha-3 converted to alpha-2 through an embedded ISO 3166-1 table; patches go back as alpha-3 when the source sent alpha-3 |
| `description` | Braintree | confirmed | `lineItems[].name` | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ | 2026-10-25 | `name` only; the separate `description` field is ignored |
| `unitCost` | Braintree | confirmed | `lineItems[].unitAmount` | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | decimal string, up to 4 decimals |
| `amountIncludesTax` | Braintree | confirmed | not sent (line total excludes tax) | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ | 2026-10-25 | left absent, which means false |
| `debitCreditIndicator` | Braintree | confirmed | `lineItems[].kind` | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node | 2026-10-25 | `debit` or `credit`, any case; anything else is an Invalid note |
| `tax.indicator` | Cybersource | corrected | `orderInformation.invoiceDetails.taxable` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-taxable.md | 2026-10-25 | true maps to taxable, false to exempt; boolean or "true"/"false" text accepted (types unconfirmed); not derived from tax because the flag exists |
| `customer.reference` | Cybersource | confirmed | `orderInformation.invoiceDetails.purchaseOrderNumber` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-user-po.md | 2026-10-25 | mapped as text |
| `order.invoiceNumber` | Cybersource | confirmed | `clientReferenceInformation.code` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/merchant-reference-code.md | 2026-10-25 | mapped as text; `invoiceDetails.invoiceNumber` is not read |
| `amounts.shippingTax` | Cybersource | confirmed | not supported | https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationAmountDetails.md | 2026-10-25 | path map entry is null |
| `shipping.fromPostalCode` | Cybersource | corrected | `orderInformation.shippingDetails.shipFromPostalCode` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-from-postal-code.md | 2026-10-25 | mapped as text |
| `shipping.toPostalCode` | Cybersource | confirmed | `orderInformation.shipTo.postalCode` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-postal-code.md | 2026-10-25 | mapped as text |
| `shipping.toCountry` | Cybersource | confirmed | `orderInformation.shipTo.country` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-country.md | 2026-10-25 | alpha-2 copied; an alpha-3 value is converted |
| `productCode` | Cybersource | unconfirmed | `orderInformation.lineItems[].productCode` or `productSku` | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-code.md | 2026-10-25 | `productCode` when present; else `productSku` (or `productSKU`) with an Ambiguous note and the path map moved to the SKU; a different SKU next to a product code adds a note |
| `debitCreditIndicator` | Cybersource | confirmed | not sent (debit) | https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationLineItems.md | 2026-10-25 | left absent, which means debit |
| `tax.amount` | Authorize.net | confirmed | `transactionRequest.tax.amount` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | decimal, number or quoted |
| `tax.indicator` | Authorize.net | confirmed | `transactionRequest.taxExempt` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | true maps to exempt, false to taxable; patches written as quoted "true"/"false" |
| `customer.reference` | Authorize.net | confirmed | `transactionRequest.poNumber` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | mapped as text |
| `order.invoiceNumber` | Authorize.net | confirmed | `transactionRequest.order.invoiceNumber` | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | mapped as text |
| `amounts.freight` | Authorize.net | confirmed | `transactionRequest.shipping.amount` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | decimal, number or quoted |
| `amounts.duty` | Authorize.net | confirmed | `transactionRequest.duty.amount` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | decimal, number or quoted |
| `amounts.discount` | Authorize.net | corrected | `transactionRequest.discount.amount` | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | mapped; the schema-only `order.discountAmount` is not read |
| `shipping.toPostalCode` | Authorize.net | confirmed | `transactionRequest.shipTo.zip` | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | mapped as text |
| `shipping.toCountry` | Authorize.net | confirmed | `transactionRequest.shipTo.country` (free text, 60) | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | alpha-2 copied, alpha-3 converted; other text copied with an Ambiguous note (the schema check then reports it) |
| `amounts.discount` | Checkout.com | corrected | `processing.discount_amount` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | integer, minor units |
| `amounts.shippingTax` | Checkout.com | corrected | `processing.shipping_tax_amount` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | integer, minor units; above 0 adds a note because Checkout.com leaves it out of the amount |
| `shipping.fromPostalCode` | Checkout.com | corrected | `shipping.from_address_zip` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | mapped as text |
| `shipping.toPostalCode` | Checkout.com | confirmed | `shipping.address.zip` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | mapped as text |
| `shipping.toCountry` | Checkout.com | confirmed | `shipping.address.country` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | alpha-2 copied |
| `amountIncludesTax` | Checkout.com | confirmed | not sent (line total excludes tax) | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | left absent, which means false |
| `debitCreditIndicator` | Checkout.com | confirmed | not sent (debit) | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | left absent, which means debit |

## Other changes from the research

| Field | Gateway | Spec | Status | Current path | Source | Retrieved | Behavior chosen |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `currency` | Authorize.net | From config | corrected | `transactionRequest.currencyCode` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | `currencyCode` when sent, else the default currency option with a Defaulted note (the default when omitted is unconfirmed) |
| `shipping.fromPostalCode` | Authorize.net | Not supported | corrected | `transactionRequest.shipFrom.zip` | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | mapped as text |
| `order.orderDate` | Cybersource | Not supported | not mapped (spec says not supported); candidate field exists | `orderInformation.invoiceDetails.purchaseOrderDate` | https://raw.githubusercontent.com/CyberSource/cybersource-rest-client-dotnet/master/docs/Ptsv2paymentsOrderInformationInvoiceDetails.md | 2026-10-25 | left unmapped to follow the spec table; the owner may add it in a spec update |
| `order.orderDate` | Authorize.net | Not supported | not mapped (spec says not supported); candidate field exists in the schema | `transactionRequest.order.purchaseOrderDateUTC` | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd | 2026-10-25 | left unmapped to follow the spec table |
| `amountIncludesTax` | Authorize.net | `taxIncludedInTotal` | unconfirmed (schema only) | `lineItems.lineItem[].taxIncludedInTotal` | https://rubydoc.info/gems/authorizenet/AuthorizeNet/API/LineItemType | 2026-10-25 | read as a boolean or quoted boolean |
| line items | Authorize.net | `lineItem[]` | unconfirmed (array form) | `transactionRequest.lineItems.lineItem` | https://developer.authorize.net/api/reference/index.html | 2026-10-25 | one object or an array |
| `tax.indicator` (line) | Checkout.com | Derived from tax | corrected (partial) | `items[].tax_exempt` | https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data | 2026-10-25 | inverted into the line `taxable` flag; the order-level indicator is still derived from tax |
| Level 2 and 3 object | Adyen | ESD keys | confirmed, plus a newer form | `enhancedSchemeData.levelTwoThree` | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 | 2026-10-25 | both forms read; the structured object wins when both are present (precedence unconfirmed) |
| line numbering | Adyen (legacy) | `itemDetailLine1.*` | unconfirmed (base and maximum) | `additionalData["enhancedSchemeData.itemDetailLine{N}.*"]` | https://docs.adyen.com/online-payments/classic-integrations/hosted-payment-pages/hpp-payments-with-l2l3-data | 2026-10-25 | lines read in ascending number order; a gap adds a note |
| `shipping.toCountry` | Stripe | Not supported | unconfirmed | top-level `shipping.address.country` exists | https://docs.stripe.com/api/payment_intents/create?query=shipping | 2026-10-25 | not mapped; the Level 2 and 3 guide does not say Stripe forwards it |
