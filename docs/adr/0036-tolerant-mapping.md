# ADR-0036: Tolerant canonical mapping

- Status: Accepted
- Date: 2026-10-25

## Context

Pipeline stage 5: type errors stop rules that need that field; everything else continues.

## Decision

The canonical reader never throws on payload content. A value of the wrong type is recorded as invalid at its pointer, the schema stage reports GEN-FMT-001, and every check that reads an invalid field skips it. The same applies to values adapters cannot convert.

## Alternatives considered

Strict deserialization was rejected; one bad field would hide all other findings.

## Consequences

A wrong-typed field produces exactly one finding (GEN-FMT-001), not a second "missing" finding.

## Spec IDs affected

FR-002, Pipeline stage 5
