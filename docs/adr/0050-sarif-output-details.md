# ADR-0050: SARIF output details beyond the mapping table

- Status: Accepted
- Date: 2026-10-26

## Context

The Interfaces spec ("Output formats") gives a SARIF 2.1.0 mapping table and GitHub's limits, but leaves some points open: where logical locations sit, what happens when a payload has no transaction ID or the format has no source pointer, how the 5,000-result cap counts the truncation note, what GitHub's required descriptor fields (`help`) contain, and how inputs from standard input or outside the repository are located. GitHub's SARIF documentation also says how it reads `automationDetails.id`.

## Decision

- **Locations.** `logicalLocations[0].fullyQualifiedName` holds the source JSON Pointer (the canonical pointer when the format has none). The SARIF schema allows `logicalLocations` only inside a `location`, so it sits in `results[].locations[0]`, next to `physicalLocation`. The artifact URI is the input path relative to `ReportWriterOptions.BaseDirectory`, with forward slashes, percent-encoded segments and `uriBaseId` `%SRCROOT%`; `..` segments are kept for files outside the base folder, and an absolute `file:` URI is used only when no relative path exists (another drive). Standard input has no physical location. A finding without a token position uses its transaction's first line as `region.startLine`, so GitHub can still place it. `columnKind` is `utf16CodeUnits`, matching `SourceLocation`.
- **Fingerprint.** `partialFingerprints["l3lint/v1"]` is the lowercase hex SHA-256 of the rule ID, the source pointer (canonical when absent) and the transaction ID, joined by line feeds. When the payload has no ID, the transaction key is the input path plus `#` and the transaction's 1-based position in that input. No other payload value enters the hash.
- **Rule descriptors.** One per rule that has a result in the file, sorted by ID. `name` and `shortDescription` are the title, `fullDescription` the requirement, `help.text` and `help.markdown` the requirement, rationale, fix and a link (GitHub lists `help` as required), `helpUri` the rule reference page, `defaultConfiguration.level` the catalog severity, and `properties.tags` the brand (`general` for all-brand rules), level and confidence. Text is cut to GitHub's limits (255 characters for `name`, 1,024 for descriptions). A rule missing from `LintReport.Rules` gets a descriptor built from its first finding, named by its ID.
- **GitHub limit.** At most 5,000 results in total, the truncation note included. When there are more findings the writer keeps 4,999: errors, then warnings, then information, earlier findings first within a severity, and writes them in report order. The last result is a `note` with the reserved rule ID `L3LINT-SARIF-001` (ADR-0033 namespace) and its own descriptor, located at line 1 of the first input file, saying how many findings were left out. `run.properties.resultsDropped` carries the same count.
- **Extra properties.** Results add `transactionId` and `canonicalPath` to the spec's property list. The run adds `invocations[0]` (`executionSuccessful` is true for exit codes 0 and 1, `exitCode`, and one `toolExecutionNotifications` entry per input error) and `properties` (`rulePacks`, `profile`, `asOf` when set, `findingsOmitted` after `--max-findings`, `resultsDropped`).
- **Automation ID.** `automationDetails.id` is `l3lint/` plus the config hash, as the spec says. GitHub reads the text before the last slash as the category, so the category is `l3lint` and the hash is the run ID; alerts therefore survive configuration edits. Projects that upload several configurations for one commit set the `category` input of the upload action.
- **Schema URI.** `$schema` is the OASIS errata 01 schema URI.

## Alternatives considered

- Hashing the input path and canonical pointer in every fingerprint was rejected because the spec names rule ID, source pointer and transaction ID, and GitHub already keys alerts by file path.
- Keeping 5,000 findings and adding the note as result 5,001 was rejected: GitHub shows only the top 5,000 by severity, so the note (a `note`) would be the result it drops.
- A result-level `logicalLocations` property, as the table's shorthand reads, was rejected because the SARIF schema forbids it.
- Omitting `help` was rejected because GitHub's documentation lists it as required for rules.

## Consequences

SARIF output validates against the OASIS schema and stays inside GitHub's limits. Fingerprints change only when the rule, the pointer or the transaction ID changes. Consumers that need every finding read the JSON output; the SARIF note says so.

## Spec IDs affected

FR-023, NFR-001, NFR-007, US-07, T-28
