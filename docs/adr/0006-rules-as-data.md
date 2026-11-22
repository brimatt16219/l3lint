# ADR-0006: Rules are versioned data plus small C# predicates

- Status: Accepted
- Date: 2026-10-25

## Context

Brands change rules every April and October. Decision D6.

## Decision

Each rule is a YAML entry with ID, title, level, severity, confidence, effective dates, applicability, check, message, fix, impact, sources and examples. Most checks use the declarative kinds (ADR-0038). The `custom` kind names a C# class compiled into a pack assembly; packs never contain executable code. Packs are semantically versioned and validated by schema and checksum on load.

## Alternatives considered

Pure C# rules were rejected because data updates would need code releases. A scripting language in packs was rejected because loading a pack must never run code (NFR-006).

## Consequences

Rule authors edit YAML for most changes. New logic needs a small C# check and a tool release.

## Spec IDs affected

D6, FR-012, FR-019, NFR-006, US-11
