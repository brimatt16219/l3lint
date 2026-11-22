# Cost estimates

With a rate table, l3lint estimates what a likely downgrade costs: the interchange at the likely tier, at the best tier, and the difference. Estimates are off unless you ask for them.

l3lint ships no rates. Interchange schedules change every April and October, and stale numbers would mislead. You keep the rate table, for example from your processor's pricing schedule.

## Use a rate table

Pass the file with `--rates`, or set `rates` in `.l3lint.yaml`:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20418.json --rates tests/L3Lint.Core.Tests/SpecExamples/rates-example.yaml
INV-20418  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  error       VISA-L3-004        /lineItems/1/commodityCode
           Line 2 has no commodity code; Visa needs one on every line for Product 3.
           Fix: Add the most specific 8-digit UNSPSC code for this item.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 1 error)
  Estimated, using My processor IC+ schedule (effective 2026-04-18):
    visa: likely cardNotPresent 7.81 USD, best product3 5.15 USD, difference 2.66 USD

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

On 280.36 USD, the missing commodity code likely costs about 2.66 USD on this one transaction. The check by hand: 280.36 x 2.70% + 0.10 = 7.67 at the card-not-present rate and 280.36 x 1.75% + 0.10 = 5.01 at Product 3, each plus the 0.05% program fee (0.14), rounded to the cent.

In JSON output the estimate is a `costEstimate` object on the transaction, labeled `estimate`:

```json
"costEstimate": {
  "label": "estimate",
  "rateTable": "My processor IC+ schedule",
  "effective": "2026-04-18",
  "currency": "USD",
  "estimates": [
    {
      "brand": "visa",
      "likelyTier": "cardNotPresent",
      "likelyCost": 7.81,
      "bestTier": "product3",
      "bestCost": 5.15,
      "difference": 2.66
    }
  ]
}
```

## It is an estimate

Every estimate names the rate table and its effective date. It is never the actual fee: interchange also depends on things l3lint does not see, such as issuer region, authorization timing and, for Visa, whether the merchant is verified under the Commercial Enhanced Data Program. Use it to rank problems, not to reconcile statements.

## The rate table

The file is YAML and must match [rates.schema.json](../schemas/rates.schema.json) (`l3lint schema rates` prints it). The example used above is `tests/L3Lint.Core.Tests/SpecExamples/rates-example.yaml` from the spec:

```yaml
name: My processor IC+ schedule
effective: 2026-04-18
currency: USD
programFees:
  - brand: visa
    when: enhancedDataSent
    percent: 0.05
categories:
  - brand: visa
    products: [corporate, purchasing]
    tier: product3
    percent: 1.75
    fixed: 0.10
  - brand: visa
    products: [corporate, purchasing]
    tier: cardNotPresent
    percent: 2.70
    fixed: 0.10
```

| Key | Required | Meaning |
| --- | --- | --- |
| `name` | yes | Shown with every estimate. |
| `effective` | yes | The date the rates apply from, `YYYY-MM-DD`. |
| `currency` | yes | ISO 4217 code. Only transactions in this currency get an estimate. |
| `programFees[]` | no | Fees on top of interchange: `brand`, `when` (`enhancedDataSent` or `always`), `percent`, optional `fixed`. |
| `categories[]` | yes, at least one | `brand`, `tier`, `percent`, optional `fixed`, optional `products` (card products the row applies to; without it, any product) and optional `name`. |

Percent values are in percent: `1.75` means 1.75%. Tiers: `product3`, `product2`, `cardNotPresent`, `cardPresent`, `largeTicket`, `dataRate3`, `dataRate2`, `dataRate1` and `standard`. Brands: `visa`, `mastercard`, `amex`, `discover`.

The example rates are the Commercial Product 3 and Commercial Card Not Present rates (1.75% + 0.10 and 2.70% + 0.10). The project's research notes (`docs/dev/research/facts-and-profiles.md`, item B8) found these values in Visa's public USA Interchange Reimbursement Fees schedule, <https://usa.visa.com/content/dam/VCOM/download/merchants/visa-usa-interchange-reimbursement-fees.pdf>. The 0.05% CEDP program fee is reported by [PayPal](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program); it is a network fee and is not in that schedule. Check every value against your own processor's schedule before you rely on it.

## How the estimate is computed

1. For each brand evaluated, take the [likely tier](likely-tiers.md) and the best tier.
2. A likely tier below the brand's ladder (`belowProduct3`, for example) uses the `cardPresent` or `cardNotPresent` category, by the payload's `channel`. Without a channel, `cardNotPresent` applies.
3. Find the category for the brand, tier and card product. A row with `products` that lists the card product wins over a row without `products`.
4. Cost = amount x percent / 100 + fixed, plus each matching program fee. A fee with `when: enhancedDataSent` applies when the payload carries line items, a tax amount or a customer reference.
5. Round to the currency's minor unit, half away from zero.

A brand with no matching category gets no estimate. American Express and Discover never get one, because no enhanced-data tier exists for them.

## Visa Large Ticket

When the rate table has a Visa `largeTicket` category for the card product, VISA-LT-001 computes the amount where Large Ticket costs the same as Product 3, and reports, as information, transactions at or above it. Without such a category the rule never fires; l3lint does not hard-code a break-even amount, because it moves with every schedule.

With the example table plus a Large Ticket row (`percent: 1.30`, `fixed: 35.00`, the Commercial Product Large Ticket rate in the same Visa schedule):

```text
$ sed 's/"amount": 280.36/"amount": 10000.00/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only VISA-LT-001 --rates rates-lt.yaml
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  information VISA-LT-001        /amount
           The amount 10000.00 is above the Large Ticket break-even of 7755.56 in rate table My processor IC+ schedule.
           Fix: Ask your processor about Large Ticket.
           Source: https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program  (processor-doc)
  Likely tier: Product 3 (meets the documented requirement)
  Estimated, using My processor IC+ schedule (effective 2026-04-18):
    visa: likely product3 180.10 USD, best product3 180.10 USD, difference 0.00 USD

1 transaction, 0 errors, 0 warnings, 1 information. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

The break-even is (35.00 - 0.10) / (1.75% - 1.30%) = 7,755.56. The run uses `--only` so the arithmetic rules do not report the edited amount.

## Errors

A rate table that cannot be read is a usage error (exit code 2) when `--rates` names it, and a configuration error (exit code 3) when `.l3lint.yaml` names it. A table that does not match the schema is a configuration error (exit code 3).
