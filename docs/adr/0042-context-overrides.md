# ADR-0042: Context overrides build an effective transaction

- Status: Accepted
- Date: 2026-10-25

## Context

Flags beat payload values, which beat config, which beat defaults. Rules must see one consistent context.

## Decision

Before evaluation the engine builds an effective transaction: `card.brand`, `card.product`, `merchant.mcc` and `merchant.country` take the flag value when given, else the payload, else config, else defaults (merchant country `US`). Each override that changes a payload value adds a note. Overridden values have no source path. As-of date is `--as-of`, else `transactionDate`, else today from `TimeProvider`.

## Alternatives considered

Passing context separately to every rule was rejected; rules read fields uniformly.

## Consequences

Notes appear in text output and in JSON `notes`.

## Spec IDs affected

FR-009, FR-010, US-03, US-13
