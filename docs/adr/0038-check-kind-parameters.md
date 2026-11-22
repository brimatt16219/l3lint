# ADR-0038: Check kind parameters

- Status: Accepted
- Date: 2026-10-25

## Context

The Architecture table lists the parameters of the eight declarative kinds. Several catalog rules need small extensions.

## Decision

Additions, all optional: `fields` (a list instead of one `field`) for `required`, `pattern`, `maxLength`; `report: once` for `required`; `positiveOnly` for `required`; `values` (inline list) and `mode: deny` for `inList` plus `list` accepting several list names and `allowMissing`; `limits` (list of field, max, profileKey, brands) for `maxLength`; conditions `when` and `unless` in `appliesTo`. Each addition is in `rulepack.schema.json` and documented in the rule authoring guide.

## Alternatives considered

Using `custom` checks for these rules was rejected; data stays editable without code.

## Consequences

The eight kinds keep their names and the spec's parameters keep their meaning.

## Spec IDs affected

FR-012, D6
