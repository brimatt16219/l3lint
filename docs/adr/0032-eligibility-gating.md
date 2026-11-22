# ADR-0032: Eligibility gating and supersession

- Status: Accepted
- Date: 2026-10-25

## Context

US-03 requires that a consumer card or an excluded MCC yields one informational finding and suppresses rate-related errors. Several ELIG rules overlap (GEN-ELIG-001 and VISA-ELIG-001 both fire for a Visa consumer card), and some brand rules restate a GEN check more strictly (VISA-DQ-001 over GEN-DQ-001).

## Decision

Rate-related rules carry `appliesTo` gates (card products, MCC lists, federal exclusion) as in the spec's pack excerpt, so they do not run on ineligible transactions. A rule may declare `supersedes: [ruleIds]`; when it fires at a canonical path, findings of the listed rules at the same path are dropped for that transaction. GEN-ELIG-001 and VISA-ELIG-003 supersede VISA-ELIG-001; VISA-DQ-001 supersedes GEN-DQ-001 and GEN-DQ-002; VISA-DQ-002 supersedes GEN-DQ-003; brand length and list rules supersede GEN-FMT-006 at the same path. Unknown card products do not skip rules, so data problems still show.

## Alternatives considered

An impact-based global suppression was rejected as hidden behavior. Removing overlapping catalog rules was not allowed (the catalog is normative).

## Consequences

`rules test` runs each rule alone, so every rule still demonstrates its own pass and fail examples.

## Spec IDs affected

US-03, FR-012
