# ADR-0041: Lock file creation

- Status: Accepted
- Date: 2026-10-25

## Context

The spec describes `.l3lint.lock` but no command writes it.

## Decision

`l3lint init` writes `.l3lint.lock` next to `.l3lint.yaml` with the IDs and versions of the embedded packs. The format is YAML (`version`, `packs`). Users may also edit it by hand. The CLI looks for it in the config file's folder, or the current folder when there is no config.

## Alternatives considered

A new `rules lock` command was rejected to keep the spec's eight commands unchanged.

## Consequences

Teams commit the lock file with their config.

## Spec IDs affected

FR-019, FR-032
