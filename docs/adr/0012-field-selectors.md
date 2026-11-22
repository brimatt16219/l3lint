# ADR-0012: Field selectors use JSON Pointer with one wildcard

- Status: Accepted
- Date: 2026-10-25

## Context

Rules need to address line-item fields. Clarification 2.

## Decision

Selectors are RFC 6901 JSON Pointers with at most one `*` segment for arrays, for example `/lineItems/*/total`. Findings report the concrete index (`/lineItems/1/total`). Selectors are resolved against a fixed table of the 52 canonical fields at pack load, so an unknown field makes the pack invalid (exit 3). Message placeholder `{index}` renders the 1-based line number, so `/lineItems/1` reads as "Line 2" as in the spec example.

## Alternatives considered

JSONPath was rejected as heavier than needed. Reflection-based access was rejected for speed.

## Consequences

Field access is a table lookup with no reflection.

## Spec IDs affected

FR-007, FR-012
