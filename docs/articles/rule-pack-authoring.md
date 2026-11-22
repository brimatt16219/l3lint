# Writing rule packs

A rule pack is a folder of data: one YAML file of rules, optional CSV lists, a passing and a failing example per rule, and a manifest of checksums. Packs never contain code, so loading a pack cannot run anything. This guide is for people who maintain the shipped packs and for teams that add their own rules with `--rule-pack`.

## Rules come from public sources

Every rule needs at least one public source, and the most specific page available: a brand page first, then a processor or gateway page, then a published guide. When sources disagree, record all of them, encode the stricter reading and explain the disagreement in `notes`. Never cite or paraphrase material that is not public, such as acquirer-only specifications. Check every source again each April and October, when brands publish changes, and bump the pack version.

## The folder

```text
po-pack/
  po.rules.yaml          the pack: header and rules (exactly one *.rules.yaml)
  lists/                 optional CSV lists that rules name
  examples/              GEN-PO-001.pass.json, GEN-PO-001.fail.json
  manifest.json          SHA-256 of every other file, written by tools/L3Lint.PackManifest
```

## A complete example

`po.rules.yaml`:

```yaml
pack:
  id: example.rules.po
  version: 1.0.0
  brand: general
  publishedOn: 2026-10-30
  title: Purchase order format
  description: One team rule that checks the shape of the purchase order number.

rules:
  - id: GEN-PO-001
    title: Purchase order number is not in the PO-number form
    requirement: The purchase order number must look like PO- followed by digits.
    rationale: The team's ERP matches payments to orders by this form.
    level: L2
    severity: warning
    confidence: policy
    check: { kind: pattern, field: /order/purchaseOrderNumber, regex: "^PO-[0-9]+$" }
    message: "{field} is {actual}; it should look like PO-12345."
    fix: Send the purchase order number as PO- followed by digits.
    impact: hygiene
    sources:
      - https://brimatt16219.github.io/l3lint/articles/rule-pack-authoring.html
    examples:
      pass: examples/GEN-PO-001.pass.json
      fail: examples/GEN-PO-001.fail.json
      passNote: The purchase order number is PO-77812.
      failNote: The purchase order number is 77812, without the PO- prefix.
```

`examples/GEN-PO-001.pass.json`:

```json
{
  "schemaVersion": "1.0",
  "amount": 10.00,
  "currency": "USD",
  "order": { "purchaseOrderNumber": "PO-77812" }
}
```

`examples/GEN-PO-001.fail.json` is the same with `"purchaseOrderNumber": "77812"`.

Write the manifest, then test the pack:

```text
$ dotnet run --project tools/L3Lint.PackManifest -- po-pack
written: po-pack\manifest.json
$ l3lint rules test --rule-pack po-pack --only "GEN-PO-*"
pass  GEN-PO-001
1 rule(s) tested, 1 passed, 0 failed.
```

(This run was on Windows, so the manifest tool prints the path with a backslash.)

Use it like any other pack:

```text
$ sed 's/"purchaseOrderNumber": "PO-77812"/"purchaseOrderNumber": "77812"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --rule-pack po-pack --only GEN-PO-001
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  warning     GEN-PO-001         /order/purchaseOrderNumber
           /order/purchaseOrderNumber is 77812; it should look like PO-12345.
           Fix: Send the purchase order number as PO- followed by digits.
           Source: https://brimatt16219.github.io/l3lint/articles/rule-pack-authoring.html  (policy)
  Likely tier: Product 3 (meets the documented requirement)

1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

`l3lint explain GEN-PO-001 --rule-pack po-pack` prints the rule's page, and `l3lint version --rule-pack po-pack` lists the pack with its folder.

## The pack header

| Key | Required | Meaning |
| --- | --- | --- |
| `id` | yes | Lowercase, dotted, for example `example.rules.po`. A local pack with the same ID as a shipped pack replaces it. |
| `version` | yes | Semantic version. Major for removed rules or changed meaning, minor for new rules, patch for wording and source fixes. |
| `brand` | yes | `general`, `visa`, `mastercard`, `amex` or `discover`. |
| `publishedOn` | yes | `YYYY-MM-DD`. |
| `title`, `description` | no | Shown in listings. |
| `lists` | no | Named CSV lists: `name: lists/file.csv`, or `name: { path: lists/file.csv, status: partial }` for a list that public sources do not confirm in full. |

Top-level keys that start with `x-` are ignored. The shipped packs use them to hold YAML anchors for applicability gates that many rules share.

## Rule fields

| Field | Required | Meaning |
| --- | --- | --- |
| `id` | yes | `<SCOPE>-<AREA>-<nnn>`. Scope is `GEN`, `VISA`, `MC`, `AMEX` or `DISC`; area is letters and digits, such as `L3` or `DQ`. |
| `title` | yes | A short name. |
| `requirement` | no | The requirement in one sentence. Defaults to the title. |
| `rationale` | no | Why the rule exists. |
| `notes` | no | Source disagreements and other caveats. |
| `level` | yes | `general`, `L2` or `L3`. |
| `severity` | yes | `error`, `warning` or `information`. Heuristics should be warnings. |
| `confidence` | yes | `public-brand`, `processor-doc`, `inferred` or `policy`. See [confidence labels](rules.md#confidence-labels). |
| `effective` | no | `{ from: YYYY-MM-DD, until: YYYY-MM-DD }`. `until` is the last day the rule applies. |
| `appliesTo` | no | Gates; see below. |
| `profiles` | no | Processor profiles under which the catalog severity applies; elsewhere it is capped at warning. |
| `supersedes` | no | Rule IDs whose findings at the same path this rule replaces. |
| `check` | yes | What to check; see below. |
| `message` | yes | The finding text, with placeholders. |
| `messages` | no | Named alternative messages that custom checks choose from. |
| `fix` | yes | The fix, stated as an action. |
| `impact` | yes | `blocksLevel3`, `blocksLevel2`, `riskVisaValidation`, `truncation`, `noBenefit` or `hygiene`. See [Likely tiers](likely-tiers.md). |
| `sources` | yes | One or more `https://` URLs. |
| `examples` | for shipped packs | `pass` and `fail` file paths, optional `passNote`, `failNote` and `context` (`asOf`, `processor`, `strictProfiles`, `rates`). |

Write messages and fixes in plain words, keep sentences under 25 words, and state the fix as an action.

## Check kinds

Fields are JSON Pointers into the [canonical payload](../reference/canonical.md). `*` stands for every line: `/lineItems/*/total`.

| Kind | Parameters | Fires when | Used by |
| --- | --- | --- | --- |
| `required` | `field` or `fields`; `allowZero` (default false); `positiveOnly`; `report` (`each` or `once`) | A value is absent, null, blank, an empty list, an "unknown" enum value, or 0 without `allowZero` | VISA-L3-004 |
| `maxLength` | `field` with `max` and/or `profileKey`, or `limits` (a list of `field`, `max`, `profileKey`, `brands`) | A text is longer than the limit; a profile's value replaces `max` | GEN-FMT-006 |
| `pattern` | `field` or `fields`; `regex` | A present value does not match the regular expression (100 ms timeout) | GEN-FMT-004 |
| `range` | `field`; `min` and/or `max`; `exclusive` | A number is outside the range | none of the shipped rules |
| `inList` | `field`; `list` (one or more list names) or `values`; `caseInsensitive`; `mode` (`allow` or `deny`); `allowMissing` | With `allow`, a value is not on the list; with `deny`, it is | MC-L3-005 |
| `ratio` | `numerator`, `denominator`, `min`, `max` | numerator / denominator is outside the range | MC-L2-001 |
| `maxCount` | `collection`; `max` and/or `profileKey` | A list has more entries than allowed | GEN-FMT-005 |
| `anyOf` | `fields` (two or more) | None of the fields has a value | MC-L2-002 |
| `custom` | `check`: the name of a compiled check | Whatever the check decides | GEN-ARITH-001 |

A `custom` check must be compiled into l3lint. The CLI never loads plugin code, so a pack that names an unknown check stops with exit code 3. The checks built into l3lint 1.0.0 are AdviceText, AmountDigits, AmountPrecision, CardNumber, CommodityCodeShape, Currency, DoubleDiscount, DuplicateLines, ExplicitTax, FillerReference, LargeTicket, LineTotal, MastercardLineIndicators, MerchantNameDescription, NegativeDebitLine, NoRateProgram, OneLineInvoice, PlaceholderCode, PlaceholderDescription, Schema, Sequence, StaticBatchData, TaxConsistency, TransactionTotal, VisaCodeQuality and VisaDescriptionQuality. In a .NET service you can register your own; see [.NET library](library.md#custom-checks-for-rule-packs).

## Message placeholders

A message may use these placeholders; a pack that uses another one fails to load: `{index}` (1-based line number), `{field}`, `{actual}`, `{expected}`, `{count}`, `{max}`, `{min}`, `{length}`, `{ratio}`, `{total}`, `{detail}`, `{brand}`, `{difference}`, `{reported}`, `{list}`, `{share}`, `{transactions}`, `{first}`, `{masked}`, `{lines}` and `{currency}`. Which ones have a value depends on the check kind: `maxLength` fills `{max}` and `{length}`, `ratio` fills `{ratio}`, `{min}` and `{max}`, `maxCount` fills `{count}` and `{max}`, and every check fills `{field}`.

## Applicability gates

`appliesTo` decides whether a rule runs for a transaction, before its check:

```yaml
appliesTo:
  cardProducts: [business, corporate, purchasing, fleet]
  mccNotIn: visaProduct3ExcludedMcc
  unless:
    - { field: /card/isFederalGovernment, equals: true }
    - { field: /merchant/mcc, inList: visaFederalExcludedMcc }
```

| Key | Meaning |
| --- | --- |
| `cardProducts` | The rule runs only for these card products. An unknown product does not skip the rule, so data problems still show. |
| `mccIn`, `mccNotIn` | One or more list names; the rule runs only when the MCC is (or is not) on them. |
| `when` | Conditions that must all hold. |
| `unless` | Conditions that, all together, skip the rule. |

A condition names a `field` and exactly one test: `equals`, `notEquals`, `in`, `notIn`, `inList`, `present`, `greaterThan` or `lessThan`.

## Lists

A list is a CSV file. Lines that start with `#` are comments; put the source URL and the retrieval date there. The first line after the comments is a header, and the first column holds the values. In MCC lists a value such as `3000-3999` stands for every code in the range.

```text
# MCCs excluded from Visa Commercial Product 3 (CEDP), per Stripe's public payment line items guide.
# source: https://docs.stripe.com/payments/payment-line-items
# retrieved: 2026-10-25
mcc,note
3000-3999,Airlines, car rental and lodging chains
4000-4010,Railroads
```

## Examples

Each example is a canonical payload. `l3lint rules test` checks that the passing example raises no finding for the rule and the failing example raises at least one. Keep examples small and synthetic: no real customer, merchant or employer data, and card numbers only from public test ranges. Set `examples.context` when the example needs an as-of date, a processor profile, `--strict-profiles` or a rate table.

## Manifest and integrity

Every pack folder needs `manifest.json` with the SHA-256 of each file. Line endings are normalized to LF before hashing, so checkouts on any operating system agree. When the loader finds a missing manifest or a changed file, it refuses the pack and the run exits 3:

```text
$ l3lint rules test --rule-pack po-pack
error: po.rules.yaml does not match its SHA-256 in manifest.json. Regenerate the manifest with L3Lint.PackManifest.
Check the rule pack with l3lint rules test --rule-pack <path>.
```

Rewrite the manifest after every change with `dotnet run --project tools/L3Lint.PackManifest -- <folder>`. With no folder it rewrites the five shipped packs; `--check` only reports manifests that are out of date.

The loader also validates the pack against [rulepack.schema.json](../schemas/rulepack.schema.json) (`l3lint schema rulepack`) and compiles every regular expression and placeholder before the first lint.

## Lock files

A local pack has its own ID and version, and `.l3lint.lock` pins them like the shipped packs. See [Configuration](configuration.md#the-lock-file).

## Changing a shipped pack

1. Edit the pack under `src/L3Lint.Rules.<Brand>/Pack/`, with sources.
2. Add or update the examples. Shipped examples are generated from compact definitions in `tools/L3Lint.FixtureGen` (ADR-0040); run `dotnet run --project tools/L3Lint.FixtureGen -- examples`.
3. Bump the pack version and rewrite the manifest.
4. Run `l3lint rules test` and the test suites.
5. Run `dotnet run --project tools/L3Lint.DocsGen -- rules` so the rule pages match. `CONTRIBUTING.md` in the repository has the full checklist.
