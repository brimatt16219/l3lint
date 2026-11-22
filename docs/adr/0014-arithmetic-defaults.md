# ADR-0014: Arithmetic defaults and edge cases

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 4 sets defaults; the spec formulas leave credit lines and handling unstated.

## Decision

Line and total tolerance default to 1 minor unit of the transaction currency; mode `strict`; rounding `awayFromZero`. In `accumulated` mode the total tolerance is the larger of the configured total tolerance and 1 minor unit per line, capped at 5 minor units. Credit lines (`debitCreditIndicator: credit`) carry positive values and subtract in formula 2. `amounts.handling` is not part of formula 2, as the spec states the formula; when handling is present and not zero the GEN-ARITH-002 message says it was not counted. Unit cost is exempt from GEN-FMT-003 and the canonical schema caps it at 6 decimal places. Missing line discount or line tax counts as 0 in the formulas; a missing quantity, unit cost or total skips the line.

## Alternatives considered

Adding handling to formula 2 was considered more lenient but contradicts the spec's formula.

## Consequences

Results match the worked example exactly. Property tests cover rounding modes and exponents 0, 2 and 3.

## Spec IDs affected

FR-015, GEN-ARITH-001 to GEN-ARITH-005, GEN-FMT-003
