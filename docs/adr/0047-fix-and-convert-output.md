# ADR-0047: Output shapes for fix and convert

- Status: Accepted
- Date: 2026-10-26

## Context

The spec says `l3lint fix --dry-run` prints an RFC 6902 JSON Patch and `--write` applies it to a copy (US-06), and `l3lint convert --to canonical` prints canonical JSON with the path map (US-02). It does not say what happens for inputs that hold more than one transaction (NDJSON, CSV), whether the copy may contain card numbers, or what the convert document looks like.

## Decision

`fix`:

- A JSON file gives one patch document (an indented JSON array).
- An NDJSON file gives one compact patch per non-blank input line, in input order, so line N of the output patches record N.
- CSV patches target the canonical form, because CSV cells have no JSON Pointer. `--write` then writes canonical NDJSON, one transaction per line.
- The patched copy is built from the masked document, so a card number never reaches the output file.
- `--write` refuses to overwrite the input, and `--dry-run` with `--write` is a usage error.
- Findings a person must decide (proposals, findings with no mechanical fix, amount changes without `--fix-amount`) go to standard error. The exit code is 0 when the plan was produced, and 1 when `--strict-pci` stopped on a card number.

`convert --to canonical`:

- Prints `{ format, cardNumbersMasked, transactions: [{ index, line?, canonical, pathMap, derivedFields, notes }] }`. `pathMap` maps every canonical leaf pointer that has a source equivalent to its source pointer, in pointer order. The MCP `map_payload` tool returns the same document.
- NDJSON input gives one compact document per line.
- `--canonical-only` prints only canonical JSON, one transaction per line, ready for `lint --format canonical`.
- `--strict-pci` stops with exit code 1 when a card number is found, the same as `lint`.

## Alternatives considered

A single JSON array of patches for NDJSON was considered; line-for-line output is easier to pair with the input and streams. Writing the unmasked original for `--write` was rejected by hard constraint 6 (masking before any output).

## Consequences

Users of CSV input get canonical NDJSON from `fix --write`, not a fixed CSV. The CLI reference documents this.

## Spec IDs affected

US-02, US-06, FR-007, FR-027, FR-033
