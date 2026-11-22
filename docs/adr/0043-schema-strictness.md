# ADR-0043: Canonical schema strictness

- Status: Accepted
- Date: 2026-10-25

## Context

The schema must catch structural mistakes without duplicating rule findings.

## Decision

`canonical.schema.json` checks types, enumerations, `additionalProperties: false` (typos such as `commodity_code` are caught), and constraints no rule covers (`id` length, `last4` pattern, `mcc` pattern, country pattern, rate ranges, non-negative summary amounts, unit cost at most 6 decimals). Constraints that a catalog rule checks (amount above 0, currency code, line-item counts, field lengths with brand limits, sequence contiguity) are left to those rules so each problem produces one finding.

## Alternatives considered

Encoding every constraint in the schema was rejected because users would get two findings for one problem.

## Consequences

The schema validates the spec's example payload.

## Spec IDs affected

FR-002, GEN-FMT-001
