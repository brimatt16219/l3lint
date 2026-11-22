# ADR-0013: What counts as a missing field

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 3: null, empty and whitespace are missing; `allowZero` controls whether 0 passes.

## Decision

A required field is missing when it is absent, JSON null, an empty or whitespace-only string, an empty array, or an enum sentinel that means unknown (`notProvided` for `tax.indicator`, `unknown` for `card.product` and `card.brand`). `allowZero` applies to numbers only and defaults to false. String values such as "0" are not missing; the data quality rules judge them. `positiveOnly` (used by the quantity rules) also fails values below zero.

## Alternatives considered

Treating the string "0" as missing would double-report with VISA-DQ-002.

## Consequences

Rules state their intent in the pack; tests cover absent, null, empty, whitespace, zero and negative inputs for each kind.

## Spec IDs affected

FR-012, VISA-L3-002 to VISA-L3-011, MC-L3-002 to MC-L3-007
