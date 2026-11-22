# Facts to verify

This file lists every fact the spec marks for verification, what public sources showed, and what l3lint ships today. It also lists release-time facts the owner must confirm. Research notes and every URL consulted are in the repository files `docs/dev/research/facts-and-profiles.md` (retrieved 2026-10-25) and `docs/dev/research/adapters.md`. Field-level adapter checks are in [verification-adapters.md](verification-adapters.md).

Status words: **confirmed** (a public source states it), **unconfirmed** (no public source found; l3lint keeps the spec's assumption), **owner** (only the owner can check it, for example because it needs a GitHub account).

## Spec list: facts to verify before v1.0 (Card brand rules tab)

| Fact | Status | What sources show | What l3lint does |
| --- | --- | --- | --- |
| Visa CEDP line and total rounding tolerance | unconfirmed | No public Visa source states a tolerance. Stripe, J.P. Morgan, Braintree, Checkout.com, Adyen and Payrix give the formulas without a tolerance; Stripe and J.P. Morgan reject any mismatch. | Default tolerance stays 1 minor unit (`arithmetic.lineTolerance` and `totalTolerance`, 0.01), as the spec says, and is configurable. A team that wants the strictest reading sets both to 0. |
| Exact date of the Visa Level 2 sunset | confirmed | Visa's public interchange schedule lists Commercial and Business Level II in the 18 October 2025 edition and removes them in the edition with rates effective 18 April 2026. Only the fuel row remains. | Level 2 rules (VISA-L2-001 to 003) and the Product 2 tier end on 2026-04-17, the last day they applied. |
| Who sets the Visa line-item detail indicator | unconfirmed | No merchant-facing API read exposes the field; it appears to be a clearing-record field processors build. Not stated by Visa. | VISA-L3-010 stays a warning, reported once per transaction, with the fix "Confirm your processor sets it." |
| Mastercard unit list against a second source | confirmed (code set) | J.P. Morgan's list equals the spec's 32 codes. Checkout.com has the same 36 unit concepts with its own 4-letter API values, so only the unit set has a second source. | `mastercard-uom.csv` holds the 32 codes; status complete. |
| Mastercard line description maximum | unconfirmed | No public Mastercard page. Processors use 26 (Checkout.com, Adyen, Stripe), 35 (Braintree) or 127 (Mastercard Gateway). | No brand-level rule; processor profiles set `descriptionMax` where the processor documents it (GEN-FMT-006). |
| Amex requestor name maximum | unconfirmed | Listed as a Level 3 prerequisite on the Mastercard Gateway Amex page, with no length; the gateway field allows 1 to 100. | GEN-FMT-006 uses the canonical default of 40; profiles can change it. |
| Discover enhanced-data program | unconfirmed | No public announcement found. Processors accept Discover line items (J.P. Morgan: up to 98). | DISC-ELIG-001 reports that no Discover Level 2 or 3 rate exists (information). |
| Large Ticket break-even for VISA-LT-001 | confirmed | Visa's 24 October 2026 schedule: Commercial Product 3 1.75% + $0.10, Large Ticket 1.30% + $35.00, so the break-even is $7,755.56, matching PayPal's "about $7,756". | VISA-LT-001 computes the break-even from the user's rate table and never hard-codes it; without a rate table it does not run. |

Naming caution from the research: in Visa's current schedule "Product 2" names card-present programs, not Level 2. l3lint's tier name `product2` means the pre-sunset Visa Level 2 tier, as the spec's tier ladder defines it; the rules and docs say "Level 2" in prose.

## Lists and tables shipped as data

| Data | Source | Status |
| --- | --- | --- |
| Amex unit codes (`amex-uom.csv`, 720 codes) | Mastercard Gateway, Level 2 and 3 data for the Amex acquirer, table "Item unit of measure enumeration values" | complete; matches the live table by SHA-256 on 2026-10-25. Six codes appear without X12's leading zero and are kept as the page shows them. |
| Mastercard unit codes (32) | J.P. Morgan commercial cards page | complete |
| Visa Product 3 and federal excluded MCCs, Visa fuel MCCs | Stripe payment line items guide | matches Stripe; Checkout.com's CEDP list leaves out 4000 to 4010 |
| Mastercard Level 2, Level 3 and tax-exempt MCCs | Stripe and J.P. Morgan pages | matches Stripe |
| ISO 4217 table (165 currencies) | ISO 4217 list published 2026-09-17 | current at build time |
| Processor profiles (10) | each limit names its own URL in `src/L3Lint.Core/Profiles/*.yaml` | values confirmed where the profile cites a page; Adyen's and most Cybersource processors' line-item maxima are not published |

## Adapter fields marked "to verify" (asterisked cells)

All 35 asterisked cells of the spec's field mapping table are checked one by one in [verification-adapters.md](verification-adapters.md), with the source page and date. The corrections there differ from the spec table and are applied in the adapters (Cybersource `invoiceDetails.taxable` and `shippingDetails.shipFromPostalCode`; Authorize.net `currencyCode`, `discount.amount`, `shipFrom.zip`; Checkout.com `processing.discount_amount`, `shipping_tax_amount`, `shipping.from_address_zip`). Cybersource `purchaseOrderDate` and Authorize.net `purchaseOrderDateUTC` exist in the gateways' public schemas but stay unmapped, because the spec table says the order date is not supported for those gateways; the owner may add them in a spec update.

## Release-time facts for the owner

| Fact | Status | Where it matters |
| --- | --- | --- |
| Final package names and repository (`l3lint`, `L3Lint.*`, `brimatt16219/l3lint`, MCP name `io.github.brimatt16219/l3lint`) | owner (T-01) | `Directory.Build.props` (`L3LintProjectUrl`, `L3LintDocsUrl`, ADR-0044), `src/L3Lint.Mcp/.mcp/server.json`, `action/README.md` |
| Written employer clearance | owner (T-02, D2) | before any publication |
| Third-party action commit SHAs | confirmed 2026-10-29 from each action's release page | `action/action.yml` and `.github/workflows/*.yml`; recheck before the first release |
| `mcp-publisher login github-oidc` works from GitHub Actions for the `io.github.brimatt16219` namespace | owner | `release.yml`, publish step |
| NuGet API key and optional signing certificate secrets | owner | `release.yml` |
| Code scanning enabled on the public sample repository | owner | Product requirements release gate ("the Action runs green in a public sample repository") |
| MCP Inspector checks | owner | Product requirements release gate; the server passes the SDK client tests over stdio and HTTP |
