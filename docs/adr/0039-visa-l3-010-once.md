# ADR-0039: VISA-L3-010 reports once per transaction

- Status: Accepted
- Date: 2026-10-25

## Context

The spec says the example payload gets exactly one VISA-L3-010 warning although both lines lack the indicator.

## Decision

VISA-L3-010 uses `report: once`: a single finding at the first line missing the indicator, with a message that counts the lines. Other per-line rules report each failing line.

## Alternatives considered

Per-line reporting would give two warnings and contradict the spec's example.

## Consequences

The fix text tells the user to confirm the processor sets the indicator, which applies to the whole transaction.

## Spec IDs affected

VISA-L3-010
