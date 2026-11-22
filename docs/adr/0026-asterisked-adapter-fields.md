# ADR-0026: Asterisked adapter fields

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 16. Several gateway field names in the Architecture mapping table are marked for confirmation.

## Decision

Each asterisked field was checked against the gateway's current public API reference by research recorded in `docs/dev/research/adapters.md`. Confirmed or corrected names are implemented. Fields that stay unconfirmed keep the spec's mapping, are marked as unconfirmed in that adapter's documentation, and are covered by contract tests built from the shape of the gateway's published examples. All results are listed in `verification.md`.

## Alternatives considered

Dropping unconfirmed fields was rejected; users of those gateways would lose findings.

## Consequences

Adapter docs carry a confirmation column.

## Spec IDs affected

FR-003, FR-004, T-24
