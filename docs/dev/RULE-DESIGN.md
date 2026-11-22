# Rule design reference

This file turns each catalog row into an implementable rule. Severity, check text, fix, confidence and effective dates come from the catalog unchanged (see SPEC-DIGEST.md section 12). The columns below record the decisions the catalog leaves open: level, impact, check kind, fields, applicability, processor scoping and supersession. Rule packs are the source of truth once written; update this file when a pack changes.

## Conventions

- Field pointers are canonical RFC 6901 pointers with one `*` wildcard (ADR-0012).
- `appliesTo.cardProducts`: the rule is skipped when the card product is known and not in the list. Unknown or absent product does not skip (ADR-0032).
- `mccNotIn`, `mccIn`: list names from the pack's `lists` section. Unknown MCC does not skip `mccNotIn` rules and does skip `mccIn` rules.
- `when` and `unless`: condition lists (all must hold) over canonical fields.
- `profiles`: the rule's catalog severity applies only under these processor profiles or with `--strict-profiles`; otherwise it is capped at warning (ADR-0016).
- `supersedes`: when this rule fires at a canonical path, findings of the listed rules at the same path are dropped for that transaction (ADR-0032).
- Visa rate gate (VR3): `cardProducts [business, corporate, purchasing, fleet]`, `mccNotIn visaProduct3ExcludedMcc`, `unless [isFederalGovernment = true, mcc in visaFederalExcludedMcc]`.
- Visa Level 2 gate (VR2): `cardProducts [business, corporate, purchasing, fleet]`.
- Mastercard Level 2 gate (MR2): `cardProducts [business, corporate, purchasing]`, `mccNotIn mastercardLevel2ExcludedMcc`.
- Mastercard Level 3 gate (MR3): `cardProducts [corporate, purchasing]`, `mccNotIn [mastercardLevel2ExcludedMcc, mastercardLevel3ExcludedMcc]`, `unless [productCode in (MBS, MEF)]`.

## Source keys

| Key | URL |
| --- | --- |
| payroc | https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program |
| versapay | https://www.versapay.com/cedp |
| weaudit | https://www.weaudit.com/level-3-credit-card-processing/ |
| mcc-consulting | https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/ |
| curbstone | https://curbstone.com/new-data-requirements-cedp/ |
| finix | https://finix.com/resources/blogs/visa-level-2-3-programs-changing-cedp |
| verisave | https://verisave.com/blog/visa-level-2-sunset-cedp-data-quality |
| forbes | https://www.forbes.com/councils/forbesbusinesscouncil/2025/06/27/what-merchants-should-know-about-visas-commercial-enhanced-data-program/ |
| epicor | https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/ |
| cybs-community | https://community.developer.cybersource.com/t5/Integration-and-Testing/Submitting-Level-2-Data/m-p/60293 |
| stripe | https://docs.stripe.com/payments/payment-line-items |
| braintree | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ |
| jpm | https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards |
| mcgw-amex | https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/ |
| cybs-uom | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html |
| cybs-l23 | https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview.md |
| paypal-cedp | https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program |
| payrix-mc | https://resource.payrix.com/docs/interchange-data-requirements |
| payrix-visa | https://resource.payrix.com/resources/understanding-interchange-data-requirements |
| paymentcloud | https://pcdocs.paymentcloudinc.com/developer-docs/processing-payments/level2 |
| navan | https://navan.com/resources/glossary/level-3-data |
| adobe | https://experienceleague.adobe.com/en/docs/commerce-merchant-services/payment-services/reporting/levels-card-payment-transactions |
| paypal-help | https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984 |
| adyen-esd | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data |
| iso4217 | https://www.iso.org/iso-4217-currency-codes.html |
| json-schema | https://json-schema.org/draft/2020-12/json-schema-core |
| pci-ssc | https://www.pcisecuritystandards.org/standards/pci-dss/ |
| iso7812 | https://www.iso.org/standard/70484.html |

## GEN (pack `l3lint.rules.general`, brands: all)

| Rule | Level | Impact | Check | Applicability and notes | Sources |
| --- | --- | --- | --- | --- | --- |
| GEN-PCI-001 | general | hygiene | custom `CardNumber` (reports scrub stage matches) | masking always runs even if the rule is disabled; code fields give warnings | pci-ssc, iso7812 |
| GEN-FMT-001 | general | hygiene | custom `Schema` (reports schema and mapping findings) | | json-schema |
| GEN-FMT-002 | general | hygiene | custom `Currency` | missing, or not an active ISO 4217 alpha-3 code | iso4217 |
| GEN-FMT-003 | general | hygiene | custom `AmountPrecision` | amount must be above 0; money fields must not exceed the exponent; unit cost exempt | iso4217, braintree |
| GEN-FMT-004 | general | truncation | `pattern` fields `/lineItems/*/description`, `/amex/adviceText/*`, regex `^[A-Za-z0-9 '.-]*$` | | braintree |
| GEN-FMT-005 | L3 | blocksLevel3 | `maxCount` `/lineItems`, max 998, profileKey `maxLineItems` | | jpm, stripe |
| GEN-FMT-006 | general | truncation | `maxLength` limit list from the field dictionary, each with a profile key | see pack for the list | stripe, jpm |
| GEN-FMT-007 | L3 | blocksLevel3 | custom `Sequence` | | cybs-community |
| GEN-ARITH-001 | L3 | blocksLevel3 | custom `LineTotal` (formula 1) | skips derived totals | epicor, curbstone, stripe |
| GEN-ARITH-002 | L3 | blocksLevel3 | custom `TransactionTotal` (formula 2) | needs line items with totals; names line failures as likely cause | braintree, jpm, curbstone |
| GEN-ARITH-003 | L3 | riskVisaValidation | custom `TaxConsistency` (formula 3) | runs when any line carries tax and header tax is present | braintree, curbstone |
| GEN-ARITH-004 | L3 | blocksLevel3 | custom `NegativeDebitLine` | | epicor, jpm |
| GEN-ARITH-005 | L3 | riskVisaValidation | custom `DoubleDiscount` | both above 0 and equal within total tolerance | curbstone, epicor |
| GEN-ELIG-001 | general | noBenefit | `inList` `/card/product` values [consumer], mode deny | supersedes VISA-ELIG-001 | stripe |
| GEN-ELIG-002 | general | hygiene | `required` `/card/product` (`unknown` counts as missing) | | stripe |
| GEN-DQ-001 | L3 | riskVisaValidation | custom `PlaceholderDescription` | | versapay, mcc-consulting, finix |
| GEN-DQ-002 | L3 | riskVisaValidation | custom `MerchantNameDescription` | | mcc-consulting, finix |
| GEN-DQ-003 | L3 | riskVisaValidation | custom `PlaceholderCode` | product code and unit | payroc |
| GEN-DQ-004 | L2 | riskVisaValidation | custom `FillerReference` | customer reference and PO | payroc |
| GEN-DQ-005 | L3 | riskVisaValidation | custom `OneLineInvoice` | | payroc, versapay |
| GEN-DQ-006 | L3 | riskVisaValidation | custom `CommodityCodeShape` | UNSPSC shape when scheme is unspsc, unknown or absent; zeros only otherwise | payroc, stripe |
| GEN-DQ-007 | L3 | riskVisaValidation | custom batch `StaticBatchData` | thresholds from config (20, 0.80) | payroc, curbstone |
| GEN-DQ-008 | L3 | riskVisaValidation | custom `DuplicateLines` | | curbstone |

## VISA (pack `l3lint.rules.visa`, brand visa)

| Rule | Level | Impact | Check | Applicability and notes | Sources |
| --- | --- | --- | --- | --- | --- |
| VISA-ELIG-001 | general | noBenefit | `inList` `/card/product` values [business, corporate, purchasing, fleet] | `unknown` skipped; superseded by GEN-ELIG-001 and VISA-ELIG-003 | stripe, paypal-cedp |
| VISA-ELIG-002 | general | noBenefit | `inList` `/merchant/mcc` list visaProduct3ExcludedMcc, mode deny | | stripe |
| VISA-ELIG-003 | general | noBenefit | `inList` `/card/product` values [commercialPrepaid], mode deny | supersedes VISA-ELIG-001 | payrix-visa |
| VISA-ELIG-004 | general | noBenefit | `inList` `/merchant/mcc` list visaFederalExcludedMcc, mode deny | when isFederalGovernment = true | stripe |
| VISA-ELIG-005 | L2 | blocksLevel3 | `required` `/billing/postalCode` | when channel = cardNotPresent | adyen-esd |
| VISA-L2-001 | L2 | blocksLevel2 | `ratio` `/tax/amount` over `/amount`, 0.001 to 0.22 | VR2; mccNotIn visaFuelMcc; unless indicator = exempt | braintree, stripe |
| VISA-L2-002 | L2 | blocksLevel2 | `required` `/tax/indicator` (`notProvided` counts as missing) | VR2 | payroc, jpm |
| VISA-L2-003 | L2 | blocksLevel2 | `required` `/customer/reference` | cardProducts [purchasing]; mccIn visaFuelMcc | payroc, braintree |
| VISA-L3-001 | L3 | blocksLevel3 | `required` `/lineItems` (empty counts as missing) | VR3 | payroc |
| VISA-L3-002 | L3 | blocksLevel3 | `required` `/lineItems/*/description` | VR3 | payroc |
| VISA-L3-003 | L3 | blocksLevel3 | `required` `/lineItems/*/productCode` | VR3 | payroc |
| VISA-L3-004 | L3 | blocksLevel3 | `required` `/lineItems/*/commodityCode` | VR3 | payroc |
| VISA-L3-005 | L3 | blocksLevel3 | `required` `/lineItems/*/quantity`, positiveOnly | VR3 | payroc |
| VISA-L3-006 | L3 | blocksLevel3 | `required` `/lineItems/*/unitOfMeasure` | VR3 | payroc |
| VISA-L3-007 | L3 | blocksLevel3 | `required` `/lineItems/*/unitCost` | VR3 | payroc |
| VISA-L3-008 | L3 | blocksLevel3 | `required` `/lineItems/*/total` | VR3 | payroc |
| VISA-L3-009 | L3 | blocksLevel3 | `required` `/lineItems/*/discountAmount`, allowZero | VR3 | payroc |
| VISA-L3-010 | L3 | blocksLevel3 | `required` `/lineItems/*/detailIndicator`, report once | VR3 (ADR-0039) | payroc, cybs-community |
| VISA-L3-011 | L3 | blocksLevel3 | `required` fields `/amounts/discount`, `/amounts/freight`, `/amounts/duty`, allowZero | VR3 | payroc |
| VISA-L3-012 | L3 | riskVisaValidation | custom `ExplicitTax` | VR3 | versapay, forbes |
| VISA-L3-013 | L3 | blocksLevel3 | `required` fields ship-from, ship-to, destination country | VR3; when freight above 0 | payroc |
| VISA-L3-014 | L3 | truncation | `maxLength` `/lineItems/*/description` max 26 | supersedes GEN-FMT-006 | stripe |
| VISA-L3-015 | L3 | blocksLevel3 | `pattern` `/lineItems/*/commodityCode` `^[A-Za-z0-9]{1,12}$` | supersedes GEN-FMT-006 | stripe |
| VISA-DQ-001 | L3 | riskVisaValidation | custom `VisaDescriptionQuality` | VR3; supersedes GEN-DQ-001, GEN-DQ-002 | mcc-consulting, verisave, finix |
| VISA-DQ-002 | L3 | riskVisaValidation | custom `VisaCodeQuality` | VR3; supersedes GEN-DQ-003 | payroc, mcc-consulting |
| VISA-LT-001 | general | noBenefit | custom `LargeTicket` | only with a rate table that has Large Ticket categories (ADR-0018) | paypal-cedp, payroc |

## MC (pack `l3lint.rules.mastercard`, brand mastercard)

| Rule | Level | Impact | Check | Applicability and notes | Sources |
| --- | --- | --- | --- | --- | --- |
| MC-ELIG-001 | general | noBenefit | `inList` `/card/product` values [business], mode deny | | stripe |
| MC-ELIG-002 | general | noBenefit | `inList` `/card/productCode` values [MBS, MEF], mode deny, case-insensitive | | payrix-mc |
| MC-ELIG-003 | general | noBenefit | `inList` `/merchant/mcc` lists [mastercardLevel2ExcludedMcc, mastercardLevel3ExcludedMcc], mode deny | | stripe |
| MC-L2-001 | L2 | blocksLevel2 | `ratio` `/tax/amount` over `/amount`, 0.001 to 0.30 | MR2; mccNotIn mastercardTaxExemptMcc; unless indicator = exempt | braintree, stripe |
| MC-L2-002 | L2 | blocksLevel2 | `anyOf` `/customer/reference`, `/order/purchaseOrderNumber` | MR2 | paymentcloud |
| MC-L2-003 | L2 | blocksLevel2 | `required` `/tax/indicator` | MR2 | jpm |
| MC-L2-004 | L2 | blocksLevel2 | `required` `/merchant/taxId` | MR2 | epicor |
| MC-L3-001 | L3 | blocksLevel3 | `required` `/lineItems` | MR3 | epicor |
| MC-L3-002 | L3 | blocksLevel3 | `required` `/lineItems/*/description` | MR3 | epicor |
| MC-L3-003 | L3 | blocksLevel3 | `required` `/lineItems/*/productCode` | MR3 | epicor |
| MC-L3-004 | L3 | blocksLevel3 | `required` `/lineItems/*/quantity`, positiveOnly | MR3 | epicor |
| MC-L3-005 | L3 | blocksLevel3 | `inList` `/lineItems/*/unitOfMeasure` list mastercardUom, missing fails | MR3; supersedes GEN-FMT-006 | jpm, weaudit, navan |
| MC-L3-006 | L3 | blocksLevel3 | `required` fields `/lineItems/*/unitCost`, `/lineItems/*/total`, allowZero | MR3 | epicor |
| MC-L3-007 | L3 | blocksLevel3 | `required` `/lineItems/*/debitCreditIndicator` | MR3 | epicor |
| MC-L3-008 | L3 | blocksLevel3 | custom `MastercardLineIndicators` | MR3; profiles [jpmorgan] | jpm |
| MC-L3-009 | L3 | blocksLevel3 | `required` `/shipping/toPostalCode` | MR3; profiles [jpmorgan] | jpm |
| MC-FMT-001 | general | truncation | custom `AmountDigits` (9 digits in minor units) | profiles [jpmorgan] | jpm |

## AMEX (pack `l3lint.rules.amex`, brand amex)

| Rule | Level | Impact | Check | Applicability and notes | Sources |
| --- | --- | --- | --- | --- | --- |
| AMEX-ELIG-001 | general | noBenefit | custom `NoRateProgram` | always, once | stripe, jpm, paypal-help, cybs-l23 |
| AMEX-FMT-001 | L3 | truncation | custom `AdviceText` (4 lines of 40) | profiles [jpmorgan, amex-advice-text] | jpm |
| AMEX-FMT-002 | L3 | truncation | `maxCount` `/lineItems`, profileKey `amexForwardedLineItems`, no default | runs only under profiles that set the key | stripe |
| AMEX-L2-001 | L2 | blocksLevel2 | `required` `/shipping/toPostalCode` | when merchant country in US, CA and US territories; profiles [mastercard-gateway-amex]; supersedes AMEX-L3-002 | mcgw-amex |
| AMEX-L2-002 | L2 | truncation | `maxLength` `/order/purchaseOrderNumber` max 22 | supersedes GEN-FMT-006 | mcgw-amex |
| AMEX-L2-003 | L2 | truncation | `maxLength` `/customer/reference` max 20 | supersedes GEN-FMT-006 | mcgw-amex |
| AMEX-L2-004 | L2 | blocksLevel2 | `required` `/tax/exemptionId` | when indicator = exempt; profiles [mastercard-gateway-amex] | mcgw-amex |
| AMEX-L3-001 | L3 | blocksLevel3 | `inList` `/lineItems/*/unitOfMeasure` list amexUom | supersedes GEN-FMT-006 | mcgw-amex, cybs-uom |
| AMEX-L3-002 | L3 | blocksLevel3 | `required` fields requestor name, customer reference, tax amount (allowZero), ship-to | when line items present; profiles [mastercard-gateway-amex] | mcgw-amex |

## DISC (pack `l3lint.rules.discover`, brand discover)

| Rule | Level | Impact | Check | Applicability and notes | Sources |
| --- | --- | --- | --- | --- | --- |
| DISC-ELIG-001 | general | noBenefit | custom `NoRateProgram` | always, once | adobe, paypal-help |
| DISC-FMT-001 | L3 | blocksLevel3 | `maxCount` `/lineItems` max 98 | profiles [jpmorgan] | jpm |
| DISC-L2-001 | L2 | blocksLevel2 | `required` `/tax/indicator` | profiles [jpmorgan] | jpm |

## Tier ladders (ADR-0020)

| Brand | Ladder (top first) | Base when all blocked | Not eligible |
| --- | --- | --- | --- |
| Visa | product3, then product2 while as-of is on or before 2026-04-17 | belowProduct3 after the sunset, belowProduct2 before | consumer, commercial prepaid, Product 3 excluded MCC, federal card at excluded medical MCC |
| Mastercard | dataRate3 (corporate, purchasing; MCC not on L2 or L3 list; product code not MBS or MEF), dataRate2 (business, corporate, purchasing; MCC not on L2 list) | dataRate1 | consumer, fleet, commercial prepaid, L2 excluded MCC |
| Amex | none | none | always: no public rate program |
| Discover | none | none | always: no rate program |
