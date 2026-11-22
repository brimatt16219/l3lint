# ADR-0048: The file size limit applies per NDJSON record

- Status: Accepted
- Date: 2026-10-26

## Context

FR-034 sets a 25 MB default for `inputs.limits.maxFileBytes`. US-04 and NFR-003 also ask for streaming NDJSON files of any size (a 1 GB batch must not sit in memory). Applying the file limit to an NDJSON file would reject exactly the batches the streaming design exists for.

## Decision

For `.ndjson` and `.jsonl` files the limit applies to each record (line), not to the file. Files are streamed one record at a time. JSON and CSV files, and anything read from standard input, keep the per-file limit.

## Alternatives considered

A separate `maxNdjsonBytes` limit was considered; it adds a config key the spec does not have.

## Consequences

A large NDJSON file lints in bounded memory. A single oversized record is reported as a too-large input error for that line, and the rest of the file still lints.

## Spec IDs affected

FR-034, NFR-003, US-04
