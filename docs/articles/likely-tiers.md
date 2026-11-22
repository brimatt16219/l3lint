# Likely tiers

After the findings for a transaction, l3lint prints a likely tier for each brand it evaluated. The tier answers the question users actually ask: "Is this payload likely to downgrade?"

The answer is advisory. It says whether the payload meets the documented requirements of a data tier, as far as public sources describe them. It does not predict the rate. The card networks and the issuer decide that, the interchange rate also depends on things l3lint never sees (such as issuer region and authorization timing), and Visa reviews enhanced data after settlement.

## How the tier is worked out

For each brand:

1. Start from the **best tier** the card product and the MCC allow. If no tier exists (a consumer card, an excluded MCC, or a brand with no enhanced-data program), the result is "no enhanced-data rate available".
2. Step down past every tier that an **error** finding blocks. Only findings of that brand's pack or of the general pack count. An error with impact `blocksLevel3` removes the Level 3 tiers; an error with impact `blocksLevel2` removes the Level 2 and Level 3 tiers.
3. The first tier left is the **likely tier**.
4. For Visa only, a warning or error with impact `riskVisaValidation` marks Product 3 "at risk" instead of blocked, because Visa judges data quality after settlement.

Warnings never block a tier. Information findings never change it.

## Tier ladders

**Visa**

| Tier | When it is available |
| --- | --- |
| Product 3 | Business, corporate, purchasing and fleet cards (and an unknown product), at an MCC that is not excluded from Product 3. Federal government cards at the excluded medical MCCs get no tier. |
| Product 2 | The same cards, while the as-of date is on or before 2026-04-17. Visa's Level 2 ended after that day. |
| Below Product 3, below Product 2 | The data does not meet the requirements of the tiers above. |

Consumer and commercial prepaid cards, and the MCCs on Visa's Product 3 exclusion list, have no Visa tier.

**Mastercard**

| Tier | When it is available |
| --- | --- |
| Data Rate III | Corporate and purchasing cards (and an unknown product) whose MCC is on neither exclusion list and whose product code is not MBS or MEF |
| Data Rate II | Business, corporate and purchasing cards (and an unknown product) whose MCC is not on the Level 2 exclusion list |
| Data Rate I | The data does not meet the requirements of the tiers above |

**American Express and Discover** always report "no enhanced-data rate available". American Express has no public Level 3 rate program and Level 2 pricing needs a direct agreement with American Express. Discover has no Level 2 or Level 3 pricing. l3lint still checks their format and limits, so data is not rejected or cut off in transit.

The exclusion lists ship in the rule packs, with their public sources. Each brand's eligibility rules (`VISA-ELIG-*`, `MC-ELIG-*`, `GEN-ELIG-*`) report why a tier is not available.

## Statuses

| Status | Text output | JSON `status` |
| --- | --- | --- |
| The payload meets the documented requirements of the best tier | `Product 3 (meets the documented requirement)` | `meets` |
| Visa may still reject the data after settlement | `Product 3 (at risk)` | `atRisk` |
| An error blocks the best tier | `below Product 3 (blocked by 1 error)` | `blocked` |
| No enhanced-data rate exists | `no enhanced-data rate available` | `noBenefit` |

## Examples

The worked example payload, `samples/action-example/fixtures/payments/INV-20417.json`, gets `Likely tier: Product 3 (meets the documented requirement)`, as shown in [Getting started](getting-started.md). The examples below change it with `sed` on the way in and lint it from standard input.

A placeholder description ("Item") is present data that Visa is reported to reject, so Product 3 is at risk:

```text
$ sed 's/"description": "Isolation gown blue cs 50"/"description": "Item"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint -
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  error       VISA-DQ-001        /lineItems/1/description
           Line 2 description "Item" is a placeholder Visa rejects.
           Fix: Use the real item name.
           Source: https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: Product 3 (at risk)

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

On a consumer card no enhanced-data rate exists, so l3lint says so once and does not report rate-related errors:

```text
$ sed 's/"product": "purchasing"/"product": "consumer"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint -
INV-20417  canonical  visa consumer  MCC 5047  as of 2026-10-19
  information GEN-ELIG-001       /card/product
           This is a consumer card, so no enhanced-data rate is available.
           Fix: None; enhanced data earns no rate on consumer cards.
           Source: https://docs.stripe.com/payments/payment-line-items  (processor-doc)
  Likely tier: no enhanced-data rate available

1 transaction, 0 errors, 0 warnings, 1 information. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

With `--brand all`, one line per brand:

```text
  Likely tier (visa): Product 3 (meets the documented requirement)
  Likely tier (mastercard): Data Rate II (blocked by 2 errors)
  Likely tier (amex): no enhanced-data rate available
  Likely tier (discover): no enhanced-data rate available
```

The same payload meets Visa's documented requirements but uses units (BX, CA) that are not on Mastercard's unit list, so it is likely to settle at Data Rate II on a Mastercard card. See [Using the CLI](cli.md#set-the-context) for the full command.

## In JSON

`likelyTier` holds the tier of the transaction's brand; `likelyTiers` holds one entry per brand when more than one was evaluated:

```json
"likelyTier": {
  "best": "product3",
  "likely": "belowProduct3",
  "status": "blocked",
  "blockingErrors": 1
}
```

Tier names in JSON: `product3`, `product2`, `belowProduct3`, `belowProduct2`, `dataRate3`, `dataRate2`, `dataRate1` and `none`. See the [JSON results reference](../reference/results.md#schema-tier).

## Rules that did not run

The rollup only sees the findings of rules that ran. If you narrow the run with `--only` or `--disable`, a tier can read "meets the documented requirement" because the rules that would block it were skipped. Use the full rule set when the tier matters.

## Cost of a downgrade

With a rate table, l3lint also estimates the cost at the likely tier and at the best tier. See [Cost estimates](cost-estimates.md).
