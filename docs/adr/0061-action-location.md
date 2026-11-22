# ADR-0061: The Action lives in action/ until the owner moves it to l3lint-action

- Status: Accepted
- Date: 2026-11-06

## Context

The spec publishes the GitHub Action from its own repository, `l3lint-action`, used as `uses: <owner>/l3lint-action@v1` (Test and delivery tab, distribution table; Interfaces tab, workflow example), with a sample repository `l3lint-action-example`. The build directive keeps Git and GitHub with the owner: no repositories are created here, and splitting `action/` into its own repository is one of the owner's next steps. Until then the Action has to work from this repository.

## Decision

- The Action lives in `action/` of this repository. Every reference uses the subfolder form `brimatt16219/l3lint/action@v<version>`: README.md, action/README.md, docs/articles/github-action.md, docs/articles/installation.md, samples/action-example, and the workflow that `l3lint init` writes (`src/L3Lint.Cli/Templates/workflow.yml`, checked by `FixConvertInitTests`).
- References pin the full version (`@v1.0.0`) rather than the major tag, so a workflow written by `init` matches the pack versions in the `.l3lint.lock` it writes. The release workflow still moves the major tag, so `@v1` works too.
- `samples/action-example` is the content for the spec's `l3lint-action-example` repository.

## Alternatives considered

- Writing `brimatt16219/l3lint-action@v1` now: every reference would point at a repository that does not exist yet, and the green and red Action tests could not run against it.

## Consequences

When the owner moves `action/` to `l3lint-action`, the references listed above change to `<owner>/l3lint-action@v<version>` in one edit, and `FixConvertInitTests` changes with the template. `release.yml` then tags the Action repository instead of this one.

## Spec IDs affected

FR-030, US-07
