# ADR-0031: The Hosting project ships as the meta-package L3Lint.Hosting

- Status: Accepted
- Date: 2026-10-25

## Context

The library package table lists `L3Lint` (Core, Hosting, Adapters, Reporting and all packs) and no separate Hosting package. Hosting already depends on everything the meta-package needs.

## Decision

`src/L3Lint.Hosting` is the meta-package. Its dependencies bring Core, Adapters, Reporting and the five pack packages. This keeps the spec's eleven production projects. Its package ID is `L3Lint.Hosting`, not `L3Lint`: NuGet package IDs ignore case, so `L3Lint` and the tool package `l3lint` cannot both exist (restore already refuses the pair). The tool keeps `l3lint` because the spec's install commands (`dotnet tool install -g l3lint`, `dnx l3lint`) are the main entry point; library users run `dotnet add package L3Lint.Hosting`.

## Alternatives considered

A twelfth empty meta-package project was rejected as redundant. Renaming the tool instead was rejected because every documented install command uses `l3lint`.

## Consequences

`AddL3Lint` lives in the `L3Lint.Hosting` package. The docs and README use this ID; this is a deliberate deviation from the spec's package table, listed in the final report.

## Spec IDs affected

FR-031, Library packages table
