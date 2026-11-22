# ADR-0034: Batch findings

- Status: Accepted
- Date: 2026-10-25

## Context

GEN-DQ-007 is decided after all transactions are read, but results stream per transaction (bounded memory, NFR-003).

## Decision

Batch findings point at the first transaction (lowest index) that carries the repeated value, with that line's canonical and source path, so they have a real file location. `ILinter.LintManyAsync` yields per-transaction results and then, when batch rules fired, one trailing `TransactionResult` of kind `Batch` that holds them. Reports show them in a separate batch section (`batchFindings` in JSON). `ILintSession` gives the same behavior for callers that drive documents themselves.

## Alternatives considered

Mutating already yielded results was impossible with immutable records.

## Consequences

Batch findings are deterministic at any thread count because counts are order-independent and the anchor is the minimum index.

## Spec IDs affected

FR-017, US-04, NFR-001, NFR-003
