# ADR-0024: Rule pack integrity: manifest and lock file

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 14 and FR-019.

## Decision

Each pack has a `manifest.json` with the SHA-256 of every pack file (YAML, lists, examples), computed over bytes with CRLF normalized to LF so checkouts on any OS agree. `tools/L3Lint.PackManifest` writes it. The loader validates each pack against `rulepack.schema.json`, recomputes the hashes and refuses to start on any mismatch (exit 3). A unit test fails if an embedded pack differs from its manifest. Local packs loaded with `--rule-pack` need a manifest too. `.l3lint.lock` records pack IDs and versions; when present, a mismatch exits 3.

## Alternatives considered

Signing packs was deferred to the release pipeline (NFR-008).

## Consequences

Pack authors run the manifest tool after editing; CI catches stale manifests.

## Spec IDs affected

FR-019, US-11
