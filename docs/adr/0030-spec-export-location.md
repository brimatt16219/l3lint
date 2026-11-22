# ADR-0030: Spec export location

- Status: Accepted
- Date: 2026-10-25

## Context

The directive names `./spec` as the spec folder, but the owner's export landed in the repository root as seven Markdown files.

## Decision

The seven root files are treated as the read-only spec set and are not moved, copied or edited. The final report asks the owner to move them into `spec/` (or out of the repository) before the first commit.

## Alternatives considered

Copying them into `spec/` was rejected because two copies would drift.

## Consequences

Nothing in the build reads these files.

## Spec IDs affected

Directive section 1
