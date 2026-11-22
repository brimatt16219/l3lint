# ADR-0018: VISA-LT-001 runs only with a rate table

- Status: Accepted
- Date: 2026-10-25

## Context

The Large Ticket break-even moves with each rate schedule. Clarification 8.

## Decision

VISA-LT-001 fires only when `--rates` supplies Visa categories with tier `largeTicket` for the card product. It computes the break-even amount where the Large Ticket cost equals the Product 3 cost (including program fees) and reports when the amount is at or above it. Without rates it never fires.

## Alternatives considered

Hard-coding the $7,756 estimate was rejected; it would go stale.

## Consequences

The rate table schema documents the `largeTicket` tier.

## Spec IDs affected

VISA-LT-001, FR-028
