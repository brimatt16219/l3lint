# l3lint research: brand facts and processor profiles

Retrieved: 2026-10-25

Scope: public sources only. Values below are recorded as the cited page states them. Where a page does not clearly state a value, the item is marked unconfirmed. Companion file: `amex-uom.csv` (Amex unit-of-measure codes).

## URLs consulted

Card brand pages:

- https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf : Visa USA Interchange Reimbursement Fees, edition dated 24 October 2026 (rates effective October 24, 2026). Reachable.
- https://web.archive.org/web/20260403105346id_/https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf : archived copy of the same Visa PDF, edition dated 18 October 2025.
- https://web.archive.org/web/20260506053529id_/https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf : archived copy of the same Visa PDF, edition dated 18 April 2026.
- https://usa.visa.com/dam/VCOM/download/about-visa/visa-rules-public.pdf : Visa Core Rules and Visa Product and Service Rules, 18 April 2026. Searched for CEDP, tolerance and line item rules; none found.
- https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/ : Mastercard Gateway, Level 2 and 3 data for the American Express acquirer. JavaScript page; read in a browser (plain fetch returns 404 or an empty shell).
- https://test-gateway.mastercard.com/api/documentation/apiDocumentation/rest-json/version/latest/operation/Transaction%3a%20%20Authorize.html?locale=en_US : Mastercard Gateway API reference (Authorize, version 100), field lengths.
- https://www.discoverglobalnetwork.com/ (and pages returned by a site search) : no commercial enhanced data program page found.

Processor, acquirer and gateway pages:

- https://docs.stripe.com/payments/payment-line-items : Stripe payment line items (limits, fields, MCC lists).
- https://support.stripe.com/questions/understand-the-visa-commercial-enhanced-data-program-(cedp) : Stripe CEDP support article.
- https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards : J.P. Morgan commercial cards (line item limits, Mastercard unit list, Amex advice text).
- https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update : TabaPay CEDP 2026 update.
- https://support.checkout.com/hc/en-us/articles/30007033714194 : Checkout.com, changes to commercial interchange in the US (read in a browser; plain fetch returns 403).
- https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data : Checkout.com Level 2 or Level 3 docs (current path of the brief's URL https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data ). Page says last updated August 28, 2026.
- https://www.paypal.com/us/brc/article/visas-commercial-enhanced-data-program : PayPal article on CEDP (Large Ticket break-even).
- https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 : Adyen Level 2/3 data.
- https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data : Adyen enhanced scheme data (MCC eligibility).
- https://raw.githubusercontent.com/Adyen/adyen-openapi/main/json/CheckoutService-v72.json : Adyen Checkout API v72 OpenAPI spec (field limits, no array maximum).
- https://raw.githubusercontent.com/Adyen/adyen-openapi/main/json/CheckoutService-v71.json : Adyen Checkout API v71 OpenAPI spec (additionalData Level 2/3 keys).
- https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ : Braintree Level 2 and 3 required fields.
- https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ : Braintree Transaction.Sale reference (line item field lengths, 249 line items).
- https://developer.paypal.com/braintree/docs/reference/general/validation-errors/all/dotnet/ : Braintree validation errors (PO length, ship-from postal length, line item count).
- https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/overview : Braintree Level 2 and 3 overview (brands).
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html : Cybersource item unit of measure (lengths by processor and brand, link to an X12 el355 list).
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-name.html : Cybersource product name length.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-code.html : Cybersource product code length.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-commodity-code.html : Cybersource commodity code lengths.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-user-po.html : Cybersource purchase order / customer code lengths.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-purchaser-code.html : Cybersource customer identifier lengths.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-postal-code.html : Cybersource ship-to postal code.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-from-postal-code.html : Cybersource ship-from postal code.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-merchant-vat-reg-num.html : Cybersource merchant tax (VAT) registration number lengths.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-amex-data-taa1.html : Cybersource Amex transaction advice addendum fields.
- https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-taxable.html : Cybersource taxable flag.
- https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/barclays/rest/level-2-3/l23-intro-overview/l23-l3-data/l23-processors-transactions-l3-info.html : Cybersource Level III item count (Barclays).
- https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview/l23-l3-data.html : Cybersource Level III overview (Visa Platform Connect guide; no item maximum stated).
- https://developer.authorize.net/api/reference/index.html : Authorize.net API reference (createTransactionRequest fields).
- https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd : Authorize.net public XML schema (maxLength values, Amex TAA fields).
- https://www.stedi.com/edi/x12/element/355 : X12 element 355 code list (cross-check for the Amex unit codes).
- https://ws2.segment.co.il/Public/EDI-Segment/x12-4040/EDI_4010/el355.htm : the X12 4010 el355 list linked from Cybersource. Blocked by bot protection; not used.
- https://help.usaepay.info/developer/reference/umcodes/ : USAePay unit of measure codes (not brand specific; context only).
- https://resource.payrix.com/docs/interchange-data-requirements : Payrix interchange data requirements (Visa amount formulas).
- https://developers.bluesnap.com/reference/visa-cedp : BlueSnap CEDP page (0.05% fee, line item detail indicator error codes).
- https://www.evolvepayment.com/blog/visa-cedp-explained/ : Evolve Payment blog (lists line-item detail indicator as a CEDP field).

Tried and not usable: https://worldpay.egain.cloud/kb/merchantserviceonline/content/DEV-39077/Visa-Commercial-Enhanced-Data-Program-CEDP-Error-Guide (empty response), https://support.worldpay.com/support/CNP-API/content/lineitemdat.htm (connection reset), https://merchantcostconsulting.com/lower-credit-card-processing-fees/visa-cedp-error-codes/ (403), https://support.payroc.com/cedp/understanding-the-visa-commercial-enhanced-data-program-cedp (404).

---

## A. Amex acquirer page facts (Mastercard Gateway)

Source: https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/ (format notation on the page: AN means alphanumeric, N means numeric). Field limits marked "API" come from https://test-gateway.mastercard.com/api/documentation/apiDocumentation/rest-json/version/latest/operation/Transaction%3a%20%20Authorize.html?locale=en_US .

| Topic | Value |
|---|---|
| Level 2 condition: merchant country | Level 2 is sent to the Amex acquirer only for US or Canadian merchants with country code ASM, CAN, GUM, MHL, FSM, MNP, PLW, PRI, USA or VIR. |
| Level 2 condition: ship-to postal | Level 2 is sent only if the merchant provides at least the shipping destination ZIP/post code in `shipping.address.postcodeZip`. API: 1 to 10 chars, characters 0-9, a-z, A-Z, space, hyphen. |
| Merchant purchase reference | `order.reference`. If absent the gateway uses `order.id`. Invalid characters are removed, then truncated to 9 chars (LIDCAP) or 30 chars (FINCAP). Valid characters: A-Z, a-z, 0-9, space, and `= : , . / ? ^ ' ( ) [ ] { } \ ! @ # $ % & * _ + - ;` plus backtick (ASCII 96) and tilde (ASCII 126). Value is returned in `transaction.acquirer.transactionId`. API: `order.reference` 1 to 40 chars. |
| Tax status and tax exempt ID | `order.taxStatus` / `transaction.taxStatus`. If set to EXEMPT, `order.taxRegistrationId` must be provided. API: `taxRegistrationId` 1 to 30 chars. |
| Tax type codes | `order.tax[n].type`: ST State Sales Tax; CA City Tax; LT Local Sales Tax; CP County/Parish Sales Tax; VA Value Added Tax; GS Goods and Services Tax; SP State/Provincial Tax; CT County Tax; EV Environmental Tax; LS State and Local Sales Tax; PG State or Provincial Tax on Goods. |
| Shipping amount | `transaction.acquirer.customData` key `NSU_3_D240`. If missing on CAPTURE or REFUND, the value from the initial transaction is used. |
| Handling amount | `transaction.acquirer.customData` key `NSU_3_D500`. |
| Purchase order number | `transaction.acquirer.customData` key `NER_5`. Format AN, 22. Confirmed: max 22. |
| Charge description lines | `transaction.acquirer.customData` keys `TAA_4`, `TAA_5`, `TAA_6`, `TAA_7` (4 lines). Shown on billing statements. Per-line length is not stated on the page. Defaults if not provided: PO number, requestor name, description, unit price and quantity of the first two line items, shipping amount, handling amount, total number of line items. API: `customData` 1 to 2048 chars overall. |
| Customer / cardholder reference | `order.customerReference`. Invalid characters removed, then truncated to 17 or 20 chars depending on the settlement file. Both values are on the page; the page does not say which file uses which. API: 0 to 25 ASCII chars. |
| Requestor name | `order.requestorName` is a Level 3 prerequisite. The Amex page gives no maximum length. API: 1 to 100 chars (gateway field limit, not an Amex-specific limit). |
| Level 3 prerequisites | Level 3 is sent only if Level 2 is also sent, and these are present: `transaction.reference`; card expiry month and year; `order.customerReference`; `shipping.address.postcodeZip`; `transaction.taxAmount`; `order.requestorName`; and at least one line item with `order.item[n].quantity`, `order.item[n].unitOfMeasure`, `order.item[n].unitPrice`, `order.item[n].description`. |
| Item unit of measure | `order.item[n].unitOfMeasure`, format AN, 2. Values from the page's "Item unit of measure enumeration values" table (720 codes, see `amex-uom.csv`). The page does not use the words ANSI or X12. The codes match X12 element 355; six codes (1, 2, 3, 4, 5, 8) are shown without the X12 leading zero. API: unitOfMeasure 1 to 10 chars. |
| Item description | API: `item[n].description` 1 to 127 chars; `item[n].name` 1 to 127; `item[n].sku` 1 to 127. No Amex-specific length on the Amex page. |
| Extra Level 3 keys (`item[n].detail.acquirerCustom`) | NPD_3 manufacturer part number AN 30; NPD_8 customer part number AN 30; NPD_2 non-integer UNSPSC AN 30; NPD_7 non-integer UPC AN 30; NTP_18 cost center AN 30; NTP_21 asset code AN 30; NSM_3 bill of lading AN 30; NSM_4 item service level code AN 2; NSM_9 destination airport AN 30; NSM_10 origin airport AN 30; NSM_11 standard carrier alpha code AN 4. Industry supplies: NPN_2 third-party vendor name AN 40. Office supplies: NA2_7_JC base dollar amount Decimal 15; NA2_7_PD service credit Decimal 15; NTP_2 lease end date YYYYMMDD; NTP_3 lease number AN 30; NTP_5 lease start date YYYYMMDD; NTP_7 meter read end N 10; NTP_8 meter read start N 10; NTP_9 meter reading date YYYYMMDD; NTP_14 machine serial AN 30; NTP_16 service date YYYYMMDD; NTP_16 total meter count N 10; NTP_26 invoice period end date YYYYMMDD; NP2_14 copies billed N 15; NP2_18 months into lease N 4; NP2_32 cost per copy N 15. Computer: NTP_3 software license number AN 30; NP2_19 hardware tracking number AN 30. Note: the page reuses NTP_16 and NTP_3 for two different items each (looks like a documentation error). |
| UNSPSC / UPC allowed characters | a-z, A-Z, 0-9, space, and `! @ # $ % & * ( ) _ + - [ ] \ { } ; = ^ ' : , . / ?` |
| Invalid acquirerCustom | Gateway returns `result=FAILURE`, `response.gatewayCode=DECLINED`, and `response.acquirerMessage` naming the field. |
| Item service level codes (NSM_4) | 9 Premium Surface; 9A 9 A.M.; AC Air Cargo; AE Air Economy; AM A.M.; BC Business Class; CE Courier Express; D1, D2, D3 Delivery scheduled next, second, third day by cartage agent; FC First Class; IA IATA; ME Metro; MW Multiweight; ND Next Day Air; NF Next Flight Out; NM Next Morning; NS Not Served; ON Overnight; PA Primary Service Area next day by 10:30 A.M.; PB Priority Mail; PI Priority Mail Insured; PM P.M.; PN Primary Service Area next day by noon; PO P.O. Box/Zip Code; PR Primary Service Area next day by 5:00 P.M.; PS Primary Service Area second day by noon; SA Same Day; SC Second Day Air; SD Saturday; SE Second Day; SG Standard Ground; SM Second Morning; ST Standard Class; ZZ Mutually Defined. |

Unit list status: complete (all 720 rows copied and checked against the live table with a SHA-256 hash of the CSV body; hashes matched). Cross-check: the Cybersource unit of measure page says American Express Direct uses a 2-char code and points to an X12 el355 list; Stedi's X12 element 355 page confirms EA, BX, HR, CA, DZ, GA, LB, FT, PK, CS and the leading-zero form (01 Actual Pounds).

---

## B. Facts to verify

### B1. Visa CEDP line and total rounding tolerance

- Status: unconfirmed.
- Value: no public source states a tolerance. Default assumption (1 minor unit) is not supported or contradicted by a public Visa source.
- What public sources say:
  - Stripe validates line item arithmetic by default and returns HTTP 400 on mismatch; with `enforce_arithmetic_validation=false` it does not send line item data with arithmetic errors to card networks (https://docs.stripe.com/payments/payment-line-items).
  - J.P. Morgan: for Visa, the transaction amount must equal the sum of line totals plus shipping, duty and VAT adjustments; otherwise the API returns an error (https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards).
  - Braintree: each processor checks that line item totals match the transaction amount; a mismatch may be declined. Visa formula: amount = sum of line totals + taxAmount + shippingAmount + shippingTaxAmount - discountAmount, with line total = quantity x unitAmount - line discount. For Mastercard it gives line total = quantity x unitAmount (https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/).
  - Checkout.com: amounts must reconcile; Visa and Mastercard may charge extra fees when they do not. Formula: amount = sum(items.total_amount) - discount + tax + shipping (https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data).
  - Adyen and Payrix give the same Visa formulas with no tolerance (https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 , https://resource.payrix.com/docs/interchange-data-requirements).
- Tried: Visa public rules PDF (April 2026) searched for CEDP, tolerance, line item: nothing. Visa interchange PDF: rates only. Worldpay CEDP error guide: not readable.

### B2. Visa Level 2 (Product 2) sunset date

- Status: confirmed from Visa's public interchange schedule (by comparing editions). Sources disagree on wording.
- Value: the Visa USA Interchange Reimbursement Fees edition dated 18 October 2025 lists "Commercial Level II" (2.50% + $0.10) in the Corporate and Purchasing table and "Business Level II" (Tier I to V: 1.90%, 2.05%, 2.10%, 2.20%, 2.25%, each + $0.10) in the Business table. The edition dated 18 April 2026 (rates effective April 18, 2026) removes both rows. Only "Commercial Level II Fuel" (2.20% + $0.10) remains, and it is still present in the 24 October 2026 edition. So Level 2 no longer applies from April 18, 2026; the last day Level 2 rates applied is April 17, 2026.
- URLs: https://web.archive.org/web/20260403105346id_/https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf (Oct 2025 edition), https://web.archive.org/web/20260506053529id_/https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf (Apr 2026 edition), https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf (Oct 2026 edition).
- How the two processor dates fit: Checkout.com says Visa discontinues Level 2 on April 18, 2026 (matches the schedule's effective date). TabaPay says Product 2 (Level 2) is sunset "effective April 17, 2026" (one day earlier; consistent only if read as the last day). Adyen says "Starting on April 1, 2026" Visa supports level 3 data only, which is Adyen's own platform date. Stripe says only "April 2026".
- Naming caution: in Visa's current schedule "Product 2" is not a Level 2 name. "Business Product 2" (same rates as the old Business Level II row) is still listed in the October 2026 edition, and the consumer tables use "Product 1" for card-not-present programs and "Product 2" for card-present programs. TabaPay's label "Product 2 (Level 2)" does not match Visa's own naming. l3lint should not treat "Product 2" as a synonym for Level 2.

### B3. Line-item detail indicator: merchant or processor?

- Status: unconfirmed (no public Visa source).
- Evidence: none of the merchant-facing APIs read here expose a line-item detail indicator field (Stripe, Adyen v72 spec, Braintree, Checkout.com, Authorize.net, J.P. Morgan page). Checkout.com says its platform automatically handles CEDP technical requirements, including flagging eligible transactions (https://support.checkout.com/hc/en-us/articles/30007033714194). PayPal says the CEDP flag is attached when the merchant populates Level 3 fields (https://www.paypal.com/us/brc/article/visas-commercial-enhanced-data-program). Evolve Payment lists the indicator among 12 CEDP line item fields but does not say who sets it (https://www.evolvepayment.com/blog/visa-cedp-explained/). BlueSnap lists clearing error TC50-1005 (indicator outside 0 to 5) without saying who sets it (https://developers.bluesnap.com/reference/visa-cedp). A search snippet shows Worldpay's CNP API has an optional `lineItemDetailIndicator` child of `lineItemData`, but the page could not be loaded.
- Reading: the indicator appears to be a clearing-record (TC50) field that processors build; merchants using these gateways do not send it. Not confirmed by Visa.

### B4. Mastercard unit-of-measure list (post October 2025)

- Status: confirmed for the code set from J.P. Morgan; the unit set is confirmed by a second processor (Checkout.com), which uses its own 4-letter API values instead of Mastercard's 3-letter codes. No second public processor page with the same 3-letter codes was found.
- J.P. Morgan list (36 rows, 32 distinct codes): Acre ACR, Are ARE, Centiliter CLT, Centimeter CMT, Each EAC, Foot FOT, GB Gallon GLI, GB Ounce ONZ, GB Pint PTI, GB Quart QTI, Gram GRM, Hectare HAR, Inch INH, Kilogram KGM, Kilometer KMT, Liter LTR, Meter MTR, Mile NMI, Milliliter MLI, Millimeter MMT, Piece PCE, Pound LBR, Square Centimeter CMK, Square Meter MTK, Square Millimeter MMK, Square Foot FTK, Square Inch INK, Square Kilometer KMK, Square Mile MIK, Square Yard YDK, Ton TNE, US Gallon GLI, US Ounce ONZ, US Pint PTI, US Quart QTI, Yard YRD.
- Compared with the spec list: identical set of 32 codes. No missing or extra codes.
- Differences and notes:
  - J.P. Morgan maps US Gallon, US Ounce, US Pint and US Quart to the same codes as the GB units (GLI, ONZ, PTI, QTI).
  - Checkout.com has the same 36 unit concepts with 4-letter values: ACRE, ARES, CELI, CMET, EACH, FOOT, GBGA, GBOU, GBPI, GBQA, GRAM, HECT, INCH, KILO, KMET, LITR, METR, MILE, MILI, MMET, PIEC, PUND, SCMT, SMET, SMIL, SQFO, SQIN, SQKI, SQMI, SQYA, TONS, USGA, USOU, USPI, USQA, YARD. Its field allows up to 12 chars, and it fills EACH when a value does not meet Mastercard's rules (support article). So Checkout.com keeps GB and US units separate at its API.
  - Compared with UN/ECE Rec 20: NMI is nautical mile in Rec 20 (statute mile is SMI), MLI is not a Rec 20 code (millilitre is MLT), and EAC is not the usual Rec 20 code for each. These are noted as oddities only; the J.P. Morgan page is the public source of the Mastercard list.
  - J.P. Morgan's own example uses "EA", which is not in its list.
  - Adyen limits `unitOfMeasure` to 3 chars (consistent with 3-letter codes) but publishes no list.
  - Change date: Checkout.com says Mastercard requires more specific units from October 17, 2025 (https://support.checkout.com/hc/en-us/articles/30007033714194).
- URLs: https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards , https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data .

### B5. Mastercard line-item description maximum length

- Status: unconfirmed at brand level (no public Mastercard page found).
- Processor values: Checkout.com `items[].name` up to 26 chars for Visa and Mastercard; Adyen `description` max 26; Stripe truncates `product_name` to 26 for cards; Braintree `Name` max 35; Mastercard Gateway `item[n].description` 1 to 127 (gateway field, acquirer may truncate); J.P. Morgan gives no length. 26 is the common processor value.

### B6. Amex requestor name maximum length (structured acquirer profile)

- Status: unconfirmed.
- Value: the Mastercard Gateway Amex page lists `order.requestorName` as a Level 3 prerequisite but gives no Amex length. The gateway API field allows 1 to 100 chars. No public Amex specification was found.

### B7. Discover enhanced data program

- Status: unconfirmed (no announcement found).
- Value: no public Discover page announcing a Level 2/3 interchange program. Discover site search returned only PCI merchant levels and Discover Enhanced Decisioning (a fraud data tool). Processors do accept Discover data: J.P. Morgan allows 98 line items for Discover and Diners Club and requires `isTaxable` for Discover. Stripe and Adyen do not support Level 2/3 for Discover.

### B8. Visa Large Ticket rate and break-even

- Status: confirmed (rates from Visa's public schedule; break-even computed).
- Visa schedule, edition 24 October 2026, Corporate and Purchasing table (Purchasing and Corporate T&E, includes Fleet):
  - Commercial Product 3: 1.75% + $0.10
  - Commercial Level II Fuel: 2.20% + $0.10
  - Commercial Card Present: 2.50% + $0.10
  - Commercial Travel Service: 2.65% + $0.10
  - Commercial Card Not Present: 2.70% + $0.10
  - Non-Qualified and Non-Qualified with Data: 2.95% + $0.10
  - Commercial Product Large Ticket: 1.30% + $35.00
  - GSA Large Ticket (Purchasing only): 1.20% + $39.00
  - Visa Purchasing Prepaid Large Ticket: 1.45% + $35.00
  - Straight Through Processing: Tier 1 under $7,000 2.00% + $0.10; Tier 2 $7,000 to $14,999.99 1.30% + $35.00; Tier 3 $15,000 to $49,999.99 1.10% + $35.00; Tier 4 $50,000 to $99,999.99 0.95% + $35.00; Tier 5 $100,000 and up 0.80% + $35.00.
  - Large Purchase Advantage (card not present): up to $10,000 Purchasing rates; $10,000.01 to $25,000 0.70% + $49.50; $25,000.01 to $100,000 0.60% + $52.50; $100,000.01 to $500,000 0.50% + $55.50; over $500,000 0.40% + $58.50.
  - Business Product 3, Tier I to V: 2.40%, 2.55%, 2.60%, 2.70%, 2.75%, each + $0.10.
- Break-even Large Ticket vs Commercial Product 3: (35.00 - 0.10) / (1.75% - 1.30%) = $7,755.56. This matches PayPal's "approximately $7,756". Large Ticket vs Commercial Card Not Present: (35.00 - 0.10) / (2.70% - 1.30%) = $2,492.86.
- Qualification thresholds for Large Ticket are not in the schedule. Checkout.com says a transaction may qualify for Large Ticket if the amount is at least 8,000 USD and the card is Corporate, Purchasing or Fleet. PayPal says the maximum is $10MM. The October 2025 edition already showed Commercial Product Large Ticket at 1.30% + $35.00; PayPal says it was 1.45% + $35 before CEDP.
- CEDP program fee: 5 bps (0.05%) per PayPal, TabaPay and BlueSnap. Not shown in the Visa interchange schedule (it is a network fee, not interchange).

### B9. Visa line-item tolerance (spec open question)

- Status: unconfirmed. Same finding as B1: no public tolerance. Processors that validate (Stripe, J.P. Morgan) reject any mismatch, and Stripe works in integer minor units. A 0 minor-unit tolerance matches the strictest public processor behavior; 1 minor unit remains an l3lint assumption.

### B10. Visa public interchange schedule reachability

- Status: confirmed reachable.
- The PDF at https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf is the 24 October 2026 edition. It shows Commercial Product 3, Commercial Level II Fuel, Large Ticket, STP, Large Purchase Advantage and Business Product 1/2/3 rates (values in B8). It does not describe CEDP data rules, verification, tolerances or the CEDP fee, and says to refer to the U.S. Interchange Reimbursement Fee Rate Qualification Guide for details (that guide was not found publicly).

---

## C. Processor profiles

Notation: "n/s" means not stated on the cited page.

### stripe

| Key | Value | URL |
|---|---|---|
| Max line items | 200 | https://docs.stripe.com/payments/payment-line-items |
| Amex line items forwarded | first 4 only (Amex specification) | https://docs.stripe.com/payments/payment-line-items |
| Brands for L2/L3 | Visa, Mastercard, Amex (Amex savings need a direct Amex agreement). No Discover. | https://docs.stripe.com/payments/payment-line-items |
| Line description (`product_name`) | max 1024; cards truncate to 26 alphanumeric | https://docs.stripe.com/payments/payment-line-items |
| Product code | max 12 | https://docs.stripe.com/payments/payment-line-items |
| Commodity code | max 12, alphanumeric, no spaces | https://docs.stripe.com/payments/payment-line-items |
| Unit of measure | max 12, alphanumeric, free text (examples "feet", "gallons") | https://docs.stripe.com/payments/payment-line-items |
| Customer reference | truncated to 25 alphanumeric (spaces excluded) before sending to the network | https://docs.stripe.com/payments/payment-line-items |
| Order reference (`order_reference`) | truncated to 25 alphanumeric (spaces excluded) for card networks | https://docs.stripe.com/payments/payment-line-items |
| Purchase order number | n/s (no separate field) | https://docs.stripe.com/payments/payment-line-items |
| Merchant tax ID | n/s | https://docs.stripe.com/payments/payment-line-items |
| Ship-to / ship-from postal | max 10, alphanumeric and hyphen | https://docs.stripe.com/payments/payment-line-items |
| Quantity / unit cost | quantity > 0 integer; unit_cost >= 0 integer minor units | https://docs.stripe.com/payments/payment-line-items |
| Arithmetic | validated by default (400 on mismatch); can be relaxed per request, then L3 data is not sent | https://docs.stripe.com/payments/payment-line-items |
| Tax rules (L2) | Mastercard 0.1% to 30%, Visa 0.1% to 22%, with MCC exceptions (see D) | https://docs.stripe.com/payments/payment-line-items |
| Not combinable | line items cannot be sent with industry data (car rental, lodging, airline) | https://docs.stripe.com/payments/payment-line-items |

### adyen

| Key | Value | URL |
|---|---|---|
| Max line items | n/s (no maxItems on `itemDetailLines` in the v72 spec) | https://raw.githubusercontent.com/Adyen/adyen-openapi/main/json/CheckoutService-v72.json |
| Brands | Visa and Mastercard for interchange; Amex data for fraud and reporting only | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Visa L2 cutoff on Adyen | from April 1, 2026 Visa supports level 3 only | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Description | ASCII, max 26; not single char, blank, all special chars, or all zeros; for Visa must not resemble merchant name | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Product code | ASCII, max 12 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Commodity code | ASCII, max 12 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Unit of measure | ASCII, max 3 (no list published) | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Customer reference | ASCII, max 25 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Purchase order / invoice ref | n/s (customer reference is the order reference field) | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Merchant tax ID | n/s | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Ship-to / ship-from postal | max 10; US 5 or 9 digits; Canada 6 chars | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Destination state | max 3 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Quantity | integer > 0, max 9999 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Amounts | minor units, max 10000000000 (v72); additionalData keys max length 12 (v71) | https://raw.githubusercontent.com/Adyen/adyen-openapi/main/json/CheckoutService-v71.json |
| Tax | L2 total tax min 0.1%; max 30% Mastercard, 22% Visa; L2 tax must not be zero, L3 may be zero | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3 |
| Not eligible MCCs (Adyen policy) | 4112, 5812, 4468, 5541, 5542, 5983, 5499 | https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data |

### braintree

| Key | Value | URL |
|---|---|---|
| Max line items | 249 (error 915157 "Too many line items") | https://developer.paypal.com/braintree/docs/reference/general/validation-errors/all/dotnet/ |
| Brands | Visa and Mastercard (overview names only these) | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/overview |
| Allowed characters (Name, UnitOfMeasure, ProductCode, CommodityCode) | a-z, A-Z, 0-9, apostrophe, period, hyphen, space. Other characters may still qualify but print as a space. Confirmed. | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ |
| Line name | max 35 (127 for PayPal) | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Line description | max 127 | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Product code | max 12 (127 for PayPal) | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Commodity code | max 12 | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Unit of measure | max 12, free text or code | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| UPC code | max 17, with UPC type | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Purchase order number | max 17 (12 for AIB), printable ASCII | https://developer.paypal.com/braintree/docs/reference/general/validation-errors/all/dotnet/ |
| Customer reference / invoice ref | n/s (PurchaseOrderNumber is the L2 reference) | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ |
| Merchant tax ID | n/s (eligibility needs a Tax ID or VAT ID on the account) | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/overview |
| Ships-from postal | max 9 letters and numbers; letters, numbers, spaces, hyphens; CEDP US format 5, 9, or 5-4 digits; Canada ANA NAN | https://developer.paypal.com/braintree/docs/reference/general/validation-errors/all/dotnet/ |
| Shipping postal (address) | 4 to 9 alphanumerics, optional dash or space | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Tax range (L2) | Visa 0.1% to 22%, Mastercard 0.1% to 30% (US). Confirmed. | https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/ |
| Decimals | quantity up to 4 places; unit amount up to 4; totals, tax, discount up to 2 | https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/dotnet/ |
| Discount / shipping amount | max 9 digits | https://developer.paypal.com/braintree/docs/reference/general/validation-errors/all/dotnet/ |

### cybersource

Processor-specific. Some processors on the Cybersource pages are not named here; they are covered by "some processors".

| Key | Value | URL |
|---|---|---|
| Max line items | Barclays: 99 Mastercard, 99 Visa. Not stated for other processors in the current guides. | https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/barclays/rest/level-2-3/l23-intro-overview/l23-l3-data/l23-processors-transactions-l3-info.html |
| Unit of measure | American Express Direct 2 (X12 el355 codes); Barclays 80; Chase Paymentech 3 (Mastercard), 12 (Visa); Elavon Americas 3 (Mastercard), 12 (Visa); some processors 3 (Mastercard), 12 (Visa); GPN, OmniPay Direct, TSYS, Visa Platform Connect, Worldpay VAP and others 12. Must not be all zeros or spaces. | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html |
| Product name | Barclays 35; other processors 255 | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-name.html |
| Product code | 255 | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-product-code.html |
| Commodity code | Barclays 35; Chase Paymentech 12; Elavon 12; GPN 15 (Mastercard), 12 (Visa); OmniPay Direct 4 (Mastercard), 12 (Visa); TSYS 12; Visa Platform Connect 12; some processors 12. Alphanumeric only. | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-commodity-code.html |
| Purchase order / customer code (`invoiceHeader_userPO`) | Amex Direct 17; Barclays 25; Chase Paymentech 25; Elavon 17; GPN 17 (Mastercard), 25 (Visa); OmniPay Direct 17 (Mastercard), 25 (Visa); Visa Platform Connect 17; TSYS 25; Worldpay VAP 17; some processors 25 | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-user-po.html |
| Customer identifier (`invoiceHeader_purchaserCode`) | default 15; Barclays 17; others vary (20 or 100) | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-purchaser-code.html |
| Merchant tax / VAT registration | general 20; Barclays 13; Elavon 20; GPN 20; OmniPay Direct 21; TSYS 20; Visa Platform Connect 20 (13 for payouts); Worldpay VAP 20 | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-merchant-vat-reg-num.html |
| Ship-to postal | 10; Amex Direct strips non-alphanumerics and truncates to 9; required for Amex Level II in US | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-to-postal-code.html |
| Ship-from postal | 10; 5 to 9 digits (US), A1B 2C3 (Canada) | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/ship-from-postal-code.html |
| Amex TAA lines | 4 fields (TAA1 to TAA4), 40 chars each (Worldpay VAP 26); needs account enablement | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-amex-data-taa1.html |
| Taxable flag | Visa Platform Connect: must be true if any item tax > 0; Worldpay VAP: required when item tax = 0 | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-taxable.html |

### authorizenet

| Key | Value | URL |
|---|---|---|
| Max line items | 30 per transaction (API reference). The XSD caps profile transactions at 30; the main transaction array is unbounded in the XSD. | https://developer.authorize.net/api/reference/index.html , https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |
| Item ID | max 31 | https://developer.authorize.net/api/reference/index.html |
| Line name | max 31 (required for L3/CEDP) | https://developer.authorize.net/api/reference/index.html |
| Line description | max 255 | https://developer.authorize.net/api/reference/index.html |
| Product code / SKU | 255 each | https://developer.authorize.net/api/reference/index.html |
| Commodity code | 12 | https://developer.authorize.net/api/reference/index.html |
| Unit of measure | 12, free text | https://developer.authorize.net/api/reference/index.html |
| Quantity / unit price / amounts | decimal, up to 4 places | https://developer.authorize.net/api/reference/index.html |
| Purchase order (`poNumber`) | 25, alphanumeric only, no spaces or symbols | https://developer.authorize.net/api/reference/index.html |
| Invoice number | 20 | https://developer.authorize.net/api/reference/index.html |
| Supplier order reference | 25 | https://developer.authorize.net/api/reference/index.html |
| Customer ID | 20, alphanumeric only | https://developer.authorize.net/api/reference/index.html |
| Purchaser code (XSD) | 17 | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |
| Merchant / purchaser VAT registration (XSD) | 21 each | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |
| Summary commodity code (XSD) | 4 | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |
| Ship-to / ship-from zip | 20 each | https://developer.authorize.net/api/reference/index.html |
| Amex TAA (XSD) | amexDataTAA1 to amexDataTAA4, 40 each | https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |

### checkoutcom

| Key | Value | URL |
|---|---|---|
| Max line items | Visa or Mastercard Level 3: 50; Amex: 4; extra items ignored | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Visa from April 18, 2026 | CEDP only; Mastercard keeps Level 2 and Level 3 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Line name (Visa/MC) | up to 26; must not be or contain the merchant name | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Product code (`items[].reference`) | up to 12; not blank, null or all zeros | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Commodity code | up to 12 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Unit of measure | up to 12; accepted values are 36 Checkout.com measure codes (ACRE ... YARD); EACH filled when invalid | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Order / PO number (`processing.order_id`) | Visa/MC up to 15; Amex up to 17 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| CEDP reference | must be 12 chars or fewer | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Customer tax number | up to 13 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Ship-to postal | Visa/MC up to 10; Amex up to 50 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Ship-from postal | up to 10; US ZIP or ZIP+4; Canada uppercase with space | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Amex | Level 2 only; item name up to 12 | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| CEDP tax | 0 USD or 0.1% to 22% of amount | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Amounts | minor units, no decimal points | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |
| Large Ticket | may qualify at 8,000 USD or more on Corporate, Purchasing or Fleet | https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |

### jpmorgan

URL for all rows: https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards

| Key | Value |
|---|---|
| Max line items | Visa and ChaseNet 998; Mastercard 998; Discover and Diners Club 98; Amex uses advice text, not line items. Exceeding returns an error. |
| Amex transaction advice text | `transactionAdviceText` array, up to 4 entries, 40 chars each; format agreed with Amex, needs certification |
| Amount max digits | `totalShippingAmount` up to 9 digits (7 for zero-decimal currencies); Visa amount fields max length 12; Mastercard amount fields max length 9; longer values truncated from the right |
| Discover | `retailAddenda.isTaxable` required for Discover. Confirmed. |
| Mastercard Level 3 extra required fields (vs Visa) | `purchaseOrderNumber`; merchant tax ID `partyTaxGovernmentIssuedIdentifier`; `lineItemTaxIndicator`; `lineItemDiscountIndicator`; line tax `taxTypeCode` (1 char); `lineItemTaxAmount`. The page summary also lists `isTaxable` as Visa optional in the same group (lower confidence). Ship-to postal is required for both Visa and Mastercard when goods ship, so it is not a Mastercard-only extra. Ship-from postal is required for Visa, optional for Mastercard. |
| Unit of measure | Mastercard 3-letter list (see B4); required for both brands; non-space, non-zero |
| Description, product code, commodity code | required (commodity optional for Mastercard); must be non-space and non-zero; no max length stated |
| Customer reference, PO, invoice, tax ID, postal lengths | n/s |
| Totals | Visa: amount must equal sum of lines plus shipping, duty and VAT adjustments, else error |
| Timing | Level 3 in authorization or capture; capture wins; brands get data at settlement |

### mastercard-gateway-amex

Main URL: https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/ . API URL: https://test-gateway.mastercard.com/api/documentation/apiDocumentation/rest-json/version/latest/operation/Transaction%3a%20%20Authorize.html?locale=en_US

| Key | Value | URL |
|---|---|---|
| Max line items | n/s | main |
| Unit of measure | AN 2, X12-style list (720 codes, `amex-uom.csv`) | main |
| Line description | API 1 to 127; Amex length n/s | API |
| Product code (sku) | API 1 to 127 | API |
| Purchase order | AN 22 (`NER_5`) | main |
| Merchant purchase reference | truncated to 9 (LIDCAP) or 30 (FINCAP); API 1 to 40 | main, API |
| Customer reference | truncated to 17 or 20 by settlement file; API 0 to 25 ASCII | main, API |
| Requestor name | required for L3; API 1 to 100 | main, API |
| Tax exempt registration ID | required when taxStatus EXEMPT; API 1 to 30 | main, API |
| Ship-to postal | required for L2; API 1 to 10 | main, API |
| Charge description | 4 lines (TAA_4 to TAA_7); per-line length n/s | main |
| Industry keys | see section A | main |

### amex-advice-text

| Key | Value | URL |
|---|---|---|
| Lines | 4 | https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards |
| Chars per line | 40 (J.P. Morgan; Cybersource most processors; Authorize.net XSD). Worldpay VAP via Cybersource: 26. | https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards , https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-amex-data-taa1.html , https://api.authorize.net/xml/v1/schema/AnetApiSchema.xsd |
| Fill order | TAA1 first, then TAA2, and so on | https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/invoice-header-amex-data-taa1.html |
| Mastercard Gateway mapping | customData keys TAA_4 to TAA_7; defaults built from PO, requestor, first two items, shipping, handling, item count | https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/ |
| Line items alternative | Stripe forwards only the first 4 line items to Amex; Checkout.com accepts up to 4 Amex items | https://docs.stripe.com/payments/payment-line-items , https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data |

---

## D. MCC list comparison

Source: https://docs.stripe.com/payments/payment-line-items

| Spec list | Stripe page | Result |
|---|---|---|
| Visa Product 3 exclusions: 3000 to 3999, 4000 to 4010, 4112, 4411, 4511, 4722, 5812, 5814, 7011, 7512, 7513 | Visa Level 3/Product 3 not eligible: 3000-3999, 4000-4010, 5812, 5814, 7512, 7513, 7011, 4511, 4411, 4112, 4722 | Match |
| Visa federal government exclusions: 8011, 8021, 8031, 8041, 8042, 8043, 8046, 8050, 8062, 8071 | "GSA T&E cards (Visa)": federal corporate and purchasing cards not eligible for Product 3 at the same 10 MCCs | Match (Stripe scopes it to federally issued cards and Product 3) |
| Visa Level 2 fuel MCCs: 4468, 5499, 5541, 5542, 5983 | Visa sales tax must be 0.1% to 22% unless MCC is 4468, 5499, 5541, 5542 or 5983 | Match as a set. Stripe frames them as tax-rule exceptions, not as "fuel MCCs". |
| Mastercard Level 2 exclusions: 3000 to 3350, 3351 to 3500, 3501 to 3999, 4112, 4511, 5812, 7011, 7512, 7513, 7519 | Mastercard Level 2 not eligible: 5812, 3501-3999, 7011, 3351-3500, 7512, 7513, 7519, 3000-3350, 4511, 4112 | Match |
| Mastercard Level 3 additions: 4468, 5499, 5541, 5542, 5983, 8398 | Mastercard Level 3 list = Level 2 list plus 8398, 4468, 5499, 5541, 5542, 5983 | Match |
| Mastercard Level 2 tax-exempt MCCs: 4111, 4131, 4215, 4468, 4784, 5499, 5541, 5542, 5983, 7511, 8211, 8220, 8398, 8661, 9211, 9222, 9311, 9399, 9402, 9752 | Mastercard sales tax must be 0.1% to 30% unless MCC is 4468, 5541, 5542, 5499, 5983, 7511, 9752, 4111, 4131, 4215, 4784, 8211, 8220, 8398, 8661, 9211, 9222, 9311, 9399 or 9402 | Match (20 codes) |

Other differences found:

- Stripe's Visa Level 2 exclusion list (now historical) is 5812, 5814, 3501-4010, 3351-3500, 7512, 7513, 7011, 3000-3350, 4511, 4411, 4112, 4722, which covers the same codes as the Product 3 list.
- Stripe's "Industry to MCC codes" table labels 3000-3299 as airlines, while the exclusion lists use 3000-3350. The exclusion lists are what matter.
- Checkout.com's CEDP excluded MCCs omit 4000 to 4010: it lists 3000-3350, 4511, 3351-3500, 7512, 7513, 3501-3999, 7011, 4112, 4411, 4722, 5812, 5814 (https://www.checkout.com/docs/payments/manage-payments/enhanced-scheme-data/submit-level-2-or-level-3-data).
- Adyen blocks all enhanced data for 4112, 5812, 4468, 5541, 5542, 5983 and 5499, and routes 3000-3350, 3351-3500, 3501-3999, 4511, 7011, 7512, 7513, 7519 to industry data types instead of Level 2/3 (https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data). This is Adyen platform policy, not a brand list.

---

## Unconfirmed

1. Visa CEDP line and total rounding tolerance (B1, B9): no public value; processors that validate require an exact match.
2. Line-item detail indicator ownership (B3): no Visa public source; evidence points to processors setting it.
3. Mastercard unit list: no second public processor page with the same 3-letter codes (Checkout.com confirms the unit set with different values) (B4).
4. Mastercard line-item description maximum at brand level (B5); processors commonly use 26.
5. Amex requestor name maximum length for the Amex acquirer (B6); only the gateway's 100-char field limit is public.
6. Discover enhanced data program (B7): no announcement found.
7. Large Ticket qualification threshold in Visa's own documents (B8); only processor statements (8,000 USD at Checkout.com).
8. Per-line length of the Mastercard Gateway Amex charge description (TAA_4 to TAA_7).
9. Which Amex settlement file uses 17 vs 20 chars for the customer reference.
10. Adyen maximum number of line items.
11. Cybersource maximum line items for processors other than Barclays.
12. J.P. Morgan maximum lengths for description, product code, commodity code, PO, customer reference, tax ID and postal codes.
13. Stripe, Adyen and Braintree merchant tax ID fields (none documented).
