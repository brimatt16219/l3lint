# ADR-0010: Apache-2.0 for code and rule packs

- Status: Accepted
- Date: 2026-10-25

## Context

Decision D10. The open question asked whether packs should carry a separate license such as CC BY 4.0.

## Decision

Code and rule packs are both Apache-2.0. The repository has the full license text in LICENSE and attribution in NOTICE. Packages declare the `Apache-2.0` license expression.

## Alternatives considered

CC BY 4.0 for packs was considered. One license is simpler for users and contributors, and pack files are configuration data shipped inside code packages.

## Consequences

The owner can still change the license before the first public release.

## Spec IDs affected

D10
