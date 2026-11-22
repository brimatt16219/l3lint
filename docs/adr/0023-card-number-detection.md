# ADR-0023: Card-number detection follows the Architecture algorithm exactly

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 13.

## Decision

Every string and number token is visited before mapping. Spaces and hyphens are stripped; values that are not 13 to 19 digits are skipped; the Luhn check runs; the prefix and length must match Visa (4; 13, 16 or 19 digits), Mastercard (51 to 55, 2221 to 2720; 16), Amex (34, 37; 15) or Discover (6011, 644 to 649, 65; 16 to 19). Matches in code fields (`productCode`, `commodityCode`, `sequence`) are warnings, elsewhere errors. Values become asterisks plus the last four digits. `pci.allowFields` (canonical pointers, `*` allowed) exempts paths after human review.

## Alternatives considered

Scanning for card numbers embedded inside longer text was considered safer but is outside the algorithm the clarification requires. It is listed as a limitation and a candidate for a later release.

## Consequences

A card number written inside a sentence is not detected. The docs say so plainly.

## Spec IDs affected

D4, FR-033, NFR-007, US-12, GEN-PCI-001
