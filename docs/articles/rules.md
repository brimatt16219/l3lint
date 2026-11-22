# Rules, severity and confidence

l3lint ships 78 rules in five rule packs. This page explains what a rule is made of and how to read a finding. The [rule reference](../rules/index.md) has one page per rule.

## Rule IDs and packs

A rule ID has three parts: scope, area and number, for example `VISA-L3-004`.

| Scope | Pack | Rules | Applies to |
| --- | --- | --- | --- |
| `GEN` | `l3lint.rules.general` | 23 | Every brand: card-number safety, format, arithmetic, eligibility and data quality |
| `VISA` | `l3lint.rules.visa` | 26 | Visa, including the Commercial Enhanced Data Program (CEDP) |
| `MC` | `l3lint.rules.mastercard` | 17 | Mastercard Data Rate II and III |
| `AMEX` | `l3lint.rules.amex` | 9 | American Express format and completeness |
| `DISC` | `l3lint.rules.discover` | 3 | Discover format and limits |

Areas: `FMT` format, `L2` Level 2, `L3` Level 3, `ARITH` arithmetic, `DQ` data quality, `ELIG` eligibility, `PCI` card numbers, `LT` Large Ticket.

The general pack always runs. A brand pack runs for the transaction's brand: `--brand`, else `card.brand` in the payload, else `context.brand` in the config file. With `--brand all`, every brand pack runs. When the brand is unknown, only the general pack runs and l3lint adds the information finding `L3LINT-CTX-001`.

Each pack has its own version. `l3lint version` and the summary line of every run show the versions, and `.l3lint.lock` can pin them (see [Configuration](configuration.md#the-lock-file)).

## Severity

| Severity | Meaning | Default exit code |
| --- | --- | --- |
| `error` | The payload misses a documented requirement, or has a problem that blocks a data tier | 1 |
| `warning` | Likely to cause trouble, or a requirement that only some processors state | 0 |
| `information` | Context, such as "no enhanced-data rate exists for this card" | 0 |

`--fail-on` (or `output.failOn`) picks the lowest severity that sets exit code 1. Change one rule's severity with `--severity RULE=level` or `rules.severity` in the config file.

Heuristics that look for fake data are warnings by default. They are errors only under Visa, where public sources name the exact pattern Visa rejects.

## Confidence labels

Every rule carries a label that says how sure the requirement is. It prints next to every source, so you can judge a finding without trusting l3lint blindly.

| Label | Meaning |
| --- | --- |
| `public-brand` | Stated in a document the card brand itself publishes. No v1.0 rule has it yet. |
| `processor-doc` | Stated in public documentation from a gateway, processor or acquirer. Most field, format, limit and eligibility rules. |
| `inferred` | l3lint's own heuristic. It agrees with public statements but no source states it as a rule. The data quality detectors and their thresholds. |
| `policy` | l3lint's own safety or input rule, not a brand requirement. Card-number masking, the schema check and currency hygiene. |

Card brand specifications are distributed to acquirers, not to the public, so most rules cite a processor or gateway page. Where public sources disagree, the rule records all of them, encodes the stricter reading and says so in its notes.

## Levels

`general`, `L2` or `L3`. The level is the data depth a rule protects: Level 2 is tax and customer reference, Level 3 is invoice line items. `l3lint rules list --level L3` filters by it.

## Impact

The impact class says what a finding means for the rate. It drives the [likely tier](likely-tiers.md).

| Impact | Meaning |
| --- | --- |
| `blocksLevel3` | The payload cannot reach Level 3, Product 3 or Data Rate III |
| `blocksLevel2` | The payload cannot reach Level 2 or Data Rate II either |
| `riskVisaValidation` | Fields are present but likely to fail Visa's review after settlement |
| `truncation` | Data will be cut or changed in transit; no rate effect expected |
| `noBenefit` | No enhanced-data rate exists for this card, brand or MCC |
| `hygiene` | An input problem or a safety issue |

## Effective dates

Brands change requirements on fixed dates, so every rule has a start date, an end date, or both. l3lint picks the rules in force on the as-of date: `--as-of`, else the payload's `transactionDate`, else today.

Visa's Level 2 rules end on 2026-04-17, the last day Visa Level 2 applied. They stay in the pack so older payloads can still be checked:

```text
$ l3lint rules list --brand visa --level L2
```

The rule table of that output:

```text
| Rule          | Severity | Level | Confidence    | Effective                                    | Title                                                 |
|---------------+----------+-------+---------------+----------------------------------------------+-------------------------------------------------------|
| VISA-ELIG-005 | warning  | L2    | processor-doc | always                                       | Card not present without a billing postal code        |
| VISA-L2-001   | error    | L2    | processor-doc | until 2026-04-17 (last day the rule applies) | Tax outside 0.1% to 22% of the amount                 |
| VISA-L2-002   | error    | L2    | processor-doc | until 2026-04-17 (last day the rule applies) | Tax indicator missing                                 |
| VISA-L2-003   | error    | L2    | processor-doc | until 2026-04-17 (last day the rule applies) | Purchasing card at a fuel MCC without a customer code |
```

A tax of 70.00 on 280.36 is 24.97%, above Visa's old Level 2 range. On a date when the rule applied:

```text
$ sed 's/"amount": 17.36/"amount": 70.00/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only 'VISA-L2-*' --as-of 2026-03-01
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-03-01
  error       VISA-L2-001        /tax/amount
           Tax is 24.97% of the amount; Visa Level 2 needs 0.1% to 22%.
           Fix: Correct the tax amount or mark the sale exempt.
           Source: https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/  (processor-doc)
  Likely tier: below Product 2 (blocked by 1 error)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Without `--as-of`, the payload's date (2026-10-19) applies, the rule no longer applies, and the same input reports no finding.

## Supersession

Some rules replace another rule's finding at the same path, so one problem is reported once. For example, VISA-DQ-001 (an error under Visa) supersedes GEN-DQ-001 (a warning for every brand). With a placeholder description `Item` on a Visa card, only the Visa rule reports:

```text
  error       VISA-DQ-001        /lineItems/1/description
           Line 2 description "Item" is a placeholder Visa rejects.
```

On a Mastercard card, the general rule reports instead:

```text
  warning     GEN-DQ-001         /lineItems/1/description
           Line 2 description "Item" is a placeholder, not an item name.
```

Each rule page lists what the rule supersedes. The brand length and code list rules also supersede the general length rule GEN-FMT-006 at the same path.

## Single-processor rules and profiles

Some requirements appear in only one processor's public documentation. Those rules name the profiles they belong to. Under any other profile their severity is capped at warning, so one processor's rule does not become an error for everyone. Select the profile with `--processor`, or pass `--strict-profiles` to keep the catalog severity everywhere.

AMEX-L2-001 (ship-to postal code missing) is an error in the catalog and belongs to the `mastercard-gateway-amex` profile:

```text
$ sed 's/"toPostalCode": "33431", //' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --brand amex --only AMEX-L2-001 --quiet
1 transaction, 0 errors, 1 warning. Rule packs: AMEX 1.0.0, GEN 1.0.0.
$ sed 's/"toPostalCode": "33431", //' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --brand amex --only AMEX-L2-001 --processor mastercard-gateway-amex --quiet
1 transaction, 1 error, 0 warnings. Rule packs: AMEX 1.0.0, GEN 1.0.0.
```

The single-processor rules are MC-L3-008, MC-L3-009, MC-FMT-001, AMEX-FMT-001, AMEX-L2-001, AMEX-L2-004, AMEX-L3-002, DISC-FMT-001 and DISC-L2-001. See [Processor profiles](processor-profiles.md).

## How to read a finding

This finding comes from a Stripe request whose second line lost its commodity code:

```text
$ sed 's/"commodity_code": "42131606"//' fixtures/adapters/stripe/01-complete-level3.json | l3lint lint - --brand visa --card-product purchasing --mcc 5047 --only VISA-L3-004
Transaction 1  stripe  visa purchasing  MCC 5047  as of 2026-10-30
  error       VISA-L3-004        /amount_details/line_items/1/payment_method_options/card/commodity_code
           Line 2 has no commodity code; Visa needs one on every line for Product 3.
           Fix: Add the most specific 8-digit UNSPSC code for this item.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
```

1. **Severity and rule ID.** Open the rule's page for the requirement, the reason and examples: `l3lint explain VISA-L3-004`.
2. **Path.** Where the problem is in your file. For a gateway request this is the gateway's own JSON Pointer. JSON output also gives the canonical path and the line and column.
3. **Message.** What is wrong, with the value when it helps. Card numbers are always masked.
4. **Fix.** What to change, stated as an action. `l3lint fix` applies the fixes that have one right answer.
5. **Source and confidence.** The public page behind the rule, and how direct that source is.

The findings for one transaction are followed by the likely tier for each brand that was evaluated. See [Likely tiers](likely-tiers.md).

## Engine diagnostics

`L3LINT-CTX-001` and `L3LINT-ENGINE-001` come from the engine, not a pack. See [Output formats](output-formats.md#engine-diagnostics).
