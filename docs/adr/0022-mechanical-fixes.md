# ADR-0022: Mechanical fixes only

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 12 and US-06.

## Decision

`fix` emits RFC 6902 operations against the source format's paths, with values converted back to the source units (for example minor units for Stripe). Fixes: recompute line totals; normalize unit codes through documented maps; trim overlong fields; set explicit indicators that follow from other data (tax indicator `taxable` when tax is above 0, debit indicator, discount indicator, line taxable flag); renumber sequences; round excess decimals; set a missing summary or line discount, freight or duty to 0 only when the arithmetic proves it. The transaction amount is recomputed only with `--fix-amount`. Uncertain mappings (for example BX to PCE) are proposals that are listed for a person and never applied. Descriptions, commodity codes and tax amounts are never invented. Each fix is attached to the finding that motivated it. For CSV input, patches target the canonical document because CSV has no pointer syntax.

## Alternatives considered

Applying proposals automatically was rejected (US-06).

## Consequences

`suggest_fixes` returns the same patch plus the list that needs a human.

## Spec IDs affected

FR-027, US-06
