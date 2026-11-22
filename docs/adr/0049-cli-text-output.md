# ADR-0049: CLI text output uses LF and ASCII tables

- Status: Accepted
- Date: 2026-10-26

## Context

The Architecture tab lists Spectre.Console for tables and color in text output. On Windows consoles that do not use UTF-8, box-drawing characters print as garbage, and the default line ending is CRLF, so the same command prints different bytes on different systems.

## Decision

- The CLI writes LF line endings on every operating system.
- Tables (`rules list`) use Spectre.Console with ASCII borders and a fixed width of 160 columns.
- Color is used only when standard output is a terminal, and never with `NO_COLOR` or `--no-color` (NFR-010).
- `explain` and `rules show` print the rule reference page as Markdown, the same page the docs site publishes, so text and docs never drift.

## Alternatives considered

Unicode borders with a fallback were considered; detecting console encoding reliably across terminals and CI logs is fragile.

## Consequences

Text output is stable enough for snapshot tests and for pasting into tickets.

## Spec IDs affected

NFR-001, NFR-010, US-05, US-11
