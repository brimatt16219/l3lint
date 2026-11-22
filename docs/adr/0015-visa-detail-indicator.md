# ADR-0015: Visa detail indicator is checked for presence only

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 5: no public list of valid values was found.

## Decision

VISA-L3-010 checks that `detailIndicator` is present and reports at most once per transaction (ADR-0039). Values are not validated.

## Alternatives considered

Validating against guessed values was rejected as unsourced.

## Consequences

If a public value list appears, a pack minor version adds a value check.

## Spec IDs affected

VISA-L3-010
