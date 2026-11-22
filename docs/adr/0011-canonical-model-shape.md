# ADR-0011: The C# model mirrors the canonical JSON

- Status: Accepted
- Date: 2026-10-25

## Context

The Architecture sketch flattens some objects (`CustomerReference`, `BillingPostalCode`) while the canonical JSON nests them (`customer.reference`, `billing.postalCode`). Clarification 1 makes JSON authoritative.

## Decision

The model is a set of nested immutable records that mirror the JSON one to one: `Transaction` with `Card`, `Merchant`, `Customer`, `Order`, `Tax`, `Amounts`, `Shipping`, `Billing`, `Amex` and `LineItems`. `Amount` and `Currency` are separate nullable properties, because either may be absent or invalid. Enumerations carry the JSON string values. A source-generated `JsonSerializerContext` writes the canonical JSON for `convert` and `map_payload`.

## Alternatives considered

Keeping the flat sketch would need a translation layer and two vocabularies.

## Consequences

Field paths in findings, docs and the schema are the same strings.

## Spec IDs affected

FR-002, FR-012, D8
