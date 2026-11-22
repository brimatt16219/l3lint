# ADR-0021: SARIF locations come from the token index

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 11.

## Decision

The reader builds a token position index over the original bytes on first need: each JSON Pointer maps to the 1-based line and UTF-16 column of its value token. When a finding's source path does not exist (a missing field) the nearest existing ancestor's position is used. For NDJSON, lines count from the start of the file. For CSV, the location is the physical row and the column where the mapped cell starts.

## Alternatives considered

Byte offsets were rejected; GitHub expects lines and columns.

## Consequences

The index is built only for documents that produce findings.

## Spec IDs affected

FR-023, US-07
