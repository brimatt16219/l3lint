# ADR-0008: A canonical payload model sits in the middle

- Status: Accepted
- Date: 2026-10-25

## Context

Decision D8: gateways change, rules should not.

## Decision

Every adapter maps its format into the canonical model and a path map from canonical pointers to source pointers. Rules read only canonical fields. Findings carry both paths so users see their own format (FR-007).

## Alternatives considered

Writing rules per gateway was rejected; it multiplies rules by gateways.

## Consequences

A new gateway is an adapter plus contract tests, with no rule changes.

## Spec IDs affected

D8, FR-003, FR-004, FR-007
