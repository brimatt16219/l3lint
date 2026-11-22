# ADR-0009: Money is decimal with an explicit ISO 4217 exponent

- Status: Accepted
- Date: 2026-10-25

## Context

Decision D9: floating point breaks line-item arithmetic at the cent level.

## Decision

All amounts are `decimal`. Currency exponents come from an embedded ISO 4217 table built from the public ISO 4217 list, with source and retrieval date in the file header. Adapters convert minor-unit integers at the boundary. Rounding happens only at comparisons, with the configured midpoint mode. Numbers are parsed and formatted with the invariant culture.

## Alternatives considered

`double` was rejected for precision. A money library was rejected as unnecessary weight.

## Consequences

Amounts that do not fit `decimal` are reported as schema findings.

## Spec IDs affected

D9, FR-008, FR-015
