# ADR-0019: Brand selection: auto and all

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 9 settles `--brand auto` and `--brand all`; the Architecture prose describes a report-as mode for unknown brands.

## Decision

`auto` reads `card.brand`. When the brand is unknown, only the GEN pack runs and the engine adds one information finding `L3LINT-CTX-001` (ADR-0033) telling the user to pass `--brand`. `all` evaluates every brand pack, labels each finding with its brand and reports a likely tier per brand. An explicit brand overrides the payload and prints a note.

## Alternatives considered

Running every brand pack for unknown brands (the prose reading) was rejected by the clarification as too noisy.

## Consequences

Transactions report `likelyTiers` per evaluated brand, plus `likelyTier` for the primary brand.

## Spec IDs affected

FR-009, US-03
