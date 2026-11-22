# ADR-0002: No employer material

- Status: Accepted
- Date: 2026-10-25

## Context

The maintainer works at Fiserv on Level 2 and Level 3 integrations. Decision D2 requires clean separation.

## Decision

The codebase holds no adapters, processor profiles, field mappings, examples, test data or documentation for any Fiserv property, including Clover, First Data and CommerceHub. Fiserv and Clover documentation sites are never used as sources. Where a public page lists such a processor next to others (for example the Cybersource unit-of-measure page), l3lint cites the page for the general fact and does not name that processor.

## Alternatives considered

Adding the employer's gateways would widen coverage but needs legal clearance first (backlog T-02).

## Consequences

Coverage gaps for those gateways are documented as out of scope. The owner can revisit after written clearance.

## Spec IDs affected

D2, R4, T-02
