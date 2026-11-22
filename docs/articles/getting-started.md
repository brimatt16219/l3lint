# Getting started

This page takes you from nothing to a first finding and its fix. It takes about five minutes.

You need the .NET 10 SDK, or a single-file binary from a release (see [Installation](installation.md)). The examples run from the root of a clone of the l3lint repository, so the sample files exist. Output on this page is real output from l3lint 1.0.0 with rule packs 1.0.0.

## 1. Install the tool

```text
dotnet tool install --global l3lint
```

Check that it runs:

```text
$ l3lint version
l3lint 1.0.0
  l3lint.rules.amex 1.0.0
  l3lint.rules.discover 1.0.0
  l3lint.rules.general 1.0.0
  l3lint.rules.mastercard 1.0.0
  l3lint.rules.visa 1.0.0
```

The five lines under the version are the embedded rule packs: one for rules that apply to every brand, and one per card brand.

## 2. Lint a payload that meets the requirements

`samples/action-example/fixtures/payments/INV-20417.json` is the worked example from the l3lint spec: a Visa purchasing card payload with two invoice lines, in l3lint's own canonical format.

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: Product 3 (meets the documented requirement)

1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ echo $?
0
```

How to read it:

- The first line names the transaction, the input format, the brand, the card product, the MCC and the date whose rules applied.
- Each finding shows its severity, the rule ID, the JSON path, a message, the fix and the public source, with the source's [confidence label](rules.md#confidence-labels).
- `Likely tier` is the best data tier the payload can still reach for Visa. See [Likely tiers](likely-tiers.md).
- The exit code is 0 because no finding is at or above the default `--fail-on` level, `error`.

## 3. Lint a payload with an error

`INV-20418.json` is the same payload with the second line's commodity code removed.

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20418.json
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

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ echo $?
1
```

The error blocks Visa Product 3, so the payload is likely to downgrade. The exit code is 1, which fails a CI job.

## 4. Ask why the rule exists

```text
$ l3lint explain VISA-L3-004
# VISA-L3-004: Commodity code is required for Product 3

| Property | Value |
| --- | --- |
| Severity | error |
| Confidence | processor-doc |
| Level | L3 |
| Brand | visa |
| Impact | blocksLevel3 |
| Effective | from 2025-10-17 |
| Pack | l3lint.rules.visa 1.0.0 |

## Requirement

Each line needs a commodity code, usually an 8-digit UNSPSC code, for Product 3.

## Why

The commodity code is one of the 12 Product 3 fields; it classifies what was bought.
```

The output is shortened here. The full page also shows the check, where the rule applies, the message, the fix, the sources and a passing and a failing example. It is the same page as [VISA-L3-004](../rules/VISA-L3-004.md) on this site.

## 5. Let l3lint fix what it can

Some problems have one right answer, such as a line total that does not match quantity times unit cost. This command breaks line 1's total on the way in and asks for fixes:

```text
$ sed 's/"total": 120.00/"total": 125.00/' samples/action-example/fixtures/payments/INV-20417.json > INV-20417-bad.json
$ l3lint fix INV-20417-bad.json
[
  {
    "op": "add",
    "path": "/lineItems/0/total",
    "value": 120.00
  }
]
2 finding(s) need a person to decide:
  error  GEN-ARITH-002  /amount  The amount is 280.36; lines and summary amounts add up to 285.36 (difference -5.00). A line total is likely wrong; fix that first.
      Fix: Recompute the amount, or fix the line or summary values.
  warning  VISA-L3-010  /lineItems/0/detailIndicator  2 line(s) have no line-item detail indicator, starting with line 1.
      Fix: Confirm your processor sets it.
```

The patch on standard output is an RFC 6902 JSON Patch. The list on standard error is what a person must decide. `l3lint fix INV-20417-bad.json --write INV-20417-fixed.json` writes a patched copy instead. l3lint never invents descriptions, commodity codes or tax amounts. See [Fixes](fixes.md).

## 6. Set up a project

`l3lint init` writes a config file, a lock file, a sample payload and a sample GitHub workflow into the current folder:

```text
$ l3lint init
wrote .l3lint.yaml
wrote .l3lint.lock
wrote samples/l3lint-sample.json
wrote .github/workflows/l3lint.yml
Next: run l3lint lint samples/l3lint-sample.json
```

It never overwrites files without `--force`. See [Configuration](configuration.md) for the config file and [GitHub Action](github-action.md) for the workflow.

## Next steps

- Lint a gateway request (Stripe, Adyen, Braintree, Cybersource, Authorize.net or Checkout.com): [Using the CLI](cli.md) and [Adapters](adapters.md).
- Lint an ERP export: [CSV input](csv-input.md).
- Read results in a program: [Output formats](output-formats.md).
- Browse every rule: [Rule reference](../rules/index.md).
