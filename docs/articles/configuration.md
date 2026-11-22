# Configuration

A `.l3lint.yaml` file holds team defaults, so CI and every laptop lint the same way. A `.l3lint.lock` file pins the rule pack versions, so results do not change when the tool updates its packs.

`l3lint init` writes both. The [configuration file reference](../reference/config.md) lists every key with its type, default and constraints; it is generated from `config.schema.json`.

## How l3lint finds the file

1. `--config <path>` names the file. If it does not exist, the run stops with exit code 2.
2. Otherwise l3lint looks for `.l3lint.yaml` in the current folder, then in each parent folder up to the root, and uses the first one it finds.
3. With no file, the built-in defaults apply.

`--verbose` prints which file was used:

```text
$ l3lint lint INV-20417.json --verbose
Note: Using configuration C:\...\cfg\.l3lint.yaml.
```

The path is shortened here; l3lint prints the full path of the file it found. The run was on Windows, in a folder named `cfg`.

Relative paths inside the file (`inputs.csvMap`, `rates`, a `context.processor` file, `output.formats` targets) resolve from the folder that holds the file, not from the current folder.

## Precedence

For each setting, the first of these that has a value wins:

1. A command-line flag, for example `--brand visa`.
2. The payload, for example `card.brand`.
3. `.l3lint.yaml`.
4. The built-in default.

The payload only carries context values (brand, card product, MCC, merchant country, transaction date). For other settings, flags beat the config file. A few lists combine instead of replacing:

| Setting | Flag and config together |
| --- | --- |
| `rules.disable` and `--disable` | Both lists apply. |
| `rules.severity` and `--severity` | Both apply; a flag wins for the same rule. |
| `rules.only` and `--only` | The flag replaces the config list. |
| `output.formats` and `--output` | Any `--output` replaces the whole config list. |
| `pci.strict` and `--strict-pci`, `context.strictProfiles` and `--strict-profiles` | Either one turns the setting on. |

When a flag changes a value that the payload sent, the text output prints a note such as `Card brand set to mastercard by option; the payload said visa.`

## An example

This file makes the Visa detail indicator rule informational and fails the run on warnings:

```yaml
version: 1
rules:
  severity:
    VISA-L3-010: information
output:
  failOn: warning
```

With that file in the current folder:

```text
$ l3lint lint INV-20417.json
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  information VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: Product 3 (meets the documented requirement)

1 transaction, 0 errors, 0 warnings, 1 information. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

## Every key

| Key | Default | Meaning |
| --- | --- | --- |
| `version` | `1` | Config schema version. |
| `context.brand` | `auto` | Brand when the payload has none: `visa`, `mastercard`, `amex`, `discover`, or `all` to lint against every brand. |
| `context.cardProduct` | none | Card product when the payload has none. |
| `context.mcc` | none | Merchant category code when the payload has none. |
| `context.merchantCountry` | `US` | Merchant country when the payload has none. |
| `context.processor` | `generic` | A [processor profile](processor-profiles.md) name, or the path of a profile file. |
| `context.strictProfiles` | `false` | Run single-processor rules at their catalog severity under every profile. |
| `inputs.format` | `auto` | Input format; `auto` detects it. |
| `inputs.defaultCurrency` | none | Currency for payloads that carry none, such as Braintree requests. |
| `inputs.csvMap` | none | Column map for CSV input. See [CSV input](csv-input.md). |
| `inputs.limits.maxFileBytes` | `26214400` (25 MB) | Largest input. For NDJSON the limit applies to each record. |
| `inputs.limits.maxLineItems` | `1000` | Most line items in one transaction. |
| `inputs.limits.maxJsonDepth` | `64` | Deepest JSON nesting. |
| `rules.only` | all rules | Run only these rule IDs or globs. |
| `rules.disable` | none | Rule IDs or globs to skip. |
| `rules.severity` | pack defaults | Severity per rule ID or glob. |
| `rules.placeholders.add`, `.remove` | none | Edit the placeholder description list used by GEN-DQ-001 and VISA-DQ-001. |
| `rules.batch.minTransactions` | `20` | Fewest transactions before the batch rule GEN-DQ-007 runs. |
| `rules.batch.sameValueShare` | `0.8` | Share of transactions with the same description or product code that fires GEN-DQ-007. |
| `arithmetic.lineTolerance` | 1 minor unit | Accepted difference for line totals, in major units. |
| `arithmetic.totalTolerance` | 1 minor unit | Accepted difference for the transaction total and header tax. |
| `arithmetic.toleranceMode` | `strict` | `accumulated` allows 1 minor unit per line, capped at 5. |
| `arithmetic.rounding` | `awayFromZero` | `toEven` for ERPs that use banker's rounding. |
| `pci.strict` | `false` | Stop instead of masking when a card number is found. |
| `pci.allowFields` | none | Canonical JSON Pointers exempt from card-number detection, after a person checks them. |
| `output.failOn` | `error` | Lowest severity that sets exit code 1. |
| `output.formats` | `[text]` | Outputs, each `kind` or `kind=path`. |
| `output.maxFindings` | no limit | Most findings to print. |
| `rates` | none | Rate table for [cost estimates](cost-estimates.md). |

`inputs.defaultCurrency` and `rules.only` are additions to the spec's configuration table (ADR-0056 adds the default currency).

Visa does not publish its rounding tolerance, so 1 minor unit is l3lint's own choice. See [Limitations](limitations.md).

## Unknown keys are errors

A typo would otherwise change behavior without a word, so any key the schema does not know stops the run with exit code 3:

```text
$ l3lint lint INV-20417.json --config bad.yaml
error: C:\...\cfg\bad.yaml /rules/disabel: C:\...\cfg\bad.yaml is invalid at /rules/disabel: /rules/disabel is not a known key. Check the spelling.
$ echo $?
3
```

The paths are shortened here. `bad.yaml` held `rules: { disabel: [GEN-DQ-008] }`.

## The lock file

`.l3lint.lock` records the ID and version of each rule pack:

```yaml
# Pins l3lint rule pack versions. A mismatch with the loaded packs stops the run with exit code 3.
version: 1
packs:
  l3lint.rules.amex: 1.0.0
  l3lint.rules.discover: 1.0.0
  l3lint.rules.general: 1.0.0
  l3lint.rules.mastercard: 1.0.0
  l3lint.rules.visa: 1.0.0
```

l3lint looks for it in the config file's folder, or in the current folder when there is no config file. When a loaded pack has a different version, the run stops:

```text
$ l3lint lint INV-20417.json
error: The lock file requires l3lint.rules.visa 0.9.0, but 1.0.0 is loaded. Update .l3lint.lock or install the locked pack version.
Check the rule pack with l3lint rules test --rule-pack <path>.
$ echo $?
3
```

After you upgrade l3lint on purpose, review the rule changes in the changelog (`CHANGELOG.md` in the repository), then update the versions in the lock file.

## Environment variables

| Variable | Effect |
| --- | --- |
| `NO_COLOR` | Any value turns off color, like `--no-color`. |

The MCP server also reads `L3LINT_MCP_TOKEN`; see [MCP server](mcp-server.md).
