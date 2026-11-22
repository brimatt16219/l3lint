# ADR-0016: Single-processor rules use a profiles attribute

- Status: Accepted
- Date: 2026-10-25

## Context

The spec says rules sourced from a single processor run as warnings unless that processor's profile is selected. Clarification 6 asks for a rule attribute, not special code.

## Decision

A rule may declare `profiles: [name, ...]`. Under any other profile its severity is capped at warning. With one of those profiles selected, or with `--strict-profiles`, the catalog severity applies. Rules whose catalog severity is already warning or information are unaffected. Applied to: MC-L3-008, MC-L3-009, MC-FMT-001, AMEX-FMT-001, AMEX-L2-001, AMEX-L2-004, AMEX-L3-002, DISC-FMT-001, DISC-L2-001.

## Alternatives considered

Skipping such rules entirely outside their profile was rejected; users would lose useful warnings.

## Consequences

Severity shown in output is the effective severity; `explain` shows both.

## Spec IDs affected

FR-011, FR-012, R6
