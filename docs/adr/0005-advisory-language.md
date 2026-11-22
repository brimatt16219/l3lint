# ADR-0005: Advisory language only

- Status: Accepted
- Date: 2026-10-25

## Context

Networks and issuers decide qualification, and Visa validates data after settlement. Decision D5.

## Decision

Output uses phrases like "likely to downgrade" and "meets the documented requirement". The word "qualifies" never appears in output strings. The tier status `meets` means the payload meets the documented requirement, not that it will earn the rate. A resource completeness test scans resource files for banned phrases.

## Alternatives considered

Plain pass or fail wording was rejected as misleading.

## Consequences

Every surface (CLI, MCP, Action, library) inherits the same wording from resources and packs.

## Spec IDs affected

D5, NG3, R1
