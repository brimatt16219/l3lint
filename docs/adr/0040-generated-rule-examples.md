# ADR-0040: Rule examples come from compact definitions

- Status: Accepted
- Date: 2026-10-25

## Context

Each rule needs a pass and a fail example (156 files). The directive also lists the 156 rule examples as part of the generated corpus.

## Decision

Examples are defined compactly in `tools/L3Lint.FixtureGen` as a base payload, a context and a small set of edits, each with the rule that must or must not fire, written from the spec. FixtureGen materializes them as `examples/<RULE>.pass.json` and `.fail.json` inside each pack and as fixtures with `expect.yaml` under `fixtures/canonical/<brand>/`. A test regenerates them in memory and fails if a committed file differs.

## Alternatives considered

Hand-writing 156 full payload files was rejected as error-prone; generation keeps each example minimal and reviewable.

## Consequences

Expectations come from the definitions, never from running l3lint.

## Spec IDs affected

FR-020, T-15 to T-17, T-22
