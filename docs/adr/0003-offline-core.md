# ADR-0003: The shipped tool is offline

- Status: Accepted
- Date: 2026-10-25

## Context

Payloads hold customer names, addresses and invoice data. Decision D3 keeps all of it on the user's machine.

## Decision

Core, rule packs, adapters, reporting, Hosting, the CLI and the Action make no network calls and contain no telemetry, update checks or crash reporting. JsonSchema.Net runs with remote reference fetching disabled. An architecture test fails if any of these assemblies references `System.Net.Http` types. The MCP server listens on 127.0.0.1 when HTTP is chosen and makes no outbound calls.

## Alternatives considered

Optional telemetry was rejected: it would need consent flows and would weaken the privacy promise.

## Consequences

Rule updates arrive through new tool or pack versions, never by download at runtime.

## Spec IDs affected

D3, FR-035, NFR-005, G5
