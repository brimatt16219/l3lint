# ADR-0029: Initial versions and pack dates

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 19.

## Decision

The tool, library packages, MCP server and every rule pack start at 1.0.0. Each pack's `publishedOn` is the build date of this release, 2026-10-25.

## Alternatives considered

Mirroring the spec's example version 1.2.0 was rejected; this is the first release.

## Consequences

Later pack updates bump pack versions independently.

## Spec IDs affected

FR-012, US-11
