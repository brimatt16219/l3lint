# ADR-0007: One .NET 10 codebase

- Status: Accepted
- Date: 2026-10-25

## Context

Decision D7: one engine for CLI, MCP server, Action and library gives identical answers everywhere.

## Decision

The solution targets `net10.0` with the latest C# language version, pinned by `global.json` with `rollForward: latestFeature`. All hosts call the same `ILinter` through `L3Lint.Hosting`.

## Alternatives considered

Separate implementations per surface were rejected; they would drift.

## Consequences

Users without .NET get self-contained single-file binaries (NFR-004).

## Spec IDs affected

D7, G3, NFR-004, NFR-009
