# ADR-0054: Reports carry the metadata of the rules that fired

- Status: Accepted
- Date: 2026-10-26

## Context

SARIF rule descriptors need each rule's title, requirement, brand, level and catalog severity, and the Markdown summary shows titles next to rule links. `LintReport` held only findings, and the CLI picks writers from the static `ReportWriters.All` list, so a writer has no linter to ask.

## Decision

`LintReport.Rules` lists the `RuleMetadata` of every rule that produced a finding, ordered by ID, engine diagnostics included. `LintRunner` fills it after a run by looking up each ID of `Summary.ByRule` with `ILinter.TryGetRule`. Writers find rules by ID; a rule missing from the list is described by its ID and its first finding, so hand-built reports still render.

## Alternatives considered

- A rule lookup in `ReportWriterOptions` was rejected: rule text is report content, not a rendering choice, and every caller would have to pass it.
- A `SarifReportWriter` constructor that takes the linter was rejected: `ReportWriters.All` is static and the CLI looks writers up by name.

## Consequences

`LintReport` gains one init property (an additive public API change), and `src/L3Lint.Hosting/Runs/LintRunner.cs` gains a few lines, so that file must merge with the WP8 writers. Library users who build reports themselves can set `Rules` to get full descriptors.

## Spec IDs affected

FR-023, FR-024, FR-031
