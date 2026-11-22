# L3LINT-CTX-001: Card brand unknown

| Property | Value |
| --- | --- |
| Severity | information |
| Confidence | policy |
| Level | general |
| Brand | all brands |
| Impact | hygiene |
| Effective | always |
| Pack | l3lint.engine 1.0.0 |

## Requirement

Card brand unknown

## Check

`engine`

## Message

`The card brand is unknown, so only the general rules ran.`

## Fix

Pass --brand, or send card.brand in the payload.

## Sources

- <https://brimatt16219.github.io/l3lint/articles/output-formats.html>
