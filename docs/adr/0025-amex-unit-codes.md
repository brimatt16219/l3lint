# ADR-0025: Amex unit codes

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 15. The spec's source is the public Mastercard Gateway Amex acquirer page.

## Decision

The AMEX pack embeds `amex-uom.csv` with source URL and retrieval date in its header. If the full list cannot be confirmed from public pages, only confirmed codes ship, `verification.md` marks the list as partial, and AMEX-L3-001 runs at warning severity until the list is confirmed.

## Alternatives considered

Copying a full X12 list from a non-public standard document was rejected (D1).

## Consequences

Severity of AMEX-L3-001 depends on the list status recorded in the pack.

## Spec IDs affected

AMEX-L3-001, FR-014
