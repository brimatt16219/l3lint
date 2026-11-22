# Limitations

l3lint reduces the risk of a downgrade. It is not a guarantee: card networks and issuers make the final decision about the rate, and l3lint reports risk, not outcomes. This page lists what l3lint does not do, the facts that public sources have not settled, and the lists that are known to be incomplete.

## What l3lint does not do

- **It does not submit transactions** or call any gateway, processor or network.
- **It does not predict the rate.** A clean result means the payload meets the documented requirements that l3lint checks. Interchange also depends on things l3lint never sees, such as issuer region and authorization timing.
- **It cannot see Visa's own review.** Visa checks enhanced data with its own validation after each settlement batch, and that logic is not public. Some l3lint checks are therefore inferred, and they carry the `inferred` confidence label.
- **It does not know a merchant's Visa status.** Whether a merchant is verified under the Commercial Enhanced Data Program changes when Product 3 rates apply. l3lint does not see it.
- **It does not calculate tax.** It checks that tax data is present, in range and consistent, never what the tax should be.
- **It does not invent data.** `fix` never writes descriptions, commodity codes or tax amounts.
- **It does not store card data**, and it masks what looks like a card number. A card number inside a longer text is not detected; see [Privacy and security](privacy-and-security.md#the-limit-of-this-check).
- **It ships no interchange rates.** Cost estimates need a rate table you keep, and they are estimates.
- **It does not validate UNSPSC codes against the full code list.** GEN-DQ-006 checks shape and specificity only. l3lint does not ship the list, to avoid redistribution questions.
- **It covers US programs only.** EU, UK and Canadian enhanced-data programs, and industry data such as airline, lodging, car rental and fleet fuel, are not checked.
- **The CLI does not load plugin code.** Custom checks need the .NET library.

## Facts that public sources have not settled

These items were researched in October 2026 (`docs/dev/research/facts-and-profiles.md` in the repository). Where no public source settles a fact, l3lint uses the reading described here.

| Fact | Status | What l3lint does |
| --- | --- | --- |
| Visa's rounding tolerance for line totals and the transaction total | Not public. Processors that validate (Stripe, J.P. Morgan) reject any mismatch. | 1 minor unit by default; `arithmetic.lineTolerance` and `totalTolerance` change it |
| The last day of Visa Level 2 | Sources give April 17 and April 18, 2026. Visa's public interchange schedule removed the Level 2 rows in the edition with rates effective April 18, 2026. | The Visa Level 2 rules end on 2026-04-17 |
| Who sets Visa's line-item detail indicator | No public Visa source. The evidence points to processors setting it. | VISA-L3-010 is a warning that asks you to confirm your processor sets it |
| Mastercard's unit-of-measure list after October 2025 | The 3-letter codes come from J.P. Morgan only. Checkout.com publishes the same units with its own 4-letter values. | MC-L3-005 uses the J.P. Morgan list |
| Mastercard's maximum line description length | No public brand value. Processors commonly use 26 characters. | For Mastercard, only a processor profile sets a description limit (Visa's 26 is VISA-L3-014) |
| American Express requestor name length under structured acquirer profiles | No public value; the gateway field allows 1 to 100 characters | GEN-FMT-006 uses 40 characters, from the spec's canonical field dictionary |
| A Discover enhanced-data program | No public announcement found | DISC-ELIG-001 reports that no rate benefit applies |
| Visa Large Ticket qualification amount | Not in Visa's public schedule; one processor states 8,000 USD | VISA-LT-001 computes a break-even from your rate table and never hard-codes one |

Other open points from the same research: the per-line length of American Express charge descriptions at the Mastercard Gateway, which American Express settlement file keeps 17 or 20 characters of the customer reference, Adyen's maximum number of line items, Cybersource's maximum line items for most processors, J.P. Morgan's field lengths, and merchant tax ID fields at Stripe, Adyen and Braintree.

## Adapter fields

Each gateway's documentation was checked field by field in October 2026. A few mappings could not be confirmed from public pages, for example whether Braintree's `orderId` reaches the networks as the invoice number. Each is marked "Unconfirmed" in [Adapters and field mappings](adapters.md), and the full record is in [the adapter verification](../dev/verification-adapters.md).

## Heuristics and thresholds

The data quality rules (`GEN-DQ-*`) are l3lint's own inferences, consistent with public statements but not stated as rules by any brand. Their thresholds are inferred too: the 0.92 similarity for "merchant name used as description", and 20 transactions with an 80% share for the batch rule GEN-DQ-007. They are warnings by default, and the batch thresholds are configurable under `rules.batch`.

## Rules change

Brands change requirements, usually in April and October. Each rule has effective dates and each pack has a version, so `--as-of` can check older payloads, but a pack is only as current as its last release. Check `l3lint rules list` for each pack's version and the rule pages for their sources.
