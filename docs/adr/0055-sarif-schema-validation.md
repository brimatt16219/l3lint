# ADR-0055: The OASIS SARIF schema is validated with draft-07 rules

- Status: Accepted
- Date: 2026-10-26

## Context

The "Output conformance" test row asks for SARIF validation against the SARIF 2.1.0 schema with JsonSchema.Net. The official OASIS schema (errata 01, also in the OASIS GitHub repository) declares JSON Schema draft-04. JsonSchema.Net 8.0.5 (ADR-0045) implements draft-06 and later, not draft-04.

## Decision

The test project vendors the OASIS file unchanged. When it loads the file it checks that the file declares draft-04 and uses none of the draft-04 forms that later drafts changed (boolean `exclusiveMinimum` or `exclusiveMaximum`), then replaces the `$schema` URI with the draft-07 URI in memory and builds the schema in a private registry. The file uses only local `$ref`s, and draft-04 and draft-07 both ignore keywords next to `$ref`, so the verdicts are the same. Format checks (`uri`, `uri-reference`, `date-time`) are on. Tests that break a valid file (a bad `level`, an unknown property, a URI with a space) prove the validator rejects what it should.

## Alternatives considered

- A copy of the schema republished in a later draft by a third party was rejected: the spec's conformance target is the OASIS artifact.
- Converting the vendored file by hand was rejected: the OASIS notice does not allow modified copies.
- Adding a second validator package with draft-04 support was rejected as a new dependency for one test.

## Consequences

If OASIS publishes a schema that uses draft-04-only forms, the guard checks fail loudly instead of validating with the wrong rules.

## Spec IDs affected

FR-023, T-28
