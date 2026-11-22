# Output formats and exit codes

Five formats render the same results. JSON is the contract for programs, SARIF is the contract for GitHub code scanning, text and Markdown are for people, and JUnit XML is for CI dashboards.

Pick formats with `--output`, or `output.formats` in the config file. Add `=path` to write a file. Repeat the option to write several formats in one run:

```text
l3lint lint samples/action-example/fixtures/payments/INV-20418.json --output json --output sarif=out/l3lint.sarif --output junit=out/l3lint.junit.xml --output markdown=out/summary.md
```

Results go to standard output. Errors and logs go to standard error, so a pipe receives only results. Every format writes LF line endings, sorts findings by transaction, path and rule ID, and contains no time stamps. The same input, options and rule pack versions give byte-identical JSON and SARIF. A payload without a `transactionDate` is checked against today's rules unless you pass `--as-of`, so pass it when you compare runs from different days.

The samples below come from that command. `INV-20418.json` has one error and one warning.

## Text

The default. One block per transaction, findings sorted by path and rule ID, then a summary line. Severity words always print, so color is never the only signal. Color appears only on a terminal, and never with `--no-color` or the `NO_COLOR` environment variable.

```text
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
```

`--quiet` keeps only the summary line. `--verbose` adds notes from the adapter and the settings. A transaction without an ID is named `Transaction N`, where N counts from 1.

## JSON

JSON output validates against `results.schema.json`. The [JSON results reference](../reference/results.md) documents every property. An excerpt:

```json
{
  "schemaVersion": "1.0",
  "tool": {
    "name": "l3lint",
    "version": "1.0.0",
    "informationUri": "https://github.com/brimatt16219/l3lint"
  },
  "rulePacks": {
    "l3lint.rules.amex": "1.0.0",
    "l3lint.rules.discover": "1.0.0",
    "l3lint.rules.general": "1.0.0",
    "l3lint.rules.mastercard": "1.0.0",
    "l3lint.rules.visa": "1.0.0"
  },
  "asOf": null,
  "profile": "generic",
  "transactions": [
    {
      "index": 0,
      "input": 0,
      "id": "INV-20418",
      "brand": "visa",
      "brands": [
        "visa"
      ],
      "cardProduct": "purchasing",
      "mcc": "5047",
      "format": "canonical",
      "asOf": "2026-10-19",
      "likelyTier": {
        "best": "product3",
        "likely": "belowProduct3",
        "status": "blocked",
        "blockingErrors": 1
      },
      "findings": [
        {
          "ruleId": "VISA-L3-004",
          "severity": "error",
          "impact": "blocksLevel3",
          "brand": "visa",
          "path": {
            "canonical": "/lineItems/1/commodityCode",
            "source": "/lineItems/1/commodityCode"
          },
          "location": {
            "line": 59,
            "column": 5
          },
          "message": "Line 2 has no commodity code; Visa needs one on every line for Product 3.",
          "expected": null,
          "actual": null,
          "fix": "Add the most specific 8-digit UNSPSC code for this item.",
          "patch": null,
          "confidence": "processor-doc",
          "sources": [
            "https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program"
          ]
        }
      ]
    }
  ],
  "summary": {
    "transactions": 1,
    "errors": 1,
    "warnings": 1,
    "information": 0,
    "byRule": {
      "VISA-L3-004": 1,
      "VISA-L3-010": 1
    },
    "byBrand": {
      "visa": {
        "errors": 1,
        "warnings": 1,
        "information": 0
      }
    }
  },
  "exitCode": 1
}
```

The excerpt leaves out the `inputs` array and the VISA-L3-010 finding. Each finding carries two paths: `canonical` points into l3lint's canonical payload and `source` into the file you gave it. For a Stripe request the source path is a Stripe JSON Pointer; for CSV it is a cell label such as `row 2, column P (qty)`.

`summary` counts every finding, even when `--max-findings` leaves some out of the output; `findingsShown` and `findingsOmitted` then say how many. Findings about the whole run, such as GEN-DQ-007, go in `batchFindings`. Inputs that could not be read go in `inputErrors`.

## SARIF

SARIF 2.1.0 for GitHub code scanning. The writer stays within GitHub's limits: at most 5,000 results per run. When there are more findings it keeps errors first, then warnings, then information, and the last result is a note with the reserved ID `L3LINT-SARIF-001` that says how many were left out.

| SARIF property | Value |
| --- | --- |
| `runs[0].tool.driver.name`, `.version`, `.informationUri` | `l3lint`, the engine version, the project URL |
| `runs[0].tool.driver.rules[]` | One entry per rule that fired: ID, title, requirement, help text with the fix, `helpUri` (the rule's page on this site), default level and tags (brand, level, confidence) |
| `results[].ruleId`, `.level`, `.message.text` | Rule ID; `error`, `warning` or `note`; the message plus "Fix:" |
| `results[].locations[0].physicalLocation` | The input path relative to the current folder, with the line and column of the value |
| `results[].locations[0].logicalLocations[0].fullyQualifiedName` | The source JSON Pointer |
| `results[].partialFingerprints["l3lint/v1"]` | SHA-256 of the rule ID, the source pointer and the transaction ID, so alerts survive edits that move lines |
| `results[].properties` | Transaction index and ID, canonical path, expected and actual values, confidence, sources and impact |
| `runs[0].automationDetails.id` | `l3lint/` plus a hash of the config file (`default` without one). GitHub reads `l3lint` as the category. |

A result from the sample run:

```json
{
  "ruleId": "VISA-L3-004",
  "level": "error",
  "message": {
    "text": "Line 2 has no commodity code; Visa needs one on every line for Product 3. Fix: Add the most specific 8-digit UNSPSC code for this item."
  },
  "locations": [
    {
      "physicalLocation": {
        "artifactLocation": {
          "uri": "samples/action-example/fixtures/payments/INV-20418.json",
          "uriBaseId": "%SRCROOT%"
        },
        "region": {
          "startLine": 59,
          "startColumn": 5
        }
      },
      "logicalLocations": [
        {
          "fullyQualifiedName": "/lineItems/1/commodityCode"
        }
      ]
    }
  ]
}
```

The excerpt leaves out `ruleIndex`, `partialFingerprints` and `properties`. Input from standard input has no file, so its results have no physical location.

## JUnit XML

One `testsuite` per input file and one `testcase` per transaction, named by the transaction ID. Every error finding becomes a `failure`, whatever `--fail-on` says. Warnings, information and the likely tier go to `system-out`. Findings about the whole run go in a last suite named "Batch findings", and an input that could not be read becomes an `error` element.

```xml
<?xml version="1.0" encoding="utf-8"?>
<testsuites name="l3lint" tests="1" failures="1" errors="0">
  <testsuite name="samples/action-example/fixtures/payments/INV-20418.json" tests="1" failures="1" errors="0">
    <testcase name="INV-20418">
      <failure message="VISA-L3-004: Line 2 has no commodity code; Visa needs one on every line for Product 3." type="VISA-L3-004">Path: /lineItems/1/commodityCode
Location: line 59, column 5
Fix: Add the most specific 8-digit UNSPSC code for this item.
Rule: https://brimatt16219.github.io/l3lint/rules/VISA-L3-004.html</failure>
      <system-out>Likely tier: below Product 3 (blocked by 1 error)
warning VISA-L3-010 /lineItems/0/detailIndicator: 2 line(s) have no line-item detail indicator, starting with line 1.
  Fix: Confirm your processor sets it.</system-out>
    </testcase>
  </testsuite>
</testsuites>
```

The XML has no time stamps or durations.

## Markdown

A summary for people, made for the GitHub job summary: a table of counts by brand, then one collapsible section per transaction with findings. Rule IDs link to their pages on this site. Text from the payload is escaped, so it cannot inject links, images or HTML. The page stops before 1,000,000 bytes and ends with a note that points to the JSON output.

```markdown
## l3lint results

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.

| Brand | Transactions | Errors | Warnings | Information |
| --- | ---: | ---: | ---: | ---: |
| visa | 1 | 1 | 1 | 0 |
| **Total** | 1 | 1 | 1 | 0 |

### samples/action-example/fixtures/payments/INV-20418.json

<details>
<summary><strong>INV-20418</strong>: 1 error, 1 warning</summary>
```

The output is shortened here. Inside each section is a findings table (severity, rule, path, message, fix) and the likely tier.

## Exit codes

| Code | Meaning | Examples |
| --- | --- | --- |
| 0 | No finding at or above `--fail-on` | A clean payload; warnings with the default `--fail-on error` |
| 1 | At least one finding at or above `--fail-on` | A missing commodity code |
| 2 | Usage or input error | An unknown option, a file that does not exist, JSON that is not well-formed, a file over the size limit, a format that cannot be detected |
| 3 | Rule pack, configuration or internal error | An invalid `.l3lint.yaml`, a lock file mismatch, a pack whose checksum does not match |

When several apply, the highest wins: 3, then 2, then 1, then 0. An input error skips only that input; the other inputs are still linted and reported.

```text
$ l3lint lint missing.json
error  missing.json: No file matches missing.json. Check the path or the glob.
0 transactions, 0 errors, 0 warnings, 1 input error. Rule packs: GEN 1.0.0.
$ echo $?
2
```

```text
$ echo '{not json' | l3lint lint -
error  -:1: The input is not well-formed JSON. Fix the syntax at the position shown.
0 transactions, 0 errors, 0 warnings, 1 input error. Rule packs: GEN 1.0.0.
$ echo $?
2
```

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json --fial-on warning
error: Unknown option "--fial-on". Run l3lint <command> --help to see the options. To lint a file whose name starts with a dash, write ./--fial-on.
Run l3lint --help for usage.
$ echo $?
2
```

A problem in the payload is a finding, never an error of the run. A problem with the run (a bad option, an unreadable file, a broken config) is an error, never a finding. That split is what lets CI trust the exit code.

## Engine diagnostics

Two finding IDs come from the engine itself rather than a rule pack. They use the reserved `L3LINT-` prefix and do not count toward the 78 catalog rules.

| ID | Severity | When |
| --- | --- | --- |
| [L3LINT-CTX-001](../rules/L3LINT-CTX-001.md) | information | The card brand is unknown, so only the general rules ran. Pass `--brand` or send `card.brand`. |
| [L3LINT-ENGINE-001](../rules/L3LINT-ENGINE-001.md) | error | A rule failed internally on a transaction. The other rules still ran. Please report it as a bug. |

`L3LINT-SARIF-001` appears only in SARIF output, as the note that counts results left out by the 5,000-result limit.
