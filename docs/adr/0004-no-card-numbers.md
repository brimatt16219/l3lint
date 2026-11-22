# ADR-0004: Card numbers are masked before anything reads them

- Status: Accepted
- Date: 2026-10-25

## Context

Users sometimes paste real card numbers. Decision D4 keeps l3lint out of PCI DSS scope.

## Decision

Stage 3 of the pipeline scrubs every string and number token before mapping (ADR-0023 has the algorithm). Matches become asterisks plus the last four digits in the in-memory document, and a GEN-PCI-001 finding records the path. Every later stage, log line and output sees only the masked value. `--strict-pci` stops the run with exit code 1. Masking runs even when GEN-PCI-001 is disabled. Test data uses only public test card numbers.

## Alternatives considered

Rejecting any input with a card number (no masking) was rejected because users would lose all other findings.

## Consequences

The original bytes stay in memory only to compute the input SHA-256 and token positions; they are never written out.

## Spec IDs affected

D4, FR-033, NFR-007, US-12
