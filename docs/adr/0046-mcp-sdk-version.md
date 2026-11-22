# ADR-0046: ModelContextProtocol SDK pinned to 1.x

- Status: Accepted
- Date: 2026-10-25

## Context

The spec names the ModelContextProtocol C# SDK 1.x (Architecture package table, NFR-009). NuGet now also has 2.x releases.

## Decision

Pin ModelContextProtocol and ModelContextProtocol.AspNetCore 1.4.1, the latest 1.x release, because the spec's normative package table and NFR-009 name 1.x.

## Alternatives considered

Taking 2.x as the latest stable was considered; it would change the protocol revision the spec commits to without a spec change.

## Consequences

Moving to 2.x is a deliberate later decision with its own ADR and Inspector run.

## Spec IDs affected

FR-029, NFR-009
