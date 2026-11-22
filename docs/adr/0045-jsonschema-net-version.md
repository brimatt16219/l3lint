# ADR-0045: JsonSchema.Net pinned to 8.0.5

- Status: Accepted
- Date: 2026-10-25

## Context

The directive asks for the latest stable packages with permissive licenses. JsonSchema.Net 9.x binary releases ship under the Open Source Maintenance Fee EULA, which charges revenue-generating users above a threshold. Version 8.0.5 and its dependencies (JsonPointer.Net 6.0.1, Json.More.Net 2.2.0, Humanizer.Core) are MIT.

## Decision

Pin JsonSchema.Net 8.0.5, the last MIT release. Its API (`JsonSchema.FromText`, `Evaluate(JsonElement, EvaluationOptions)`, list output) covers every need.

## Alternatives considered

Version 9.x was rejected for its license terms on the binary release. Writing a schema validator by hand was rejected; the spec names JsonSchema.Net.

## Consequences

Upgrading needs a fresh license review. `docs/dev/dependencies.md` records the reason.

## Spec IDs affected

FR-002, FR-019, NFR-008
