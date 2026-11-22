# ADR-0033: Engine diagnostics outside the rule catalog

- Status: Accepted
- Date: 2026-10-25

## Context

Some outcomes are not rules but still need to appear as findings: a rule that throws (pipeline stage 8) and an unknown brand (ADR-0019).

## Decision

The engine emits findings with IDs in a reserved `L3LINT-` namespace: `L3LINT-ENGINE-001` (error: a rule failed internally; other rules still ran) and `L3LINT-CTX-001` (information: card brand unknown, only GEN rules ran). They are documented in the output formats guide and handled by `explain`, but they are not catalog rules and do not count toward the 78.

## Alternatives considered

Inventing catalog-style IDs such as GEN-ELIG-003 was rejected; the catalog is fixed.

## Consequences

Users can filter them with `--disable L3LINT-*`, except that an internal error always sets exit code 3.

## Spec IDs affected

Pipeline stage 8, FR-025
