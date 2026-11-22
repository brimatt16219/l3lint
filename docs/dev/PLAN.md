# l3lint v1.0 build plan

This plan turns SPEC-DIGEST.md into work packages. Each package lists the spec IDs it covers, the files it owns, what it depends on and the tests that define done. Execution is in waves; after each wave the whole solution builds, every test runs and PROGRESS.md is updated.

## Ground rules

- Hard constraints from the build directive apply to every package: no git writes, no publishing, public sources only, no Fiserv material, offline runtime, card numbers masked first, deterministic output, no shortcuts, non-circular tests, plain writing with no em-dashes or en-dashes.
- Core's public API is frozen at the end of wave 1. Later packages extend Core only through small, reviewed additions.
- Subagents build only their own projects (`dotnet build <project> -p:BuildProjectReferences=false` after the lead has built the solution) so parallel builds do not fight over shared `obj` folders.

## Work packages

| WP | Name | Spec IDs | Owns | Depends on | Done when |
| --- | --- | --- | --- | --- | --- |
| WP0 | Digest and plan | all | `docs/dev/*`, `docs/adr/0001` to `0045` | none | every ID in the digest; ADRs written |
| WP1 | Foundations | T-03, T-05, T-06, NFR-013, D9, FR-008 | root build files, `L3Lint.sln(x)`, project skeletons, `schemas/*`, `src/L3Lint.Core/Model/*`, `Currencies/*` | WP0 | clean build; schemas validate every spec example; model round-trips the example payload |
| WP2 | Engine | T-07 to T-14, FR-001, FR-002, FR-010, FR-012, FR-015, FR-016, FR-019, FR-033, FR-034, NFR-001, NFR-006, NFR-007 | `src/L3Lint.Core/**` (reader, scrub, schema stage, canonical mapper, normalize, engine, checks, detectors, arithmetic, tiers, fixes, estimates) | WP1 | unit tests per stage; property tests for arithmetic |
| WP3 | GEN, VISA, MC packs | T-15, T-16, T-17, FR-013 | `src/L3Lint.Rules.General|Visa|Mastercard/**`, example definitions in `tools/L3Lint.FixtureGen/Examples` | WP2 | `rules test` passes for the three packs; spec example gives exactly one VISA-L3-010 warning and fails MC-L3-005 |
| WP4 | Text and JSON writers | T-19, FR-021, FR-022, FR-025, NFR-010, NFR-011 | `src/L3Lint.Reporting/{Text,Json,Abstractions}/**` | WP2 | snapshot tests; JSON validates against results schema |
| WP5 | Hosting | FR-018, FR-031, US-09, T-21 | `src/L3Lint.Hosting/**` | WP2, WP4 | DI test, config tests, thread-safety and latency tests |
| WP6 | CLI basics | T-20, FR-009, FR-026, US-01, US-03, US-05, US-13 | `src/L3Lint.Cli/**` | WP5 | CLI end-to-end tests for lint, explain, rules list and show, version |
| WP7 | Adapters and detection | T-18, T-24, T-25, T-26, FR-003 to FR-008, US-02, US-14 | `src/L3Lint.Adapters/**`, `tests/L3Lint.Adapters.Tests/**`, `fixtures/adapters/**` | WP2 | contract tests; detection at least 99% on adapter fixtures |
| WP8 | SARIF, JUnit, Markdown | T-28, T-29, FR-023, FR-024 | `src/L3Lint.Reporting/{Sarif,JUnit,Markdown}/**`, `tests/L3Lint.Reporting.Tests/**` | WP4 | SARIF schema-valid, JUnit XSD-valid, snapshots |
| WP9 | Fix, cost, convert, schema, init, profiles | T-27, T-30, T-31, T-32, T-37, FR-011, FR-027, FR-028, FR-032, US-06, US-10 | `src/L3Lint.Core/{Fixes,Estimates,Profiles}/**`, CLI commands | WP6, WP7 | patched fixtures lint clean; estimates match hand calculations |
| WP10 | AMEX, DISC, batch, rules test | T-39 to T-42, FR-014, FR-017, FR-020 | `src/L3Lint.Rules.Amex|Discover/**` | WP3 | `l3lint rules test` passes for all 78 rules |
| WP11 | MCP server | T-33 to T-35, FR-029, FR-035, US-08 | `src/L3Lint.Mcp/**`, `tests/L3Lint.Mcp.Tests/**` | WP5 | SDK client tests over stdio and HTTP; packed server launches |
| WP12 | GitHub Action | T-36, FR-030, US-07 | `action/**`, `samples/action-example/**` | WP8 | local `run.sh` run gives expected outputs for green and red |
| WP13 | Corpus | T-22, success metrics | `tools/L3Lint.FixtureGen/**`, `fixtures/{canonical,seeded,known-good,pci}/**`, corpus runner tests | WP3, WP7, WP10 | recall at least 95%, false positives at most 2%, every rule example correct |
| WP14 | Quality harnesses | T-43, T-44, T-45, NFR-002, NFR-003, NFR-005, NFR-006 | `tests/L3Lint.Benchmarks/**`, `tests/L3Lint.Fuzz/**`, `tests/L3Lint.Architecture.Tests/**`, `stryker-config.json`, security tests | WP6 | benchmarks recorded; fuzz smoke; network and log tests |
| WP15 | Docs | T-38, FR-036, NFR-014 | `tools/L3Lint.DocsGen/**`, `docs/**` (site), root docs | WP10 | DocFX zero warnings; every rule has a page |
| WP16 | Delivery | T-48 (dry run), NFR-004, NFR-008 | `.github/workflows/**`, `tools/L3Lint.PackManifest/**`, packaging | WP11, WP12 | packing into `./artifacts/` succeeds; SBOM generated |
| WP17 | Agent eval | T-47 | `tools/L3Lint.AgentEval/**` | WP11 | builds; exits cleanly without a key |
| WP18 | Final audit | all | `docs/dev/IMPLEMENTATION_REPORT.md`, `traceability.md`, `verification.md` | all | every gate green; report written |

## Waves

1. Wave 1 (lead, sequential): WP1, WP2, WP3, WP4, WP5, WP6. Research agents run in parallel for `verification.md` inputs.
2. Wave 2 (parallel): WP7 (subagent), WP8 (subagent), WP9 and WP10 (lead).
3. Wave 3 (parallel): WP11 (subagent), WP12 (subagent), WP13 (lead), WP14 (subagent).
4. Wave 4 (parallel): WP15 (subagents per docs area), WP16 and WP17 (lead).
5. Wave 5: WP18 with a fresh reviewer subagent, then fixes.

## Phase exit checks (directive section 6)

| Phase | Exit criteria | Packages |
| --- | --- | --- |
| 0 | every ID in digest | WP0 |
| 1 | clean build; schemas validate spec examples; model round-trips example | WP1 |
| 2 | US-01, 03, 05, 09, 12, 13 end to end; example gives one VISA-L3-010 and fails MC-L3-005; worked example reproduces | WP2 to WP6 |
| 3 | adapter contract tests; detection at least 99% | WP7 |
| 4 | SARIF and JUnit schema-valid; patched fixtures lint clean | WP8, WP9 |
| 5 | `rules test` passes for 78 rules | WP10 |
| 6 | MCP client tests over both transports; server launches from local package | WP11 |
| 7 | local Action run correct for green and red | WP12 |
| 8 | all section 8 gates | WP13, WP14 |
| 9 | docs zero warnings; public API documented; packing succeeds | WP15, WP16 |
| 10 | audit gaps fixed; report written | WP18 |
