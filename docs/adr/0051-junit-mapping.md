# ADR-0051: JUnit XML mapping, batch findings and input errors

- Status: Accepted
- Date: 2026-10-26

## Context

The spec says: one `testsuite` per input file, one `testcase` per transaction named by transaction ID, each error finding a `failure`, warnings and information in `system-out`. It does not say which JUnit schema applies, where batch findings (ADR-0034) and unreadable inputs go, what to call a transaction without an ID, or whether times appear.

## Decision

- **Schema.** Output validates against the Jenkins xUnit plugin's `junit-10.xsd` (MIT), vendored for tests. It requires no time stamps or durations, so the writer writes none: no `time`, `timestamp` or `hostname`.
- **Suites and cases.** One suite per input in report order, named by the input path, including inputs that produced no transactions. One case per transaction, named by its ID, or "Transaction N" with N the 1-based run index, as in text output.
- **Failures.** Every error finding is a `failure` whatever `--fail-on` says, as the spec states. `message` is "RULE-ID: message", `type` is the rule ID, and the text lists the path, the canonical path when it differs, the line and column, expected and actual values when known, the fix and the rule reference URL.
- **system-out.** The likely tier for each evaluated brand, then each warning and information finding with its fix.
- **Batch findings.** They go in a final suite named "Batch findings" with one case, "all transactions", using the same failure and system-out rules. Each entry names the anchor transaction and its input. Batch findings describe a pattern across the run, and JSON and text output already keep them apart; putting them on the anchor transaction's case would fail that one transaction for a run-level problem.
- **Input errors.** An input that could not be read becomes an `error` element (JUnit's element for something that stopped the check) in a case named "read input" or "read input, line N", inside the suite of that input. An error whose source matches no listed input, such as a glob with no matches, gets its own suite.
- **Counts.** `failures` and `errors` count cases, not elements, as Surefire and the Jenkins plugin do. The root `testsuites` totals are the sums over suites.
- **Text.** UTF-8 without a byte order mark, LF line endings, two-space indent. CR and CR LF become LF; characters XML 1.0 cannot carry (most control characters, unpaired surrogates) become U+FFFD.

## Alternatives considered

- The windyroad `JUnit.xsd` (Apache-2.0) was considered; the Jenkins schema was chosen because its license sits inside the file and it matches what CI dashboards read.
- Attaching batch findings to the anchor transaction's case was rejected for the reason above.
- Writing `time="0"` was rejected; the schema does not need it and a constant zero tells dashboards nothing.

## Consequences

Report notes and the `--max-findings` count do not appear in JUnit, because the schema gives `testsuites` no place for them; JSON and text carry them. JUnit output is deterministic and dashboards show one red case per transaction with errors.

## Spec IDs affected

FR-024, NFR-001, US-07, T-29
