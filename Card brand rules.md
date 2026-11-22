# 2. Card brand rules

## How enhanced data works

Enhanced data rides with the authorization or the capture, but the network judges it at clearing; whether a transaction earns a lower rate depends on the card product, the merchant's MCC, the data itself and, for Visa, the merchant's verification status.

**Where the data travels**

1. The merchant system builds a payment request with order, tax and line-item fields.
2. The gateway validates format (often only format) and forwards it. Some gateways drop invalid Level 3 data and fall back to Level 2 ([Adyen](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3/l2-l3-details-in-webhooks)); others refuse line items whose math does not add up ([Stripe](https://docs.stripe.com/payments/payment-line-items)).
3. Level 3 data can be sent at authorization or capture. When both carry it, the capture usually wins ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards), [BlueSnap](https://developers.bluesnap.com/v8976-Basics/reference/level-23-data)).
4. The card network receives the line items at settlement, applies its qualification rules and assigns an interchange category.
5. The issuer shows the detail to the cardholder's business for reconciliation.

l3lint runs before step 1 ends, on the request the merchant is about to send. That is the cheapest moment to fix data.

**What decides qualification**

| Factor | What matters | l3lint input |
| --- | --- | --- |
| Card product | Only commercial cards qualify. Visa Product 3 covers purchasing, corporate and business cards; Mastercard Level 3 covers purchasing and corporate only ([Stripe](https://docs.stripe.com/payments/payment-line-items)). | `card.product`, `card.productCode` |
| Merchant category | Restaurants, lodging, airlines, car rental, travel agencies and others are excluded; the lists differ by brand and level ([Stripe](https://docs.stripe.com/payments/payment-line-items)). | `merchant.mcc` |
| Data completeness | Every required field present for the target level. | The payload |
| Data quality | Since CEDP, Visa also judges whether values are real: no generic text, no filler, math that reconciles. | The payload |
| Tax | Level 2 needs tax within a brand-specific range unless the MCC or an explicit exemption applies. | `tax.*`, `merchant.mcc` |
| Merchant status (Visa) | Verified merchants get Product 3 at settlement; others wait for after-the-fact validation. | Out of scope; reported as context |

**What a downgrade looks like.** The transaction settles in a commercial card-present, card-not-present or standard category instead of Product 3, Data Rate III or Data Rate II. Nothing fails at checkout, which is why a linter is needed at all.

## Visa: Commercial Enhanced Data Program

Visa pays Product 3 rates only when a US domestic commercial credit transaction carries the CEDP flag and a complete, accurate, invoice-quality set of line items, and the merchant is verified or the transaction validates after settlement.

**Program mechanics**

- **Flag and fee.** Populating enhanced data attaches a CEDP flag that tells Visa to review the transaction; the program fee is 5 basis points ([PayPal](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program)). The fee applies whether or not the data passes ([Spreedly](https://www.spreedly.com/blog/what-is-visa-cedp-commercial-enhanced-data-program)).
- **Verification.** A merchant becomes verified after 500 successful CEDP transactions or 20 consecutive days of them ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/)). Verified status can be revoked after notice if quality drops ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program)).
- **Lagged interchange.** Non-verified transactions settle at standard rates; if the data validates, a credit posts about 10 to 15 days after settlement ([Merchant Cost Consulting](https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/)).
- **2026 changes.** Verification paused in November 2025 and resumed February 24, 2026; Visa also introduced exclusion of merchants with consistently poor data ([TabaPay](https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update)).
- **Large Ticket.** Large Ticket rates still apply to corporate and purchasing cards and use the same CEDP flag ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program), [PayPal](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program)).

**Eligibility**

- Cards: US domestic Visa commercial credit, meaning corporate, purchasing, fleet and business cards ([PayPal](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program), [Stripe](https://docs.stripe.com/payments/payment-line-items)). One processor lists commercial prepaid as ineligible ([Payrix](https://resource.payrix.com/resources/understanding-interchange-data-requirements)).
- MCCs excluded from Product 3: 3000 to 3999, 4000 to 4010, 4112, 4411, 4511, 4722, 5812, 5814, 7011, 7512 and 7513. Federal government corporate and purchasing cards are also excluded at MCCs 8011, 8021, 8031, 8041, 8042, 8043, 8046, 8050, 8062 and 8071 ([Stripe](https://docs.stripe.com/payments/payment-line-items)).

**Required data for Product 3**

Visa requires the Level 2 fields plus 12 enhanced fields ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program)). Older processor guidance lists the same set split into a summary record and a line-item record, and adds an item sequence number ([Cybersource community](https://community.developer.cybersource.com/t5/Integration-and-Testing/Submitting-Level-2-Data/m-p/60293)).

| Record | Field | Canonical path | Notes |
| --- | --- | --- | --- |
| Level 2 | Sales tax amount and tax indicator | `tax.amount`, `tax.indicator` | Fuel MCCs on purchasing cards need a customer code instead |
| Summary | Discount amount | `amounts.discount` | 0 when no discount |
| Summary | Freight or shipping amount | `amounts.freight` | 0 when nothing ships |
| Summary | Duty amount | `amounts.duty` | 0 when no duty |
| Line item | Item sequence number | `lineItems[].sequence` | 1-based, contiguous |
| Line item | Item commodity code | `lineItems[].commodityCode` | Usually UNSPSC |
| Line item | Item descriptor | `lineItems[].description` | Real product or service name |
| Line item | Product code | `lineItems[].productCode` | SKU or part number |
| Line item | Quantity | `lineItems[].quantity` | Greater than 0 |
| Line item | Unit of measure | `lineItems[].unitOfMeasure` | For example EA, BX, HR |
| Line item | Unit cost | `lineItems[].unitCost` | Before discount |
| Line item | Discount per line item | `lineItems[].discountAmount` | 0 when no discount |
| Line item | Line-item total | `lineItems[].total` | Extended amount |
| Line item | Line-item detail indicator | `lineItems[].detailIndicator` | Often set by the processor |

**What Visa rejects, per public sources**

- Generic descriptions such as "Product", "Service", "Item", "Goods", "N/A", a merchant name, a generic label or a single character ([Versapay](https://www.versapay.com/cedp), [Merchant Cost Consulting](https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/), [Finix](https://finix.com/resources/blogs/visa-level-2-3-programs-changing-cedp)).
- Blanks, zeros and filler text in required fields ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program)).
- Math that does not reconcile: unit cost times quantity minus line discount must equal the line total, and the transaction amount must equal line totals plus tax, shipping and duty, less discounts ([Curbstone](https://curbstone.com/new-data-requirements-cedp/), [Epicor](https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/)).
- A zero tax amount presented as a tax-exempt sale. Exemption must be flagged explicitly ([Versapay](https://www.versapay.com/cedp), [Forbes Business Council](https://www.forbes.com/councils/forbesbusinesscouncil/2025/06/27/what-merchants-should-know-about-visas-commercial-enhanced-data-program/)).

**Level 2 rules kept for historical dates.** Until the April 2026 sunset, Visa Level 2 needed a tax amount between 0.1% and 22% of the transaction amount, except at fuel MCCs 4468, 5499, 5541, 5542 and 5983 ([Braintree](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/), [Stripe](https://docs.stripe.com/payments/payment-line-items)). These rules stay in the pack with an end date so `--as-of` can lint older payloads.

## Mastercard requirements

Mastercard still pays for both tiers: Data Rate II for Level 2 and Data Rate III for Level 3. CEDP did not touch Mastercard ([Checkout.com](https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data)), but unit-of-measure requirements tightened in October 2025 ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/), [Navan](https://navan.com/resources/glossary/level-3-data)).

**Eligibility**

- Level 2: business, corporate and purchasing cards. Level 3: purchasing and corporate cards only ([Stripe](https://docs.stripe.com/payments/payment-line-items)).
- Card products MBS (Mastercard B2B Product 1) and MEF (Electronic Payment Account) are ineligible for Level 3 ([Payrix](https://resource.payrix.com/docs/interchange-data-requirements)).
- MCCs excluded from Level 2: 3000 to 3350, 3351 to 3500, 3501 to 3999, 4112, 4511, 5812, 7011, 7512, 7513 and 7519. Level 3 adds 4468, 5499, 5541, 5542, 5983 and 8398 to that list ([Stripe](https://docs.stripe.com/payments/payment-line-items)).

**Level 2 (Data Rate II)**

| Field | Canonical path | Rule |
| --- | --- | --- |
| Sales tax amount | `tax.amount` | Between 0.1% and 30% of the transaction amount ([Braintree](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/)), unless the MCC is one of 4111, 4131, 4215, 4468, 4784, 5499, 5541, 5542, 5983, 7511, 8211, 8220, 8398, 8661, 9211, 9222, 9311, 9399, 9402 or 9752 ([Stripe](https://docs.stripe.com/payments/payment-line-items)) |
| Tax indicator | `tax.indicator` | Required ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)) |
| Customer code or purchase order number | `customer.reference`, `order.purchaseOrderNumber` | Required and non-blank ([PaymentCloud](https://pcdocs.paymentcloudinc.com/developer-docs/processing-payments/level2)) |
| Merchant tax ID | `merchant.taxId` | Listed as required by one processor ([Epicor](https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/)); a warning, not an error |

**Level 3 (Data Rate III)**

Line items need a description, product code, quantity, unit of measure, unit price, extended amount, debit or credit indicator, and either a valid tax amount or a tax-exempt indicator ([Epicor](https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/)). One large processor also requires a purchase order number, a ship-to postal code, a line tax indicator, a line discount indicator, a line tax type code and a line tax amount ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)).

| Limit | Value | Source |
| --- | --- | --- |
| Line items per transaction | 998 | [J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards) |
| Amount fields | 9 digits, truncated from the right when longer | [J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards) |
| Unit of measure | 3 characters at Chase, Elavon and FDC Compass | [Cybersource](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html) |

**Unit-of-measure code list.** Mastercard accepts a fixed list of three-letter codes ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)). The rule pack embeds it and `MC-L3-005` rejects anything else:

```csv
code,meaning
ACR,Acre
ARE,Are
CLT,Centiliter
CMT,Centimeter
EAC,Each
FOT,Foot
GLI,Gallon (US and GB)
ONZ,Ounce (US and GB)
PTI,Pint (US and GB)
QTI,Quart (US and GB)
GRM,Gram
HAR,Hectare
INH,Inch
KGM,Kilogram
KMT,Kilometer
LTR,Liter
MTR,Meter
NMI,Mile
MLI,Milliliter
MMT,Millimeter
PCE,Piece
LBR,Pound
CMK,Square centimeter
MTK,Square meter
MMK,Square millimeter
FTK,Square foot
INK,Square inch
KMK,Square kilometer
MIK,Square mile
YDK,Square yard
TNE,Ton
YRD,Yard
```

The `fix` command maps common free-text units to this list (each to EAC, piece or pc to PCE, lb to LBR, gal to GLI) and leaves anything ambiguous for a person to choose.

## American Express requirements

American Express has no public Level 3 rate program; Level 2 savings depend on a direct agreement, and the main payoff is cleaner reporting for the cardholder's business. l3lint therefore checks Amex data for format and completeness so nothing is truncated or rejected, and it says when no rate benefit should be expected.

**Program facts**

- Merchants must get Amex approval before Level 2 data counts ([PayPal](https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984), [Cybersource](https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview.md)).
- Cost savings on line items require a direct agreement with Amex, and one gateway forwards only the first 4 line items to Amex ([Stripe](https://docs.stripe.com/payments/payment-line-items)).
- Some processors send invoice detail as transaction advice text: up to 4 lines of 40 characters each, in a format certified with Amex ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)).

**Acquirer-level structured data.** Where an acquirer accepts structured Amex Level 2 and Level 3 records, the public rules look like this ([Mastercard Gateway, Amex acquirer](https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/)):

| Data | Rule |
| --- | --- |
| Merchant location | Level 2 is only sent for US and Canadian merchants (and US territories) |
| Ship-to postal code | Required for Level 2 |
| Purchase order number | Alphanumeric, at most 22 characters |
| Customer reference | Truncated to 17 or 20 characters depending on the settlement file |
| Merchant purchase reference | Truncated to 9 or 30 characters; a limited character set |
| Tax exempt | When tax status is exempt, a tax registration ID must be supplied |
| Charge descriptions | Up to 4 lines; defaults built from the PO number, requestor name and first two line items |
| Level 3 prerequisites | Level 2 data, transaction reference, customer reference, ship-to postal code, transaction tax amount, requestor name, and at least one line item with quantity, unit of measure, unit price and description |
| Unit of measure | A 2-character code from the ANSI X12 unit list (for example EA, BX, HR, CA) |

The 2-character length for Amex units also appears in Cybersource's field reference for American Express Direct, which points to the EDI unit code list ([Cybersource](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html)). The rule pack embeds that list as `amex-uom.csv` and validates against it.

**How l3lint treats Amex**

- `AMEX-ELIG-001` always reports, as information, that no public Level 3 rate exists and Level 2 pricing needs an Amex agreement.
- Format rules run against the selected processor profile: advice-text profile (4 by 40 characters) or structured profile (the table above).
- A payload with more than 4 line items gets a warning under profiles known to forward only the first 4.

## Discover requirements

Discover offers no Level 2 or Level 3 pricing ([Adobe Commerce](https://experienceleague.adobe.com/en/docs/commerce-merchant-services/payment-services/reporting/levels-card-payment-transactions), [PayPal](https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984)), so l3lint checks Discover payloads only for format and limits and tells the user that no rate benefit applies.

- Discover and Diners Club accept line-item data on any card and pass it through to the brand ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)).
- The limit is 98 line items per transaction; more causes an error at that processor ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)).
- The same processor marks the taxable flag as required for Discover Level 2 and every other Level 2 field as optional.

**Rules:** `DISC-ELIG-001` (information: no rate program), `DISC-FMT-001` (error: more than 98 line items), `DISC-L2-001` (warning: taxable flag missing under that processor profile), plus every `GEN` rule. If Discover publishes an enhanced-data program, it gets its own pack version with a start date.

## Canonical field dictionary

Every adapter maps into these 52 fields, and every rule reads only these fields. Brand columns say what each brand needs for its enhanced rate.

Legend: **R** required for the rate, **C** conditional (see the rule), **O** accepted but optional, **ctx** context the tool reads but the brand never receives, **-** not used.

**Transaction level**

| Path | Type | Constraint | Visa | MC | Amex | Disc |
| --- | --- | --- | --- | --- | --- | --- |
| `schemaVersion` | string | "1.0" | ctx | ctx | ctx | ctx |
| `id` | string | 1 to 64 chars, merchant's own reference | ctx | ctx | ctx | ctx |
| `amount` | decimal | Greater than 0; no more decimals than the currency exponent | R | R | R | R |
| `currency` | string | ISO 4217 alpha-3 | R | R | R | R |
| `transactionDate` | date | ISO 8601; drives rule effective dates | ctx | ctx | ctx | ctx |
| `channel` | enum | `cardPresent`, `cardNotPresent` | ctx | ctx | ctx | ctx |
| `card.brand` | enum | `visa`, `mastercard`, `amex`, `discover`, `unknown` | ctx | ctx | ctx | ctx |
| `card.product` | enum | `consumer`, `business`, `corporate`, `purchasing`, `fleet`, `commercialPrepaid`, `unknown` | ctx | ctx | ctx | ctx |
| `card.productCode` | string | Up to 3 chars, for example MBS or MEF | - | ctx | - | - |
| `card.isFederalGovernment` | boolean | Default false | ctx | - | - | - |
| `card.last4` | string | Exactly 4 digits; never a full card number | O | O | O | O |
| `merchant.mcc` | string | 4 digits | ctx | ctx | ctx | ctx |
| `merchant.name` | string | Used by data quality checks | ctx | ctx | ctx | ctx |
| `merchant.country` | string | ISO 3166-1 alpha-2 | ctx | ctx | ctx | ctx |
| `merchant.taxId` | string | Up to 15 chars | O | C | O | O |
| `customer.reference` | string | Customer code; up to 25 chars | C | R | C | O |
| `order.purchaseOrderNumber` | string | Up to 25 chars (22 for Amex) | O | R | C | O |
| `order.invoiceNumber` | string | Up to 25 chars | O | O | O | O |
| `order.orderDate` | date | ISO 8601 | O | O | O | O |
| `tax.amount` | decimal | 0 or more | R | R | C | O |
| `tax.indicator` | enum | `taxable`, `exempt`, `notProvided` | R | R | C | C |
| `tax.rate` | decimal | 0 to 1 (0.07 means 7%) | O | O | O | O |
| `tax.exemptionId` | string | Required with `exempt` under Amex profiles | O | O | C | O |
| `amounts.discount` | decimal | 0 or more; header-level discount | R | O | O | O |
| `amounts.freight` | decimal | 0 or more | R | C | O | O |
| `amounts.duty` | decimal | 0 or more | R | O | O | O |
| `amounts.shippingTax` | decimal | 0 or more | C | O | O | O |
| `amounts.handling` | decimal | 0 or more | O | O | O | O |
| `shipping.fromPostalCode` | string | Up to 10 chars | C | O | O | O |
| `shipping.toPostalCode` | string | Up to 10 chars | C | R | R | O |
| `shipping.toCountry` | string | ISO 3166-1 alpha-2 | C | O | O | O |
| `billing.postalCode` | string | Up to 10 chars | C | O | O | O |
| `amex.requestorName` | string | Up to 40 chars | - | - | C | - |
| `amex.adviceText` | string array | At most 4 lines of 40 chars | - | - | C | - |

**Line item level** (`lineItems[]`, 1 to 998 entries)

| Path | Type | Constraint | Visa | MC | Amex | Disc |
| --- | --- | --- | --- | --- | --- | --- |
| `sequence` | integer | 1-based, contiguous; adapters fill it from array order | R | O | O | O |
| `description` | string | Visa statements show 26 chars ([Stripe](https://docs.stripe.com/payments/payment-line-items)) | R | R | C | O |
| `productCode` | string | Up to 12 chars ([Stripe](https://docs.stripe.com/payments/payment-line-items)) | R | R | O | O |
| `commodityCode` | string | Up to 12 alphanumeric chars, no spaces ([Stripe](https://docs.stripe.com/payments/payment-line-items)) | R | O | O | O |
| `commodityScheme` | enum | `unspsc`, `nigp`, `hs`, `naics`, `other`, `unknown` | ctx | ctx | ctx | ctx |
| `quantity` | decimal | Greater than 0 | R | R | C | O |
| `unitOfMeasure` | string | Visa up to 12 chars; Mastercard list; Amex 2-char X12 code | R | R | C | O |
| `unitCost` | decimal | 0 or more, any precision | R | R | C | O |
| `discountAmount` | decimal | 0 or more | R | C | O | O |
| `discountIndicator` | boolean | Set when a discount applies | - | C | - | - |
| `taxAmount` | decimal | 0 or more | C | R | O | O |
| `taxRate` | decimal | 0 to 1 | C | C | O | O |
| `taxable` | boolean | Line tax indicator | O | R | O | O |
| `taxTypeCode` | string | Processor tax type code | - | C | O | - |
| `amountIncludesTax` | boolean | Default false; changes the arithmetic | ctx | ctx | ctx | ctx |
| `total` | decimal | Extended amount for the line | R | R | O | O |
| `debitCreditIndicator` | enum | `debit` (default) or `credit` | O | R | O | O |
| `detailIndicator` | string | Visa line-item detail indicator, 1 char | C | - | - | - |

**Example canonical payload** (commodity codes are illustrative)

```json
{
  "schemaVersion": "1.0",
  "id": "INV-20417",
  "amount": 280.36,
  "currency": "USD",
  "transactionDate": "2026-10-19",
  "channel": "cardNotPresent",
  "card": { "brand": "visa", "product": "purchasing", "last4": "4242" },
  "merchant": { "mcc": "5047", "name": "Gulfside Medical Supply", "country": "US" },
  "customer": { "reference": "PO-77812" },
  "order": { "purchaseOrderNumber": "PO-77812", "invoiceNumber": "INV-20417" },
  "tax": { "amount": 17.36, "indicator": "taxable", "rate": 0.07 },
  "amounts": { "discount": 0.00, "freight": 15.00, "duty": 0.00 },
  "shipping": { "fromPostalCode": "33602", "toPostalCode": "33431", "toCountry": "US" },
  "billing": { "postalCode": "33431" },
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
      "total": 120.00
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
      "total": 128.00
    }
  ]
}
```

This payload raises no Visa errors; it gets one warning (`VISA-L3-010`) because it leaves the line-item detail indicator for the processor to set. Sent on a Mastercard purchasing card it fails `MC-L3-005`, because BX and CA are not on Mastercard's unit list; the `fix` command proposes PCE for both and asks a person to confirm.

## Arithmetic and reconciliation rules

Four formulas cover every arithmetic rule. All math runs in `decimal` at full precision and rounds once, at the comparison, to the currency's exponent.

**1. Line extended amount** (`GEN-ARITH-001`). For line i with quantity q, unit cost u, line discount d and line tax t:

```latex
\text{expected}_i = \operatorname{round}_c\left(q_i \times u_i\right) - d_i + t_i \cdot I_i
```

Here I is 1 when `amountIncludesTax` is true and 0 otherwise, and round with subscript c rounds half away from zero to the currency exponent (2 for USD). The rule fails when the reported total differs from the expected value by more than the line tolerance. The formula matches the one processors publish for Visa: unit cost times quantity, less the line discount, equals the line total ([Epicor](https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/)).

**2. Transaction total** (`GEN-ARITH-002`). With header tax T, freight F, duty D, shipping tax S and header discount H:

```latex
A = \sum_{i} \left(\text{total}_i - t_i \cdot I_i\right) + T + F + D + S - H
```

Subtracting included line tax and adding T once keeps tax from counting twice, whichever convention the gateway uses. Braintree publishes this shape for US Visa ([Braintree](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/)); J.P. Morgan publishes the tax-inclusive variant ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)).

**3. Tax consistency** (`GEN-ARITH-003`). When any line carries tax, the header tax must equal the sum of line taxes within the total tolerance:

```latex
\left| T - \sum_{i} t_i \right| \le \varepsilon_{\text{total}}
```

**4. Tax ratio** (`VISA-L2-001`, `MC-L2-001`). The ratio r = T / A must fall between 0.001 and 0.22 for Visa Level 2 and between 0.001 and 0.30 for Mastercard Level 2, unless the MCC is exempt or `tax.indicator` is `exempt`.

**Tolerance and rounding settings**

| Setting | Default | Options | Why the default |
| --- | --- | --- | --- |
| `arithmetic.lineTolerance` | 1 minor unit (0.01 USD) | Any non-negative decimal | Absorbs one rounding step, nothing more |
| `arithmetic.totalTolerance` | 1 minor unit | Any non-negative decimal | Visa's exact tolerance is not public, so strict is safer |
| `arithmetic.toleranceMode` | `strict` | `strict`, `accumulated` (1 minor unit per line, capped at 5) | Some ERPs round per line; `accumulated` matches them |
| `arithmetic.rounding` | `awayFromZero` | `awayFromZero`, `toEven` | Matches common invoicing practice; `toEven` for ERPs that use banker's rounding |

Currency exponents come from an embedded ISO 4217 table (USD 2, JPY 0, BHD 3). A value with more decimals than its exponent raises `GEN-FMT-003` before any arithmetic runs. Unit cost is exempt from that check because unit prices often carry 3 or 4 decimals.

**Worked example** (the canonical payload above)

| Line | q | u | q times u | d | Expected | Reported | Difference | Result |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 12.50 | 125.00 | 5.00 | 120.00 | 120.00 | 0.00 | Pass |
| 2 | 2 | 64.00 | 128.00 | 0.00 | 128.00 | 128.00 | 0.00 | Pass |
| Total |  |  |  |  | 248.00 + 17.36 + 15.00 + 0.00 + 0.00 - 0.00 = 280.36 | 280.36 | 0.00 | Pass |

If line 1 had reported 125.00 (discount forgotten), `GEN-ARITH-001` would fire with expected 120.00, reported 125.00 and difference 5.00. `GEN-ARITH-002` would then compare against reported totals and also fail, and its message would say that the line-level failure is the likely cause, so the user fixes one thing, not two.

## Data quality heuristics

These checks look for data that is present but fake: the pattern Visa now rejects. They are warnings by default (errors for Visa where public sources name the exact pattern), and every detector is a pure function so it can be unit tested in isolation.

**Normalization before matching.** Trim, collapse inner whitespace, lowercase, strip punctuation except inner hyphens, and remove trailing sequence markers ("item 1", "item #2" become "item").

| Detector | Fires when | Default severity | Rules using it |
| --- | --- | --- | --- |
| Placeholder text | Normalized description is in the placeholder list below, is 1 character, is all digits, or repeats one character ("xxxx", "----") | Warning; error under Visa | `GEN-DQ-001`, `VISA-DQ-001` |
| Merchant name as description | Description equals `merchant.name`, or Jaro-Winkler similarity is at least 0.92 | Warning; error under Visa | `GEN-DQ-002`, `VISA-DQ-001` |
| Placeholder code | Product code or UOM is "0", "00", all zeros, "na", "n/a", "none", "sku", or equals the line sequence number | Warning; error under Visa | `GEN-DQ-003`, `VISA-DQ-002` |
| Filler customer code | Customer code or PO is "111111", "123456", "000000", "po", "none", "n/a" or "na" | Warning | `GEN-DQ-004` |
| One-line invoice | Exactly one line, quantity 1, line total equal to the transaction amount minus tax and freight | Warning | `GEN-DQ-005` |
| Generic commodity code | All zeros; or, for UNSPSC, not 8 digits, segment outside 10 to 95, or ending in 000000 (segment level) or 0000 (family level) | Warning | `GEN-DQ-006` |
| Duplicate lines | Two lines identical in description, product code, quantity and unit cost | Information | `GEN-DQ-008` |
| Static batch data | Across at least 20 transactions in one run, one description or one product code appears on at least 80% of them | Warning | `GEN-DQ-007` |
| Zero tax without exemption | `tax.amount` is 0 and `tax.indicator` is not `exempt` | Error under Visa | `VISA-L3-012` |

**Placeholder list** (shipped as `placeholders.txt` in the GEN pack, editable through config):

```csv
term
item
items
product
products
service
services
goods
merchandise
misc
miscellaneous
n/a
na
none
null
undefined
test
default
sale
purchase
order
invoice
fee
charge
various
other
general
description
line
```

The Visa-specific error escalation rests on public statements that Visa rejects "ITEM", "PRODUCT", "N/A", "Goods" and "Service", merchant names, generic labels and single characters ([Merchant Cost Consulting](https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/), [Verisave](https://verisave.com/blog/visa-level-2-sunset-cedp-data-quality), [Finix](https://finix.com/resources/blogs/visa-level-2-3-programs-changing-cedp)). The rest of the list, the similarity threshold and the batch percentages are l3lint's own inferences and carry the `inferred` label.

**Why the UNSPSC check is structural only.** l3lint does not ship the full UNSPSC code list, to avoid redistribution questions. It checks shape and specificity; a later release may validate against a list the user downloads.

## Rule catalog

The v1.0 catalog holds 78 rules: 23 general, 26 Visa, 17 Mastercard, 9 American Express and 3 Discover. Each row becomes one entry in a rule pack and one page in the rule reference. Severity is the default; config can change it.

**General (GEN): all brands**

| Rule | Severity | Check | Fix | Confidence | Effective |
| --- | --- | --- | --- | --- | --- |
| GEN-PCI-001 | Error | A value has 13 to 19 digits, passes the Luhn check and matches a major brand's prefix and length (warning in code fields) | Remove the card number; send only the last four in `card.last4` | policy | Always |
| GEN-FMT-001 | Error | Input fails the canonical JSON Schema | Correct the field named in the path | policy | Always |
| GEN-FMT-002 | Error | Currency missing or not ISO 4217 | Send a 3-letter code such as USD | policy | Always |
| GEN-FMT-003 | Error | Amount not positive, or more decimals than the currency allows | Round to the currency exponent before sending | policy | Always |
| GEN-FMT-004 | Warning | Descriptive text has characters outside letters, digits, space, apostrophe, period and hyphen | Replace them; some brands print them as spaces ([Braintree](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/)) | processor-doc | Always |
| GEN-FMT-005 | Error | More line items than the brand or processor allows | Consolidate lines or split the invoice | processor-doc | Always |
| GEN-FMT-006 | Warning | A field is longer than the brand or processor maximum | Shorten it; otherwise it is truncated | processor-doc | Always |
| GEN-FMT-007 | Error | Line sequence numbers missing, duplicated or not contiguous | Number lines 1, 2, 3 in order | processor-doc | Always |
| GEN-ARITH-001 | Error | Line total does not match formula 1 | Recompute the line total | processor-doc | Always |
| GEN-ARITH-002 | Error | Transaction amount does not match formula 2 | Recompute the amount, or fix the line or summary values | processor-doc | Always |
| GEN-ARITH-003 | Warning | Header tax differs from the sum of line taxes | Make header tax the sum of line taxes | processor-doc | Always |
| GEN-ARITH-004 | Error | Negative quantity or unit cost on a debit line | Mark returns as credit lines with positive values | processor-doc | Always |
| GEN-ARITH-005 | Warning | Header discount equals the sum of line discounts (counted twice) | Send the discount at one level only | processor-doc | Always |
| GEN-ELIG-001 | Information | Consumer card | None; enhanced data earns no rate on consumer cards | processor-doc | Always |
| GEN-ELIG-002 | Information | Card product unknown | Pass `card.product` to enable eligibility checks | policy | Always |
| GEN-DQ-001 | Warning | Placeholder description | Use the real product or service name | inferred | Always |
| GEN-DQ-002 | Warning | Description is the merchant name | Describe the item, not the seller | inferred | Always |
| GEN-DQ-003 | Warning | Placeholder product code or unit | Send the real SKU and unit | inferred | Always |
| GEN-DQ-004 | Warning | Filler customer code or PO | Ask the buyer for their real reference | inferred | Always |
| GEN-DQ-005 | Warning | One-line invoice that mirrors the total | Send the invoice's real lines | inferred | Always |
| GEN-DQ-006 | Warning | Generic or malformed commodity code | Use the most specific 8-digit UNSPSC code | inferred | Always |
| GEN-DQ-007 | Warning | Same description or product code on 80% or more of a batch | Pull line data from the invoice, not a template | inferred | Always |
| GEN-DQ-008 | Information | Duplicate lines in one transaction | Merge them or confirm they are separate | inferred | Always |

**Visa (VISA)**

| Rule | Severity | Check | Fix | Confidence | Effective |
| --- | --- | --- | --- | --- | --- |
| VISA-ELIG-001 | Information | Card product not business, corporate, purchasing or fleet | None; no Product 3 rate | processor-doc | From 2025-10-17 |
| VISA-ELIG-002 | Information | MCC on the Product 3 exclusion list | None; no Product 3 rate | processor-doc | From 2025-10-17 |
| VISA-ELIG-003 | Information | Commercial prepaid card | None; listed as ineligible by one processor | processor-doc | Always |
| VISA-ELIG-004 | Information | Federal government card at an excluded medical MCC | None | processor-doc | From 2025-10-17 |
| VISA-ELIG-005 | Warning | Card not present and no billing postal code | Send the billing postal code for address verification ([Adyen](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data)) | processor-doc | Always |
| VISA-L2-001 | Error | Tax ratio outside 0.1% to 22%, MCC not exempt, not marked exempt | Correct the tax amount or mark the sale exempt | processor-doc | Until 2026-04-17 |
| VISA-L2-002 | Error | Tax indicator missing | Send `taxable` or `exempt` | processor-doc | Until 2026-04-17 |
| VISA-L2-003 | Error | Purchasing card at a fuel MCC with no customer code | Send the customer code | processor-doc | Until 2026-04-17 |
| VISA-L3-001 | Error | No line items | Send the invoice lines | processor-doc | From 2025-10-17 |
| VISA-L3-002 | Error | Line item descriptor missing | Add the item name | processor-doc | From 2025-10-17 |
| VISA-L3-003 | Error | Product code missing | Add the SKU or part number | processor-doc | From 2025-10-17 |
| VISA-L3-004 | Error | Commodity code missing | Add the UNSPSC code | processor-doc | From 2025-10-17 |
| VISA-L3-005 | Error | Quantity missing or not above 0 | Send the real quantity | processor-doc | From 2025-10-17 |
| VISA-L3-006 | Error | Unit of measure missing | Send a unit such as EA or BX | processor-doc | From 2025-10-17 |
| VISA-L3-007 | Error | Unit cost missing | Send the unit price | processor-doc | From 2025-10-17 |
| VISA-L3-008 | Error | Line total missing | Send the extended amount | processor-doc | From 2025-10-17 |
| VISA-L3-009 | Error | Line discount missing | Send 0 when there is no discount | processor-doc | From 2025-10-17 |
| VISA-L3-010 | Warning | Line-item detail indicator missing | Confirm your processor sets it | processor-doc | From 2025-10-17 |
| VISA-L3-011 | Error | Summary discount, freight or duty missing | Send 0 for each that does not apply | processor-doc | From 2025-10-17 |
| VISA-L3-012 | Error | Tax neither above 0 nor marked exempt | Send the tax, or set `tax.indicator` to `exempt` | processor-doc | From 2025-10-17 |
| VISA-L3-013 | Warning | Freight above 0 but ship-from, ship-to or destination country missing | Send the shipping postal codes and country | processor-doc | From 2025-10-17 |
| VISA-L3-014 | Warning | Descriptor longer than 26 characters | Shorten it; statements show 26 | processor-doc | Always |
| VISA-L3-015 | Error | Commodity code over 12 chars or not alphanumeric without spaces | Send the bare code, for example 42132203 | processor-doc | Always |
| VISA-DQ-001 | Error | Placeholder description or merchant name as description | Use the real item name | processor-doc | From 2025-10-17 |
| VISA-DQ-002 | Error | Zero or filler in a required code field | Send real values | processor-doc | From 2025-10-17 |
| VISA-LT-001 | Information | Amount is high enough that Large Ticket rates may be cheaper | Ask your processor about Large Ticket | processor-doc | Always |

**Mastercard (MC)**

| Rule | Severity | Check | Fix | Confidence | Effective |
| --- | --- | --- | --- | --- | --- |
| MC-ELIG-001 | Information | Business card: Level 2 only | None; Data Rate III needs corporate or purchasing | processor-doc | Always |
| MC-ELIG-002 | Information | Card product code MBS or MEF | None; not Level 3 eligible | processor-doc | Always |
| MC-ELIG-003 | Information | MCC on the Level 2 or Level 3 exclusion list | None | processor-doc | Always |
| MC-L2-001 | Error | Tax ratio outside 0.1% to 30%, MCC not exempt, not marked exempt | Correct the tax amount or mark the sale exempt | processor-doc | Always |
| MC-L2-002 | Error | Customer code and PO both missing or blank | Send the buyer's reference | processor-doc | Always |
| MC-L2-003 | Error | Tax indicator missing | Send `taxable` or `exempt` | processor-doc | Always |
| MC-L2-004 | Warning | Merchant tax ID missing | Send your tax ID | processor-doc | Always |
| MC-L3-001 | Error | No line items | Send the invoice lines | processor-doc | Always |
| MC-L3-002 | Error | Description missing | Add the item name | processor-doc | Always |
| MC-L3-003 | Error | Product code missing | Add the SKU | processor-doc | Always |
| MC-L3-004 | Error | Quantity missing or not above 0 | Send the quantity | processor-doc | Always |
| MC-L3-005 | Error | Unit of measure not on the Mastercard list | Map to a listed code such as EAC or PCE | processor-doc | From 2025-10-01 |
| MC-L3-006 | Error | Unit price or extended amount missing | Send both | processor-doc | Always |
| MC-L3-007 | Warning | Debit or credit indicator missing | Send `debit` or `credit` | processor-doc | Always |
| MC-L3-008 | Warning | Line tax indicator, discount indicator or tax type missing | Send the indicators | processor-doc | Always |
| MC-L3-009 | Warning | Ship-to postal code missing | Send it when goods ship | processor-doc | Always |
| MC-FMT-001 | Warning | Amount longer than 9 digits | Check the value; it may be truncated | processor-doc | Always |

**American Express (AMEX)**

| Rule | Severity | Check | Fix | Confidence | Effective |
| --- | --- | --- | --- | --- | --- |
| AMEX-ELIG-001 | Information | Any Amex transaction | None; no public Level 3 rate, Level 2 pricing needs an Amex agreement | processor-doc | Always |
| AMEX-FMT-001 | Error | More than 4 advice lines, or a line over 40 characters | Condense the text | processor-doc | Always |
| AMEX-FMT-002 | Warning | More than 4 line items under a profile that forwards only 4 | Put the most important lines first | processor-doc | Always |
| AMEX-L2-001 | Error | Ship-to postal code missing | Send it | processor-doc | Always |
| AMEX-L2-002 | Warning | PO number over 22 characters | Shorten it | processor-doc | Always |
| AMEX-L2-003 | Warning | Customer reference over 20 characters | Shorten it; some files keep only 17 | processor-doc | Always |
| AMEX-L2-004 | Error | Tax exempt without `tax.exemptionId` | Send the tax registration ID | processor-doc | Always |
| AMEX-L3-001 | Error | Unit of measure not a 2-character ANSI X12 code | Map to a code such as EA | processor-doc | Always |
| AMEX-L3-002 | Error | Level 3 prerequisites missing (requestor name, customer reference, tax amount, ship-to postal code) | Send the missing fields | processor-doc | Always |

**Discover (DISC)**

| Rule | Severity | Check | Fix | Confidence | Effective |
| --- | --- | --- | --- | --- | --- |
| DISC-ELIG-001 | Information | Any Discover transaction | None; no Level 2 or 3 rate exists | processor-doc | Always |
| DISC-FMT-001 | Error | More than 98 line items | Consolidate lines | processor-doc | Always |
| DISC-L2-001 | Warning | Taxable flag missing | Send `tax.indicator` | processor-doc | Always |

Rules whose source is a single processor run only when that processor's profile is selected or when the user opts in with `--strict-profiles`; otherwise they run as warnings. This keeps one processor's quirks from becoming false positives for everyone else.

## Provenance, confidence and facts to verify

Every rule carries a confidence label and at least one source, and both travel into every output so a user can judge a finding without trusting l3lint blindly.

| Label | Meaning | Used for |
| --- | --- | --- |
| `public-brand` | Stated in a document the card brand itself publishes (rate schedules, public rules) | None yet in v1.0; future rules sourced from brand publications |
| `processor-doc` | Stated in public documentation from a gateway, processor or acquirer | Most field, format, limit and eligibility rules |
| `inferred` | l3lint's own heuristic, consistent with public statements but not stated as a rule anywhere | Data quality detectors and thresholds |
| `policy` | l3lint's own safety or input-hygiene rule, not a brand requirement | PCI masking, schema, currency hygiene |

**Source policy for rule authors**

1. Link the most specific public page; prefer a brand page, then a processor or gateway page, then a published guide.
2. When sources disagree, record all of them, encode the stricter reading, and note the disagreement in the rule's `notes`.
3. Never cite or paraphrase non-public material, including acquirer-only specifications and any employer document.
4. Re-check every source each April and October, when brands publish changes, and bump the pack version.

**Facts to verify before v1.0**

- [ ] Visa CEDP line and total rounding tolerance (default is 1 minor unit until a public source says otherwise).
- [ ] Exact date of the Visa Level 2 sunset: sources give April 17 and April 18, 2026; the pack uses April 17 as the last day the Level 2 rules apply.
- [ ] Whether Visa still expects the line-item detail indicator from merchants or always sets it at the processor.
- [ ] Mastercard's post-October 2025 unit-of-measure list against a second public processor source.
- [ ] Mastercard line-item description maximum length (not found in public sources; profile-specific for now).
- [ ] Amex requestor name maximum length under structured acquirer profiles.
- [ ] Whether Discover has announced any enhanced-data program since this spec was written.
- [ ] The Large Ticket break-even amount for `VISA-LT-001`; one source estimates about $7,756 ([PayPal](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program)), but it moves with each rate schedule, so the rule should compute it from the user's rate table instead of hard-coding it.
