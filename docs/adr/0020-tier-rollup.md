# ADR-0020: Tier rollup

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 10 defines the stepping rule.

## Decision

Each brand pack supplies a tier ladder and eligibility (RULE-DESIGN.md). For each evaluated brand the engine starts at the top tier the card and MCC allow, then steps down past every tier blocked by an error finding of that brand or of GEN with impact `blocksLevel3` (removes Level 3 tiers) or `blocksLevel2` (removes Level 2 and Level 3 tiers). A warning or error with impact `riskVisaValidation` marks Product 3 as `atRisk` instead. Statuses: `meets`, `atRisk`, `blocked`, `noBenefit`. Amex and Discover always report `noBenefit`. Unknown brands report no tier.

## Alternatives considered

Computing tiers from impact `noBenefit` findings was rejected; eligibility comes from the brand's own lists so disabling an ELIG rule cannot change the tier.

## Consequences

The text output shows lines such as "Likely tier: below Product 3 (blocked by 1 error)".

## Spec IDs affected

FR-028, US-03, US-10
