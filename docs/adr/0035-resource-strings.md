# ADR-0035: Where user-facing text lives

- Status: Accepted
- Date: 2026-10-25

## Context

NFR-011 requires user-facing strings in resource files.

## Decision

Engine, CLI, reporting and MCP strings live in `.resx` files. Rule titles, messages, fixes and rationale live in the rule pack YAML, which is an embedded resource and the localization unit for rule text. A completeness test checks that every resource key used in code exists and that no user-facing literal appears in code outside resources.

## Alternatives considered

Duplicating rule text into `.resx` was rejected; packs must stay the single source for rule wording.

## Consequences

A future locale ships translated `.resx` files and translated pack text.

## Spec IDs affected

NFR-011
