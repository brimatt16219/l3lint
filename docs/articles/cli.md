# Using the CLI

This guide shows the common tasks. The [CLI reference](../reference/cli.md) lists every command and option, generated from the tool itself. Run `l3lint <command> --help` for the same text in a terminal.

The examples run from the root of a clone of the l3lint repository. Output is real output from l3lint 1.0.0. Long output is shortened where a note says so. When a payload carries no transaction date, the `as of` date is the day you run the command, so yours will differ.

## Lint files, folders, globs and standard input

`lint` takes one or more inputs. Each input is a file, a folder, a glob such as `fixtures/**/*.json`, an NDJSON file, or `-` for standard input.

```text
$ l3lint lint samples/action-example/fixtures/payments
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: Product 3 (meets the documented requirement)

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

2 transactions, 1 error, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Standard input is useful in pipelines. This one changes line 1's total on the way in:

```text
$ sed 's/"total": 120.00/"total": 125.00/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint -
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  error       GEN-ARITH-002      /amount
           The amount is 280.36; lines and summary amounts add up to 285.36 (difference -5.00). A line total is likely wrong; fix that first.
           Fix: Recompute the amount, or fix the line or summary values.
           Source: https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/  (processor-doc)
  error       GEN-ARITH-001      /lineItems/0/total
           Line 1 total is 125.00; quantity, unit cost and discount give 120.00 (difference 5.00).
           Fix: Recompute the line total.
           Source: https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 2 errors)

1 transaction, 2 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Files ending in `.ndjson` or `.jsonl` hold one transaction per line. l3lint reads them one record at a time, and the size limit applies to each record. The `invoices.ndjson` below was written by `l3lint convert --canonical-only` from a CSV export (see [Convert](#convert-a-gateway-request-to-canonical-json)).

```text
$ l3lint lint invoices.ndjson --as-of 2026-10-19 --quiet
2 transactions, 4 errors, 5 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

A file name that starts with a dash would look like an option. Write it as `./-name.json`.

## Lint a gateway request

Without `--format`, l3lint detects the format. Gateway requests rarely carry the card brand, card product or MCC, so pass them:

```text
$ l3lint lint fixtures/adapters/stripe/01-complete-level3.json --brand visa --card-product purchasing --mcc 5047
Transaction 1  stripe  visa purchasing  MCC 5047  as of 2026-10-30
  error       VISA-L3-011        /amount_details/discount_amount
           /amounts/discount is missing; Visa needs it for Product 3, 0 when it does not apply.
           Fix: Send 0 for each that does not apply.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  error       VISA-L3-011        /amounts/duty
           /amounts/duty is missing; Visa needs it for Product 3, 0 when it does not apply.
           Fix: Send 0 for each that does not apply.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  error       VISA-L3-009        /amount_details/line_items/1/discount_amount
           Line 2 has no discount amount; Visa needs one, 0 when there is no discount.
           Fix: Send 0 when there is no discount.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  warning     VISA-L3-013        /shipping/toCountry
           Freight is charged but /shipping/toCountry is missing.
           Fix: Send the shipping postal codes and country.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 3 errors)

1 transaction, 3 errors, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Paths point at the Stripe request when Stripe has the field (`/amount_details/line_items/1/discount_amount`). When the format cannot carry a field at all, such as duty for Stripe, the path is the canonical one (`/amounts/duty`).

Without `--brand`, an unknown brand runs only the general rules and adds the information finding `L3LINT-CTX-001`. When detection cannot choose between two formats, that input is skipped, the message lists the candidates and the run exits with code 2; pass `--format` to choose. Braintree requests carry no currency, so pass `--currency USD` (or set `inputs.defaultCurrency`). See [Adapters and field mappings](adapters.md).

## Set the context

| Option | What it sets |
| --- | --- |
| `--brand` | The brand whose rules run: `visa`, `mastercard`, `amex`, `discover` or `all`. `auto` (the default) reads `card.brand`. `all` lints against every brand and reports a likely tier for each. |
| `--card-product` | The card product, such as `purchasing`. Eligibility rules need it. |
| `--mcc` | The merchant category code. Some MCCs are excluded from enhanced-data rates. |
| `--merchant-country` | The merchant country. The default is `US`. |
| `--processor` | A [processor profile](processor-profiles.md), or the path of a profile file. |
| `--as-of` | The date whose rules apply. The default is the payload's `transactionDate`, else today. |

A flag beats the payload, which beats the config file, which beats the built-in default. When a flag changes a value the payload sent, the output says so:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json --brand mastercard --card-product corporate
INV-20417  canonical  mastercard corporate  MCC 5047  as of 2026-10-19
  Note: Card brand set to mastercard by option; the payload said visa.
  Note: Card product set to corporate by option; the payload said purchasing.
  error       MC-L3-005          /lineItems/0/unitOfMeasure
           Line 1 unit of measure BX is not on the Mastercard list.
           Fix: Map to a listed code such as EAC or PCE.
           Source: https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards  (processor-doc)
```

The output is shortened here; the full run lists two errors and six warnings and ends with `Likely tier: Data Rate II (blocked by 2 errors)`.

`--brand all` shows one likely tier per brand:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json --brand all
INV-20417  canonical  visa+mastercard+amex+discover purchasing  MCC 5047  as of 2026-10-19
```

The findings are left out here. The run ends with:

```text
  Likely tier (visa): Product 3 (meets the documented requirement)
  Likely tier (mastercard): Data Rate II (blocked by 2 errors)
  Likely tier (amex): no enhanced-data rate available
  Likely tier (discover): no enhanced-data rate available

1 transaction, 2 errors, 8 warnings, 2 information. Rule packs: AMEX 1.0.0, DISC 1.0.0, GEN 1.0.0, MC 1.0.0, VISA 1.0.0.
```

## Choose which rules run

- `--only VISA-L3-*` runs only the matching rules. Repeat it, or give several values.
- `--disable GEN-DQ-008` skips rules. It adds to the config file's list.
- `--severity GEN-DQ-007=error` changes one rule's severity. Repeat it for more rules.

IDs and globs are case-insensitive; `*` matches any text and `?` one character.

The likely tier only counts the rules that ran. With `--only`, a tier can read "meets the documented requirement" because the rules that would block it did not run.

## Decide what fails the run

`--fail-on` sets the lowest severity that makes the exit code 1. The default is `error`.

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json --fail-on warning --quiet
1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ echo $?
1
```

`--fail-on none` never fails on findings. Input and usage errors still exit 2, and rule pack, config and internal errors exit 3. See [Output formats and exit codes](output-formats.md#exit-codes).

## Choose outputs

`--output` picks a format: `text` (the default), `json`, `sarif`, `junit` or `markdown`. Add `=path` to write a file instead of standard output. Repeat it to write several at once:

```text
l3lint lint samples/action-example/fixtures/payments/INV-20418.json --output json --output sarif=out/l3lint.sarif --output junit=out/l3lint.junit.xml --output markdown=out/summary.md
```

`--max-findings 50` prints at most 50 findings; the summary still counts all of them. `--annotations github` also prints GitHub workflow annotations for findings at or above `--fail-on`:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20418.json --annotations github --quiet
1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
::error file=samples/action-example/fixtures/payments/INV-20418.json,line=59,col=5,title=VISA-L3-004::Line 2 has no commodity code; Visa needs one on every line for Product 3. Fix: Add the most specific 8-digit UNSPSC code for this item. (/lineItems/1/commodityCode)
```

## Quieter or louder

- `--quiet` prints only the summary line for each run.
- `--verbose` adds notes: which config file was used, adapter remarks, derived values and ignored source fields. Internal errors then include the stack trace.
- `--no-color` turns color off. Setting the `NO_COLOR` environment variable does too. Color is only used on a terminal, and severity words always print, so color is never the only signal.
- `--log-level` and `--log-format json` control logs, which always go to standard error. Standard output carries only results.

```text
$ l3lint lint fixtures/adapters/stripe/01-complete-level3.json --brand visa --card-product purchasing --mcc 5047 --verbose --only VISA-L3-009
Transaction 1  stripe  visa purchasing  MCC 5047  as of 2026-10-30
  Note: Stripe sends no line totals, so each line total was computed as quantity times unit cost minus the line discount.
  Note: No order-level tax amount was sent, so the tax amount is the sum of the line tax amounts.
  Note: Stripe has no tax indicator field; the sale was marked taxable because the tax amount is above 0.
  Note: /automatic_payment_methods/enabled has no canonical field and was ignored.
  Note: /expand/0 has no canonical field and was ignored.
  error       VISA-L3-009        /amount_details/line_items/1/discount_amount
           Line 2 has no discount amount; Visa needs one, 0 when there is no discount.
           Fix: Send 0 when there is no discount.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 1 error)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

## Explore the rules

- `l3lint rules list` prints the packs and every rule. Filter with `--brand`, `--level` and `--query`, or use `--output json`.
- `l3lint rules show VISA-L3-004` and `l3lint explain VISA-L3-004` print a rule's reference page. `explain --output json` prints it as JSON.
- `l3lint rules test` runs every rule's passing and failing example:

```text
$ l3lint rules test --quiet
78 rule(s) tested, 78 passed, 0 failed.
```

## Convert a gateway request to canonical JSON

`convert` shows what the adapter read: the canonical payload, the path map back to your format, the values it derived and its notes.

```text
$ l3lint convert fixtures/adapters/stripe/01-complete-level3.json --to canonical
{
  "format": "stripe",
  "cardNumbersMasked": 0,
  "transactions": [
    {
      "index": 0,
      "canonical": {
        "schemaVersion": "1.0",
        "amount": 280.36,
        "currency": "USD",
```

The output is shortened here. After `canonical` come `pathMap` (for example `"/lineItems/0/unitCost": "/amount_details/line_items/0/unit_cost"`), `derivedFields` and `notes`.

`--canonical-only` prints only the canonical payloads, one per line, ready for `lint --format canonical`:

```text
$ l3lint convert fixtures/adapters/csv/01-erp-two-invoices.csv --map fixtures/adapters/csv/01-erp-two-invoices.map.yaml --to canonical --canonical-only > invoices.ndjson
```

## Other commands

- `l3lint fix`: mechanical fixes as a JSON Patch, or a patched copy. See [Fixes](fixes.md).
- `l3lint schema canonical`: prints a JSON Schema. The names are `canonical`, `results`, `config`, `rulepack`, `csvmap`, `rates`, `profile` and `lock`.
- `l3lint init`: writes `.l3lint.yaml`, `.l3lint.lock`, a sample payload and a sample workflow. See [Getting started](getting-started.md#6-set-up-a-project).
- `l3lint version`: the engine and rule pack versions.
