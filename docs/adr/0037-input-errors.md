# ADR-0037: Input errors versus findings

- Status: Accepted
- Date: 2026-10-25

## Context

The spec separates payload problems (findings) from run problems (exit codes).

## Decision

Bytes that are not well-formed JSON, files over the size limit, JSON deeper than the depth limit, transactions with more line items than `inputs.limits.maxLineItems`, unreadable files and ambiguous formats are input errors: the document is skipped, stderr explains why, and the run exits 2 (unless a pack or internal error raises it to 3). Other documents in the same run are still linted. Everything else about a well-formed document is a finding. Exit code precedence is 3, then 2, then 1, then 0.

## Alternatives considered

Reporting malformed JSON as GEN-FMT-001 was rejected because there is no tree to point into.

## Consequences

MCP returns these input errors as `isError` tool results.

## Spec IDs affected

FR-025, FR-034, US-01
