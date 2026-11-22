# Fixes

`l3lint fix` repairs the problems that have exactly one right answer, and lists the rest for a person. It works on the same inputs as `lint` and takes the same context options (`--brand`, `--card-product`, `--processor`, `--only` and so on).

## Mechanical fixes only

A fix is applied only when the payload itself proves the right value:

- Recompute a line total from quantity, unit cost and line discount.
- Map a free-text unit to the brand's code list when the mapping is unambiguous, for example `each` to `EAC` for Mastercard.
- Trim a field that is longer than the brand or processor allows.
- Set an explicit indicator that follows from other data: the tax indicator `taxable` when the tax is above 0, the debit indicator, the discount indicator and the line taxable flag.
- Renumber line sequences 1, 2, 3.
- Round amounts that carry more decimals than the currency allows.
- Set a missing summary or line discount, freight or duty to 0, only when the arithmetic proves it is 0.
- Recompute the transaction amount, only with `--fix-amount`, because that changes what the card is charged.

l3lint never invents data that needs business knowledge. It does not write item descriptions, choose commodity codes or work out tax amounts. When a unit mapping is uncertain (a box `BX` or a case `CA` has no exact Mastercard equivalent), it is listed for a person and never applied.

## Dry run: print the patch

The default (`--dry-run`) prints an RFC 6902 JSON Patch on standard output and writes nothing. Findings a person must decide go to standard error.

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

The patch fixes line 1. GEN-ARITH-002 stays on the list because the amount check reads the reported line totals; once the patch is applied, the totals add up again.

Patches use the source format's paths and units. For a Stripe request the path is a Stripe JSON Pointer and amounts are integers in minor units; for Braintree, Cybersource and Authorize.net they are decimal strings. A canonical value the format cannot express produces no patch.

## Write a patched copy

`--write <path>` applies the patch to a copy. It never overwrites the input.

```text
$ l3lint fix INV-20417-bad.json --write INV-20417-fixed.json
Applied 1 patch operation(s) and wrote INV-20417-fixed.json.
2 finding(s) need a person to decide:
  error  GEN-ARITH-002  /amount  The amount is 280.36; lines and summary amounts add up to 285.36 (difference -5.00). A line total is likely wrong; fix that first.
      Fix: Recompute the amount, or fix the line or summary values.
  warning  VISA-L3-010  /lineItems/0/detailIndicator  2 line(s) have no line-item detail indicator, starting with line 1.
      Fix: Confirm your processor sets it.
$ l3lint lint INV-20417-fixed.json --quiet
1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ l3lint fix INV-20417-bad.json --write INV-20417-bad.json
error: --write must name a new file, not the input. The fix writes a patched copy.
Run l3lint --help for usage.
```

The copy is built from the masked document, so a card number in the input never reaches the output file. `--dry-run` and `--write` together are a usage error.

## Unit codes

An unambiguous unit word is mapped; a packaging unit is left for a person:

```text
$ sed 's/"unitOfMeasure": "BX"/"unitOfMeasure": "each"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint fix - --brand mastercard --only MC-L3-005
[
  {
    "op": "add",
    "path": "/lineItems/0/unitOfMeasure",
    "value": "EAC"
  }
]
1 finding(s) need a person to decide:
  error  MC-L3-005  /lineItems/1/unitOfMeasure  Line 2 unit of measure CA is not on the Mastercard list.
      Fix: Map to a listed code such as EAC or PCE.
```

## The transaction amount

When only the amount is wrong, the patch is empty unless you ask for the amount:

```text
$ sed 's/"amount": 280.36/"amount": 290.36/' samples/action-example/fixtures/payments/INV-20417.json | l3lint fix -
[]
2 finding(s) need a person to decide:
  error  GEN-ARITH-002  /amount  The amount is 290.36; lines and summary amounts add up to 280.36 (difference 10.00).
      Fix: Recompute the amount, or fix the line or summary values.
  warning  VISA-L3-010  /lineItems/0/detailIndicator  2 line(s) have no line-item detail indicator, starting with line 1.
      Fix: Confirm your processor sets it.
$ sed 's/"amount": 280.36/"amount": 290.36/' samples/action-example/fixtures/payments/INV-20417.json | l3lint fix - --fix-amount --quiet
[
  {
    "op": "add",
    "path": "/amount",
    "value": 280.36
  }
]
```

Use `--fix-amount` only when you know the line items are right and the charged amount is wrong.

## NDJSON and CSV

| Input | Dry run | `--write` |
| --- | --- | --- |
| JSON file | One indented patch | The patched document, indented |
| NDJSON file | One compact patch per input line, in order, so line N of the output patches record N | Patched NDJSON, one record per line |
| CSV file | One compact patch per transaction, against the canonical form | Canonical NDJSON, one transaction per line |

CSV cells have no JSON Pointer, so CSV patches target the canonical form and `--write` writes canonical NDJSON, not a fixed CSV.

```text
$ l3lint fix fixtures/adapters/csv/01-erp-two-invoices.csv --map fixtures/adapters/csv/01-erp-two-invoices.map.yaml --quiet
[{"op":"add","path":"/amounts/discount","value":0.00},{"op":"add","path":"/amounts/duty","value":0.00}]
[{"op":"add","path":"/amounts/discount","value":0.00},{"op":"add","path":"/amounts/duty","value":0.00}]
```

`--quiet` hides the list of findings that need a person.

## Exit codes

`fix` exits 0 when it produced a plan, even if findings remain. With `--strict-pci` it stops with exit code 1 when the input holds a card number. Usage and input errors exit 2; config and rule pack errors exit 3.

## From an AI agent

The MCP tool `suggest_fixes` returns the same patch and the same list of findings that need a person. See [MCP server](mcp-server.md).
