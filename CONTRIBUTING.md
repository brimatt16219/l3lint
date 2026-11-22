# Contributing to l3lint

Thank you for helping. l3lint is only useful if it is right, so this guide is mostly about evidence: where rules come from, how they are tested, and what we never do to make a build pass.

By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md). Report security problems privately, as [SECURITY.md](SECURITY.md) describes, not in an issue.

## Build

You need the .NET 10 SDK. `global.json` pins the feature band.

```text
dotnet build L3Lint.slnx
```

Every project builds with warnings as errors, nullable reference types and the recommended analyzers. Public members of the `src` projects need XML documentation.

The repository uses Central Package Management (`Directory.Packages.props`) with lock files. After you add a package or a project, run `dotnet restore` and commit the changed `packages.lock.json` files. With `ContinuousIntegrationBuild=true`, restore runs in locked mode and fails on a stale lock file.

## Test

The test projects use xUnit v3 on Microsoft.Testing.Platform (`global.json` opts in), so pass the project with `--project`:

```text
dotnet test --project tests/L3Lint.Core.Tests
dotnet test --project tests/L3Lint.Rules.Tests
dotnet test --project tests/L3Lint.Adapters.Tests
dotnet test --project tests/L3Lint.Reporting.Tests
dotnet test --project tests/L3Lint.Cli.Tests
dotnet test --project tests/L3Lint.Mcp.Tests
```

Also run every rule's examples:

```text
dotnet run --project src/L3Lint.Cli -- rules test
```

## Generated files

Some files are generated. Change the source, regenerate, and commit both. Each tool has a `--check` mode that changes nothing and fails on stale output.

| Tool | Writes | Command |
| --- | --- | --- |
| `tools/L3Lint.PackManifest` | `manifest.json` of each rule pack | `dotnet run --project tools/L3Lint.PackManifest` |
| `tools/L3Lint.FixtureGen` | Rule pack examples and samples | `dotnet run --project tools/L3Lint.FixtureGen -- examples` |
| `tools/L3Lint.DocsGen` | Rule pages, the CLI reference, the schema references and `docs/schemas/` | `dotnet run --project tools/L3Lint.DocsGen -- all` |

`L3Lint.DocsGen --check` also fails when a rule, command, option or schema property has no description, so every new option and output field needs one.

## Formatting and style

```text
dotnet format L3Lint.slnx --verify-no-changes
```

`.editorconfig` holds the code style. A rule may be turned off only with a comment that gives the reason.

## Public API

Library projects track their public surface with `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (Microsoft.CodeAnalysis.PublicApiAnalyzers). A new public member fails the build until you add it to `PublicAPI.Unshipped.txt`; the analyzer's code fix writes the line for you. Do not remove a shipped entry without a major version change.

## Adding or changing a rule

1. **Find a public source.** Rules come only from public pages: brand publications first, then processor or gateway documentation, then published guides. Never use or paraphrase non-public material, such as acquirer-only specifications or anything from your employer.
2. **Write the rule** in the pack YAML under `src/L3Lint.Rules.<Brand>/Pack/`, with its sources, confidence label, effective dates, severity, message and fix. [Writing rule packs](docs/articles/rule-pack-authoring.md) documents every field.
3. **When sources disagree**, record all of them, encode the stricter reading, and explain it in the rule's `notes`.
4. **Add a passing and a failing example.** Shipped examples are generated from compact definitions in `tools/L3Lint.FixtureGen` (ADR-0040).
5. **Bump the pack version** (minor for a new rule, patch for wording or sources, major for a removed rule or a changed meaning) and regenerate the manifest.
6. **Run** `rules test`, the test suites and `L3Lint.DocsGen -- rules`.
7. **Add an ADR** in `docs/adr/` when the change makes a decision the spec does not.

Heuristics that look for fake data are warnings unless a public source names the exact pattern.

## No shortcuts

- **Tests are not circular.** Expected results come from the spec, the sources and hand-written definitions, never from running l3lint and copying its output. Snapshot files are reviewed by hand like code.
- **Do not weaken a check to pass a build.** No disabled tests, no loosened assertions, no analyzer suppressions or `#pragma` without a written reason, no skipped hooks.
- **No invented facts.** If a public source does not settle a fact, say so in the rule notes and in `docs/articles/limitations.md`; do not pick a value and present it as known.
- **No placeholders.** No `TODO`, `FIXME` or placeholder text in code, data or docs.
- **Synthetic data only.** Fixtures never contain real customer, merchant or employer data. Card numbers come only from public test ranges.
- **Offline and deterministic.** No network calls from Core, rules, adapters, reporting or the CLI. The same input gives the same output.
- **Card numbers are masked first.** Nothing may read a payload before the masking stage.

## Writing style

This applies to docs, messages, comments and commit messages.

- Use plain words and short sentences. Messages stay under 25 words a sentence and state the fix as an action.
- Do not use em dashes or en dashes. Use commas, colons, periods or parentheses.
- Findings are advisory. Write "likely to downgrade" or "meets the documented requirement". Never write that a transaction will earn a rate.
- User-facing strings live in `Resources/Strings.resx`, not in code.
- Every command example in the docs must be one you ran, and printed output must be real output (shortened with a note when needed).

## Pull requests

Keep a pull request to one change. Describe what changed and why, link the sources for any rule change, and include the commands you ran. Before you open it, run the build, the tests, the format check and the generated-file checks above.
