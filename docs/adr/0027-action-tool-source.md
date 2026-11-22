# ADR-0027: Action input tool-source

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 17. The Action installs l3lint from NuGet, which cannot work before the package is published.

## Decision

The Action gains an optional `tool-source` input. When set, it is passed to `dotnet tool install --add-source`, so a local folder or feed can supply the tool. This is an addition to the spec's input table and is documented as such.

## Alternatives considered

Building from source inside the Action was rejected as slow.

## Consequences

The owner can test the Action against a locally packed tool before publishing.

## Spec IDs affected

FR-030, T-36
