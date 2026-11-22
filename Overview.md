# l3lint: Commercial Card Enhanced Data Linter, Spec Set

Oct 19, 2026 · @brian

## How to use this spec set

This set specifies **l3lint**, an open-source tool that checks commercial card Level 2 and Level 3 payloads against Visa, Mastercard, American Express and Discover requirements before submission, then explains each likely downgrade and how to fix it. It ships as a CLI, an MCP server, a GitHub Action and a .NET library that share one rule engine.

Read the tabs in order the first time. After that, each tab stands alone as a reference.

| Tab | What it answers | Who needs it most |
| --- | --- | --- |
| Overview (this tab) | What the product is, the brand landscape, the decisions already made | Everyone |
| 1. Product requirements | Who it is for, what it must do, how success is measured | Product owner, reviewers |
| 2. Card brand rules | What each brand requires, every field, every formula, every rule | Rule authors, testers |
| 3. Architecture | How the code is organized and how a payload flows through it | Developers |
| 4. Interfaces | Exact CLI, config, output, MCP and GitHub Action contracts | Developers, integrators |
| 5. Test and delivery | How it is tested, packaged, released, and the task backlog | Developers, release owner |
| Sources | Every public page this spec relies on | Rule authors, reviewers |

### Conventions used in every tab

- **MUST, SHOULD, MAY** follow RFC 2119. A MUST is a release blocker; a SHOULD needs a written reason to skip.
- **Requirement IDs:** `FR-nnn` functional, `NFR-nnn` non-functional, `US-nn` user story. Backlog tasks cite the IDs they satisfy.
- **Rule IDs:** `<SCOPE>-<AREA>-<nnn>`. Scope is `GEN` (all brands), `VISA`, `MC`, `AMEX` or `DISC`. Area is `FMT` (format), `L2`, `L3`, `ARITH` (arithmetic), `DQ` (data quality), `ELIG` (eligibility) or `PCI`. Example: `VISA-L3-004`.
- **Money in examples** is USD in major units with two decimals unless a row says otherwise.
- **Confidence labels** on rules (`public-brand`, `processor-doc`, `inferred`, `policy`) are defined in the Card brand rules tab and travel with every rule into the code.
- **Working name:** l3lint. Checking the name on NuGet, GitHub and the MCP registry is a backlog task before the first public package.

## Product summary

l3lint catches enhanced-data problems before a commercial card transaction is submitted, while they are still cheap to fix. Today those problems surface weeks later as downgrades on an interchange report, or never surface at all.

**The problem.** Enhanced-data failures are silent: the transaction still approves, it just settles at a higher rate. Under Visa's Commercial Enhanced Data Program (CEDP), placeholder descriptions, bad line-item math and missing fields now fail validation, and the 0.05% participation fee is charged whether or not the data passes ([Spreedly](https://www.spreedly.com/blog/what-is-visa-cedp-commercial-enhanced-data-program)). The checks that exist live inside individual gateways, each covering a different subset, and most gateways only validate format, not brand requirements ([BlueSnap](https://developers.bluesnap.com/docs/level-23-data)).

**Who it is for.**

- Integration developers at merchants, ISVs and gateways who build the payloads.
- Payments analysts and solutions engineers who review client payloads and explain downgrades.
- Finance and AR teams who export invoices from an ERP and want to know if the data will qualify.
- AI coding agents that write payment integrations, through the MCP server.

**What ships.**

| Component | What it does | Primary user |
| --- | --- | --- |
| Rule engine and rule packs | Evaluates a payload against versioned, sourced rules for each brand | Every other component |
| CLI (`l3lint`) | Lints files or stdin, prints findings, exits non-zero on errors | Developers, analysts, CI scripts |
| MCP server (`l3lint-mcp`) | Exposes lint, explain, map and fix-suggestion tools to AI agents | AI coding agents, chat assistants |
| GitHub Action (`l3lint-action`) | Lints fixture and sample payloads on every pull request, uploads SARIF | Teams with payment code in GitHub |
| .NET library (`L3Lint.Core`) | Runs the same checks inside a service before submission | Backend teams on .NET |

**What it is not.**

- Not a gateway or processor. It never transmits a transaction or calls a network.
- Not a guarantee. Card networks and issuers make the final qualification decision; l3lint reports risk, not outcomes.
- Not a tax engine. It checks that tax data is present, plausible and consistent, never what the tax should be.
- Not a card data store. It refuses or masks anything that looks like a card number.
- Not a rate database. Cost estimates use a rate table the user supplies.

## Card brand landscape at a glance

Only Visa and Mastercard pay a published interchange reward for enhanced data in the US today; American Express and Discover accept some of the same data but offer no Level 3 rate program. l3lint still checks all four brands, because a payload that a gateway rejects or truncates for Amex or Discover is a bug either way, and the tool tells the user plainly when no rate benefit is possible.

| Brand | Program today | Level 2 reward | Level 3 reward | What l3lint checks |
| --- | --- | --- | --- | --- |
| Visa | CEDP. Product 3 replaced Level 3 in October 2025; Level 2 sunset in April 2026 ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program)) | None since April 2026 | Product 3 rates for verified merchants, or per transaction after validation | The full CEDP field set, line-item arithmetic, placeholder detection, card and MCC eligibility |
| Mastercard | Data Rate I, II and III continue; unit-of-measure rules tightened in October 2025 ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/)) | Data Rate II | Data Rate III | Tax range, customer code, line-item fields, the Mastercard unit-of-measure code list |
| American Express | Level 2 with Amex approval; line items travel as transaction advice text or acquirer-specific Level 3 records ([J.P. Morgan](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards)) | Only by direct agreement with Amex | No public program | Amex Level 2 fields and lengths, four 40-character advice lines, acquirer Level 3 formats |
| Discover | No Level 2 or 3 pricing ([Adobe Commerce](https://experienceleague.adobe.com/en/docs/commerce-merchant-services/payment-services/reporting/levels-card-payment-transactions)); line items accepted and passed through | None | None | Format, lengths and the 98 line-item limit; an informational note that no rate benefit applies |

The stakes are concrete. Visa's April 2026 schedule lists Commercial Product 3 at 1.75% + $0.10 against 2.70% + $0.10 for card not present, as reported by [weAudit](https://www.weaudit.com/level-3-credit-card-processing/).

**Key dates, newest first**

| Date | Event | Source |
| --- | --- | --- |
| April 2026 (sources say April 17 or 18) | Visa Level 2 (Product 2) sunsets | [TabaPay](https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update), [Checkout.com](https://support.checkout.com/hc/en-us/articles/30007033714194) |
| February 24, 2026 | Visa resumes merchant verification after a November 2025 pause and adds a merchant exclusion process | [TabaPay](https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update) |
| October 2025 | Mastercard tightens unit-of-measure requirements | [weAudit](https://www.weaudit.com/level-3-credit-card-processing/) |
| October 17 to 18, 2025 | Visa replaces Level 3 with CEDP Product 3; validation enforcement begins | [Checkout.com](https://support.checkout.com/hc/en-us/articles/30007033714194) |
| April 2025 | CEDP launches with a 0.05% participation fee on transactions carrying enhanced data | [Nuvei](https://www.nuvei.com/posts/visa-cedp-commercial-enhanced-data-program) |
| October 2024 | Visa announces CEDP | [Versapay](https://www.versapay.com/resources/visa-commercial-enhanced-data-program-guide) |

## Key decisions and constraints

Ten decisions are settled; changing any of them changes the architecture, so each one needs a written reason to reopen.

| ID | Decision | Why |
| --- | --- | --- |
| D1 | Rules come only from public sources: published brand documents, public interchange schedules and public processor or gateway documentation. Every rule cites its source URL. | Brand specifications such as Visa's Enhanced Data Services File Specifications are distributed to acquirers, not the public ([PayPal Braintree summary](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program)). Citing public pages keeps the project shareable and auditable. |
| D2 | No employer material, ever. The maintainer builds on personal time and equipment, gets written clearance under the employer's outside-activity and IP policy before the first public release, and leaves out adapters for the employer's own gateways unless legal clears them. | The maintainer works on Level 2/3 integrations professionally. Clean separation protects both sides. |
| D3 | The core is offline. No network calls, no telemetry by default. | Payloads can contain customer names, addresses and invoice data. Nothing leaves the machine unless the user wires it to. |
| D4 | No card numbers in scope. Inputs that contain a value passing a Luhn check in a card-number field are rejected or masked before any processing. | Keeps every user of l3lint outside PCI DSS scope for this tool. |
| D5 | Advisory language only. Output says "likely to downgrade" or "meets the documented requirement", never "qualifies". | Networks and issuers decide qualification, and Visa validates data after settlement. |
| D6 | Rules are versioned data plus small C# predicates. Each rule has an ID, brand, effective date range, severity, confidence label and source. | Brands change rules every April and October; data updates must ship without code changes. |
| D7 | One .NET 10 codebase in C# 14 serves the CLI, MCP server, GitHub Action and library. | .NET 10 is the current LTS; one engine means one set of tests and identical answers everywhere. |
| D8 | A canonical payload model sits in the middle. Gateway formats are adapters that map into it. | New gateways add an adapter, not new rules. |
| D9 | Money is `decimal` with an explicit ISO 4217 currency and exponent. Minor-unit integer inputs are converted at the adapter boundary. | Floating point breaks line-item arithmetic at the cent level. |
| D10 | License: Apache-2.0 for code and rule packs. | Patent grant, permissive, standard for developer tooling. Final call rests with the owner. |

## Glossary

| Term | Meaning in this spec |
| --- | --- |
| Acquirer | The merchant's bank or processor that submits transactions to the card network. |
| Issuer | The bank that issued the card. It decides how line items appear to the cardholder. |
| Interchange | The fee the acquirer pays the issuer on each transaction, set by network rate tables. |
| Downgrade | A transaction that settles in a more expensive interchange category than it could have, usually because data was missing or invalid. |
| Level 1, 2, 3 | Industry names for data depth: basic authorization data; plus tax and customer reference; plus invoice line items. |
| Product 2, Product 3 | Visa's CEDP names for Level 2 and Level 3. Product 2 is retired. |
| CEDP | Visa's Commercial Enhanced Data Program for US commercial cards. Validates enhanced data and rewards verified merchants. |
| Verified merchant | A Visa CEDP status earned by consistently sending valid data; it unlocks Product 3 rates at settlement. |
| Lagged interchange | Visa's process for non-verified merchants: standard rate first, then an adjustment if the data later validates. |
| Data Rate I, II, III | Mastercard's names for its commercial data tiers. |
| Commodity code | A standard product classification code on a line item, usually UNSPSC (8 digits) or NIGP. |
| Unit of measure (UOM) | The unit for a line item quantity. Mastercard and Amex accept specific code lists; Visa accepts up to 12 characters. |
| Line-item detail indicator | A Visa field that marks the record as line-item detail. |
| Debit/credit indicator | Marks whether a line item adds to or subtracts from the total (for example, a returned item). |
| Tax exempt indicator | An explicit flag that no tax applies. Under CEDP a bare zero tax amount is not enough. |
| Customer code | The buyer's reference, often a purchase order number. Required by Mastercard Level 2. |
| MCC | Merchant category code. Some MCCs are excluded from Level 2 and Level 3 rates. |
| Large Ticket | Visa interchange category for high-value corporate and purchasing card transactions, flagged under CEDP. |
| Transaction advice text | Amex free-text lines (up to 4 of 40 characters) that carry invoice detail instead of structured line items. |
| Canonical payload | l3lint's own neutral JSON model of a transaction, independent of any gateway. |
| Adapter | Code that maps a gateway's request format to and from the canonical payload. |
| Rule pack | A versioned bundle of rules for one brand, with sources and effective dates. |
| Finding | One rule result on one payload: rule ID, severity, location, message, fix. |
| SARIF | Static Analysis Results Interchange Format 2.1.0, the JSON format GitHub code scanning accepts. |
| MCP | Model Context Protocol, the open protocol AI clients use to call tools on servers. |
