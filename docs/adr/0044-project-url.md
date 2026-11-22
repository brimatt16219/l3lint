# ADR-0044: Project and docs URLs

- Status: Accepted
- Date: 2026-10-25

## Context

SARIF needs an `informationUri` and per-rule `helpUri`, and packages need a project URL. The owner has not created the repository yet (backlog T-01 reserves the name).

## Decision

The URLs live in two MSBuild properties in `Directory.Build.props`: `L3LintProjectUrl` (https://github.com/brimatt16219/l3lint) and `L3LintDocsUrl` (https://brimatt16219.github.io/l3lint/), named after the working name. Code reads them from assembly metadata, so changing the two properties updates packages, SARIF and generated docs together.

## Alternatives considered

Leaving URLs out of SARIF was rejected because the spec's mapping table names them. Hard-coding them in several files was rejected because a rename would miss some.

## Consequences

If the final repository lives elsewhere, the owner edits two lines before the first release. The final report lists this step.

## Spec IDs affected

FR-023, FR-036, T-01
