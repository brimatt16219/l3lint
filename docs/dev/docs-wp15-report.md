# WP15 report: documentation

Date: 2026-10-30. Scope: T-38, FR-036, NFR-014, release steps 6 and 7 inputs (docs site and changelog). Nothing was committed, pushed, tagged or published.

## Results

| Check | Command | Result |
| --- | --- | --- |
| Solution build | `dotnet build L3Lint.slnx` | 0 warnings, 0 errors |
| Generated docs | `dotnet run --project tools/L3Lint.DocsGen -- all --check` | `94 generated file(s) checked, 0 stale, 0 documentation problem(s).` exit 0 |
| Docs site | `dotnet tool run docfx docs/docfx.json --warningsAsErrors` (docfx 2.81.0) | `Build succeeded.` `0 warning(s)` `0 error(s)` |
| Formatting | `dotnet format tools/L3Lint.DocsGen/L3Lint.DocsGen.csproj --verify-no-changes` and the same for `src/L3Lint.Cli` | exit 0 |
| Tests after the schema edits | `dotnet test --project tests/L3Lint.<X>.Tests` for Core, Cli, Mcp, Reporting, Rules, Adapters | 121, 89, 40, 110, 104 and 749 passed, 0 failed |
| Rule examples | `l3lint rules test` | 78 tested, 78 passed |
| README quick start | see `docs/dev/readme-check.md` | every command matched; the global NuGet install was checked with a local package instead |

## What was built

### tools/L3Lint.DocsGen

A console tool (in `L3Lint.slnx` under `/tools/`) with the commands `rules`, `cli`, `schemas` and `all`, and `--check`.

- `rules` renders `docs/rules/{ID}.md` with `RuleMarkdown.Render` (the page `l3lint explain` prints) for the 78 rules and the two engine diagnostics that `ILinter.TryGetRule` knows (L3LINT-CTX-001, L3LINT-ENGINE-001), plus `docs/rules/index.md` (grouped by pack: linked ID, title, severity, level, confidence, effective dates) and `docs/rules/toc.yml`. It checks that every rule has a title, requirement, message, fix and at least one source.
- `cli` builds the System.CommandLine tree the way `CliApplication.RunAsync` does and writes `docs/reference/cli.md`: every command, subcommand, argument and option with description, aliases, value, default and allowed values. To catch drift between the duplicated tree and `CliApplication`, it runs `--help` through `CliApplication.RunAsync` for every command and reports a problem when the printed commands or options differ. The help names `profile|path` and `rule|glob` are placeholders, not value lists; `CliDocs.Value` describes them in words (the profile names come from `ProcessorProfiles.All`). A new option whose help name has a `|` but is not a value list needs an entry there.
- `schemas` writes `docs/reference/results.md`, `config.md` and `canonical.md` (every property with type, required flag, default, constraints and description; nested objects and `$defs` objects as their own tables) and copies all eight `schemas/*.schema.json` to `docs/schemas/`, so the `$id` URLs resolve on the site.
- `--check` regenerates in memory, writes nothing, and exits 1 when a generated file is stale or missing, when a file in `docs/rules/` or `docs/schemas/` is no longer generated, or when a rule, command, argument, option or schema property has no description (NFR-014). Without `--check` it writes the files, deletes files that are no longer generated, and still exits 1 on documentation problems.

### DocFX site

`docs/docfx.json` (conceptual only, modern template, no API metadata), `docs/index.md`, `docs/toc.yml`, `docs/articles/toc.yml`, `docs/reference/toc.yml`. Output goes to `docs/_site`. The build includes `docs/dev/verification*.md` so `limitations.md` can link the adapter verification record; a future `docs/dev/verification.md` (WP18) is published too and must build without warnings.

### Articles (18)

getting-started, installation, cli, configuration, output-formats, rules, likely-tiers, adapters (existing, two fixes), csv-input, processor-profiles, fixes, cost-estimates, mcp-server, github-action, library, rule-pack-authoring, privacy-and-security, limitations.

Every l3lint command in the articles was run against the CLI built from this tree (`dotnet src/L3Lint.Cli/bin/Debug/net10.0/l3lint.dll` through a wrapper named `l3lint`), from the repository root unless the article says otherwise. Printed output is real; where it is shortened, the article says so. Paths in two configuration examples are shortened to `C:\...\cfg\`. Payloads without a transaction date show the run date (2026-10-30) as their as-of date. Other runs: the MCP server over Streamable HTTP with curl (401 without the token, 403 for a foreign Origin and for a foreign Host, `isError` results for an unknown rule, bad JSON and a bad brand), the Action's `scripts/run.sh` against a copy of `samples/action-example`, a scratch console project for every C# snippet in `library.md` (including a custom check named from a local pack), and a local rule pack built with `tools/L3Lint.PackManifest`.

Fixes to `docs/articles/adapters.md`: the Braintree currency note now names `--currency`, `inputs.defaultCurrency` and the MCP `context.currency` (ADR-0056), and the CSV section now says `pci.allowFields` applies to CSV columns through the map (checked with fixture `05-card-number-in-cell`).

### Root and package files

`README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CHANGELOG.md`, `LICENSE`, `NOTICE`, and a `README.md` for each packable `src` project that had none (Core, Hosting, Reporting, Adapters, Cli, the five Rules packages). `dotnet pack src/L3Lint.Cli` failed before these existed (NU5019 for LICENSE, NU5039 for README) and succeeds now.

- `LICENSE` is byte-identical to https://www.apache.org/licenses/LICENSE-2.0.txt (SHA-256 `cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30`).
- `CODE_OF_CONDUCT.md` is the Contributor Covenant 2.1 Markdown from the official site. The only change is the contact method (a private report through GitHub security advisories, or the issue tracker for matters that are not sensitive), plus a CC BY 4.0 attribution line.
- `NOTICE` lists the bundled data files with their sources (ISO 4217 table, ISO 3166 table, the Amex X12 unit list, the Mastercard unit list, the MCC lists, the placeholder list), the third-party libraries in the tool and server with the licenses from their NuGet metadata, and the two test-only schemas that ship in no package.
- `CHANGELOG.md` follows Keep a Changelog 1.1.0 with a `1.0.0 - Unreleased` entry.

## Changes outside the owned paths

1. `src/L3Lint.Cli/L3Lint.Cli.csproj`: one line, `<InternalsVisibleTo Include="L3Lint.DocsGen" />`.
2. `schemas/config.schema.json`, `schemas/results.schema.json`, `schemas/canonical.schema.json`: a `description` on every property (170 were missing), so the generated references are complete and `--check` can enforce NFR-014. `config.schema.json` also gained `default` annotations for keys with a fixed default (they do not change validation). The root description of `results.schema.json` was reworded to drop the word "qualifies". The Core, Cli, Mcp and Reporting tests that read these files still pass. `docs/schemas/` holds copies, so the lead must merge the schemas and the copies together (`DocsGen --check` reports a mismatch).
3. `L3Lint.slnx`: the DocsGen project added under `/tools/` (requested by the brief).

The `packages.lock.json` files under `src/` were rewritten by restores during the work but their content is identical to the main repository.

## Not done, and why

- `dotnet tool install --global l3lint`, `dnx L3Lint.Mcp`, a GitHub-hosted run of the Action and the GitHub Pages deployment need published packages or pushes, which the brief forbids. The install path was checked with a locally packed package (`--add-source`, `--tool-path` and a local tool manifest); the Action script was run locally.
- Single-file publishing was run for `win-x64` and `linux-x64` only (the win-x64 binary was run). The other four runtime identifiers use the same command and were not built.
- No API reference is generated from assemblies; the site is conceptual, as the brief allows.

## Findings for the lead

- The `IRule` XML documentation example in `src/L3Lint.Core/Rules/IRule.cs` uses the rule ID `ACME-PO-001`. `results.schema.json` only accepts IDs that start with `GEN`, `VISA`, `MC`, `AMEX`, `DISC` or `L3LINT`, so JSON output with that rule would not validate. `library.md` advises a `GEN-` prefix. Either widen the schema pattern or change the examples.
- `src/L3Lint.Mcp/README.md` (WP11) says findings "never say a transaction qualifies for a rate"; the wording rule avoids that word even in a negation.
- With `--only` or `--disable`, the likely tier only counts the rules that ran, so a narrowed run can report "meets the documented requirement". `cli.md` and `likely-tiers.md` say so.
- The release workflow (WP16) should run `L3Lint.DocsGen -- all --check` and the docfx build, and step 7 publishes `docs/_site`.

## Pages consulted

- https://www.apache.org/licenses/LICENSE-2.0.txt (license text)
- https://www.contributor-covenant.org/version/2/1/code_of_conduct/ and https://www.contributor-covenant.org/version/2/1/code_of_conduct/code_of_conduct.md (Code of Conduct text)
- https://www.contributor-covenant.org/, https://www.contributor-covenant.org/faq/ and https://www.contributor-covenant.org/adopt/ (looking for the license statement; none of them states it)
- https://raw.githubusercontent.com/EthicalSource/contributor_covenant/2.1/LICENSE.md (the Covenant's CC BY 4.0 license at the 2.1 tag)
- A web search for the Contributor Covenant 2.1 license, which pointed to the project homepage
- https://keepachangelog.com/en/1.1.0/ (changelog format)
- Library licenses were read from the `.nuspec` files in the local NuGet cache, not from the web.
