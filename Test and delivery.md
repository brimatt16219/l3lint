# 5. Test and delivery plan

## Test strategy

Twelve test layers, each with a gate in CI. The fixture corpus and the property tests carry most of the weight, because a linter's value is entirely in being right.

| Layer | What it proves | Tooling | Gate |
| --- | --- | --- | --- |
| Unit | Money, rounding, each check kind, each detector, each custom check | xUnit v3, Shouldly | 90% line coverage in Core and rule packs |
| Rule examples | Every rule's pass example passes and fail example fails | `l3lint rules test` | All 78 rules, every PR |
| Property-based | Generated valid transactions pass every arithmetic rule; perturbing one amount beyond tolerance fails exactly the expected rule; results do not depend on currency exponent or rounding mode beyond the documented effect | FsCheck, 1,000 cases per property | Every PR |
| Golden corpus | Each fixture yields exactly its expected findings | Custom runner plus Verify snapshots | Recall at least 95%, false positives at most 2% |
| Determinism | Two runs, 1 and 8 threads, produce byte-identical JSON and SARIF | Runner | Every PR |
| Adapter contracts | Published gateway examples map to the expected canonical payload and path map | xUnit with recorded samples | Every PR |
| Output conformance | SARIF validates against the SARIF 2.1.0 schema and GitHub limits; JSON against `results.schema.json`; JUnit against its XSD | JsonSchema.Net, XSD validator | Every PR |
| CLI end to end | Exit codes, stdin, globs, `NO_COLOR`, config discovery, annotations | Process-level tests | Every PR, three operating systems |
| MCP | Each tool through an in-memory MCP client; error results come back with `isError`; Inspector smoke test | SDK test client, MCP Inspector | Every PR |
| GitHub Action | The Action runs green and red in a sample repository | Real workflow in `l3lint-action-example` | Every Action change |
| Security | Fuzzed parsers and adapters; card numbers masked in every output; no network traffic | SharpFuzz, network-namespace test | Nightly fuzz, every PR for the rest |
| Performance | Latency and throughput targets; no regression over 15% | BenchmarkDotNet | Every PR (short run), release (full run) |

Two more checks run less often. **Mutation testing** with Stryker.NET on Core and the rule packs must keep a mutation score of at least 80%, checked weekly. **Agent evaluation** runs nightly: an AI agent receives a seeded-defect payload and the MCP server, and the harness measures how many defects it fixes within two tool calls (target 90%).

## Fixture corpus

The corpus is about 830 small, synthetic files with expectations next to them. It is the single source of truth for "does l3lint behave correctly", and every bug report should end as a new fixture.

**Layout**

```text
fixtures/
  canonical/
    visa/valid/*.json          visa/invalid/*.json
    mastercard/valid/*.json    mastercard/invalid/*.json
    amex/ ...  discover/ ...  general/ ...
  adapters/
    stripe/*.json  adyen/*.json  braintree/*.json
    cybersource/*.json  authorizenet/*.json  checkoutcom/*.json
    csv/*.csv  csv/*.map.yaml
  seeded/                      generated defects, committed as files
  known-good/                  real-shaped, fully valid payloads
  pci/                         payloads carrying public test card numbers only
```

Each payload has a sibling `*.expect.yaml`:

```yaml
description: Line 2 is missing its commodity code
context: { brand: visa, cardProduct: purchasing, mcc: "5047", asOf: 2026-10-19 }
expect:
  exitCode: 1
  findings:
    - { ruleId: VISA-L3-004, path: /lineItems/1/commodityCode, severity: error }
  absent: [GEN-ARITH-001, GEN-ARITH-002]   # must not fire
  likelyTier: { brand: visa, status: blocked }
```

**Size targets**

| Set | Count | How it is built |
| --- | --- | --- |
| Rule examples | 156 (one pass and one fail per rule) | Hand-written with each rule |
| Adapter fixtures | 70 (10 per adapter) | From published gateway examples, then varied |
| Seeded defects | 400 | 40 valid base payloads times 10 mutation operators, generated once with a fixed seed |
| Known-good | 200 | Varied valid payloads across brands, products, currencies and line counts |
| PCI | 10 | Every output format, with public test card numbers such as 4111 1111 1111 1111 |

**The ten mutation operators** used for the seeded set: drop a required field; zero a required field; replace a description with a placeholder; swap in an unlisted unit; shift a line total by one cent beyond tolerance; apply a discount at both levels; push the tax ratio outside its range; inject a test card number into a free-text field; overflow a field length; exceed a line-item limit.

**Rules for fixtures.** All data is synthetic. No real customer, merchant or employer data, ever. Card numbers come only from public test ranges. Every fixture states its context explicitly, so a change to defaults cannot silently change what it tests.

## CI/CD, packaging and release channels

Every pull request runs the full gate on three operating systems; every `v*` tag publishes signed packages to five channels from one workflow.

**Pull request workflow** (`ci.yml`)

1. Restore with locked dependencies; build with warnings as errors on ubuntu, windows and macos.
2. Run unit, rule example, property, corpus, determinism, adapter, conformance, CLI and MCP tests.
3. Enforce coverage (90% in Core and packs) and the recall and false-positive targets.
4. Run the short benchmark and compare against the last release baseline.
5. Lint the repository's own fixtures with the freshly built Action and upload SARIF (the tool checks itself).
6. Build the docs site and fail on any rule without a reference page.

**Release workflow** (`release.yml`, on tag `vX.Y.Z`)

1. Build and test as above, then pack: the `l3lint` .NET tool, `L3Lint` library packages, five rule pack packages, and `L3Lint.Mcp` with `PackageType` `McpServer`.
2. Publish ReadyToRun single-file binaries for win-x64, win-arm64, osx-x64, osx-arm64, linux-x64 and linux-arm64, with SHA-256 checksums.
3. Sign packages, generate a CycloneDX SBOM, and create GitHub artifact attestations.
4. Push to NuGet.org; wait for package validation to finish.
5. Run `mcp-publisher publish .mcp/server.json` for the official MCP Registry.
6. Update the Action repository's `VERSION` file, tag `vX.Y.Z`, and move the major tag (`v1`).
7. Publish the docs site to GitHub Pages and the changelog to the GitHub release.

**Release channels**

| Channel | Artifact | How users get it |
| --- | --- | --- |
| NuGet .NET tool | `l3lint` | `dotnet tool install -g l3lint` or `dnx l3lint` |
| GitHub Releases | Single-file binaries | Download and run, no .NET needed |
| NuGet libraries | `L3Lint`, `L3Lint.Core`, rule packs | `dotnet add package L3Lint` |
| NuGet MCP server and MCP Registry | `L3Lint.Mcp` | `dnx L3Lint.Mcp` from any MCP client config |
| GitHub Marketplace | `l3lint-action` | `uses: <owner>/l3lint-action@v1` |

**Versioning.** The tool, library and MCP server share one semantic version. Rule packs version independently (see Architecture) and can ship between tool releases. Brand sources get a review every April and October; a pack patch ships out of band whenever a source changes.

## Milestones

The MVP gate comes after 42.5 focused days; v1.0 is ready after 80.5, and launch takes 3.5 more.

&#91;embedded content: l3lint roadmap · 6 phases, 2 release gates\]

Effort is in focused days from the backlog below, so calendar time depends on hours per week. Phases can overlap once their dependencies land; the order shown is the critical path.

## Backlog

Fifty tasks, 84 focused days: about 17 weeks full time, or about 10 months at two focused days a week. Estimates are for one experienced .NET developer; tasks list the requirements they satisfy so nothing in the PRD is orphaned.

| ID | Task | Milestone | Days | Depends on | Done when | Satisfies | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| T-01 | Check the name on NuGet, GitHub and the MCP Registry; reserve it | M0 Foundations | 0.5 | None | Final name reserved everywhere | Overview | Not started |
| T-02 | Get written employer clearance for the project | M0 Foundations | 0.5 | None | Written approval on file | D2, R4 | Not started |
| T-03 | Scaffold repository, solution, build props, central packages, license, contributing guide | M0 Foundations | 1 | T-01 | Empty solution builds in CI | NFR-013 | Not started |
| T-04 | CI workflow: build and test matrix, coverage gate | M0 Foundations | 1 | T-03 | Green on three operating systems | NFR-004 | Not started |
| T-05 | JSON Schemas: canonical, results, config, rule pack | M0 Foundations | 2 | T-03 | Schemas validate the examples in this spec | FR-002, FR-022 | Not started |
| T-06 | Canonical model, Money, Currency, ISO 4217 table | M0 Foundations | 2 | T-05 | Model round-trips every canonical fixture | D9 | Not started |
| T-07 | Reader: JSON, NDJSON, stdin, limits, line and column index | M1 MVP | 2 | T-06 | Limit and streaming tests pass | FR-001, FR-034 | Not started |
| T-08 | Card-number scrub and masking | M1 MVP | 1.5 | T-07 | PCI fixtures masked in every output | FR-033, US-12 | Not started |
| T-09 | Schema validation stage producing `GEN-FMT` findings | M1 MVP | 1 | T-05, T-07 | Schema errors carry correct paths | FR-002 | Not started |
| T-10 | Normalize stage and derived facts | M1 MVP | 1 | T-06 | Derived values match hand-computed cases | FR-015 | Not started |
| T-11 | Rule engine: metadata, loader, checksum, effective dates, selection | M1 MVP | 3 | T-06 | `--as-of` selects the right rules in tests | FR-010, FR-012, FR-019 | Not started |
| T-12 | Eight declarative check kinds | M1 MVP | 2 | T-11 | Each kind has unit tests for pass, fail and absent | FR-012 | Not started |
| T-13 | Arithmetic checks `GEN-ARITH-001` to `005` with property tests | M1 MVP | 2 | T-10, T-11 | Properties hold over 1,000 cases each | FR-015 | Not started |
| T-14 | Data quality detectors | M1 MVP | 2 | T-10 | Detector unit tests pass | FR-016 | Not started |
| T-15 | GEN rule pack, 23 rules with examples | M1 MVP | 2 | T-12, T-13, T-14 | `rules test` passes for GEN | FR-012 | Not started |
| T-16 | VISA rule pack, 26 rules with examples | M1 MVP | 3 | T-15 | `rules test` passes for VISA | FR-013 | Not started |
| T-17 | MC rule pack, 17 rules, unit-of-measure list | M1 MVP | 2 | T-15 | `rules test` passes for MC | FR-013 | Not started |
| T-18 | Adapters: Stripe, Adyen, Braintree, with detection and path maps | M1 MVP | 4 | T-06 | Adapter contract tests pass | FR-003, FR-007, FR-008 | Not started |
| T-19 | Text and JSON writers, exit codes | M1 MVP | 2 | T-11 | Snapshot tests pass | FR-021, FR-022, FR-025 | Not started |
| T-20 | CLI: `lint`, `explain`, `rules list` and `show`, `version`, config loading | M1 MVP | 3 | T-19 | CLI end-to-end tests pass | FR-009, FR-018, FR-026 | Not started |
| T-21 | Library: Hosting, `ILinter`, `LintManyAsync` | M1 MVP | 1.5 | T-11 | Thread-safety and latency tests pass | FR-031, US-09 | Not started |
| T-22 | Fixture corpus v1 | M1 MVP | 3 | T-16, T-17, T-18 | Recall and precision targets met | Success metrics | Not started |
| T-23 | MVP prerelease (0.1.0) | M1 MVP | 0.5 | T-20, T-21, T-22 | MVP release gate met | Scope | Not started |
| T-24 | Adapters: Cybersource, Authorize.net, Checkout.com; confirm every asterisked field | M2 Adapters and outputs | 3 | T-18 | Contract tests pass; no asterisks left | FR-004 | Not started |
| T-25 | CSV adapter and mapping schema | M2 Adapters and outputs | 2 | T-18 | CSV fixtures pass | FR-006, US-14 | Not started |
| T-26 | Format auto-detection with tie handling | M2 Adapters and outputs | 1 | T-24 | 99% detection accuracy on fixtures | FR-005 | Not started |
| T-27 | Processor profiles | M2 Adapters and outputs | 1.5 | T-11 | Profile limits change findings as specified | FR-011 | Not started |
| T-28 | SARIF writer with GitHub limits | M2 Adapters and outputs | 2 | T-19 | Schema-valid; upload succeeds in sample repo | FR-023 | Not started |
| T-29 | JUnit and Markdown writers | M2 Adapters and outputs | 1 | T-19 | XSD-valid; snapshot tests pass | FR-024 | Not started |
| T-30 | `fix` command, JSON Patch, unit mapping | M2 Adapters and outputs | 2.5 | T-18 | Patched fixtures lint clean | FR-027, US-06 | Not started |
| T-31 | Cost estimates, rate table schema, tier rollup | M2 Adapters and outputs | 2 | T-19 | Estimates match hand calculations | FR-028, US-10 | Not started |
| T-32 | `convert` and `schema` commands | M2 Adapters and outputs | 0.5 | T-18 | CLI tests pass | FR-001 | Not started |
| T-33 | MCP server: six tools, resources, prompts over stdio | M3 Surfaces | 3 | T-21 | SDK client tests pass | FR-029, US-08 | Not started |
| T-34 | MCP Streamable HTTP with Origin check and token | M3 Surfaces | 1 | T-33 | Rejects foreign origins in tests | FR-035 | Not started |
| T-35 | MCP packaging: `McpServer` type, `server.json`, `dnx` run, Inspector check | M3 Surfaces | 1 | T-33 | Launches from a clean machine with `dnx` | FR-029 | Not started |
| T-36 | GitHub Action, annotations, job summary, sample repository | M3 Surfaces | 2.5 | T-28 | Green and red runs behave as specified | FR-030, US-07 | Not started |
| T-37 | `init` command | M3 Surfaces | 0.5 | T-20 | Writes valid config and samples | FR-032 | Not started |
| T-38 | Rule reference generator and docs site | M3 Surfaces | 2 | T-16, T-17 | Every rule has a page | FR-036 | Not started |
| T-39 | AMEX rule pack, 9 rules, X12 unit list | M4 Brands and hardening | 1.5 | T-15 | `rules test` passes for AMEX | FR-014 | Not started |
| T-40 | DISC rule pack, 3 rules | M4 Brands and hardening | 0.5 | T-15 | `rules test` passes for DISC | FR-014 | Not started |
| T-41 | Batch heuristic `GEN-DQ-007` and threshold experiments | M4 Brands and hardening | 1.5 | T-22 | Threshold chosen with data; open question closed | FR-017 | Not started |
| T-42 | `rules test` command | M4 Brands and hardening | 1 | T-11 | Runs every pack's examples | FR-020 | Not started |
| T-43 | Fuzzing harness and fixes | M4 Brands and hardening | 2 | T-24, T-25 | 24 hours of fuzzing without a crash | NFR-006 | Not started |
| T-44 | Benchmarks and tuning to targets | M4 Brands and hardening | 2 | T-22 | Latency and throughput targets met | NFR-002, NFR-003 | Not started |
| T-45 | Security review: threat walk-through, network-deny and log-scan tests | M4 Brands and hardening | 1 | T-33 | All threats in Architecture have a passing test | NFR-005, NFR-007 | Not started |
| T-46 | Close the "facts to verify" list in Card brand rules | M4 Brands and hardening | 1 | T-16, T-17 | Every item resolved or documented as unknown | R1, R2 | Not started |
| T-47 | Agent evaluation harness, nightly | M4 Brands and hardening | 2 | T-33 | Repair rate measured and reported | Success metrics | Not started |
| T-48 | Release pipeline: signing, SBOM, attestations, binaries, NuGet, MCP Registry | M5 Launch | 2 | T-35, T-36 | A dry-run release completes | NFR-008 | Not started |
| T-49 | v1.0 release, Action `v1` tag, docs publish | M5 Launch | 0.5 | T-48 | v1.0 release gate met | Scope | Not started |
| T-50 | Launch write-up, sample repository, feedback channel | M5 Launch | 1 | T-49 | Published and linked from the README | Adoption metric | Not started |

## Definition of done and launch checklist

A task is done only when its tests, docs and fixtures land with it; a release ships only when every box below is ticked.

**Definition of done for every task**

- Code merged with tests at the layers the Test strategy table names for it.
- Coverage, recall, false-positive and benchmark gates still green.
- Any new rule has metadata, sources, confidence, effective dates, a pass example, a fail example and a reference page.
- Any new option or output field is in the schema, the docs and the `--help` text.
- User-facing messages use plain words, stay under 25 words per sentence, and state the fix as an action.
- The task's "Done when" condition in the backlog is demonstrably true.

**v1.0 launch checklist**

- [ ] Written employer clearance on file (T-02).
- [ ] Final name reserved on NuGet, GitHub and the MCP Registry (T-01).
- [ ] All 14 user stories pass their acceptance tests.
- [ ] Recall at least 95% and false positives at most 2% on the corpus.
- [ ] Every item on the "facts to verify" list resolved or documented.
- [ ] Every adapter field marked with an asterisk confirmed against current gateway docs.
- [ ] Security review complete; network-deny and log-scan tests green.
- [ ] 24 hours of fuzzing without a crash.
- [ ] Signed packages, SBOM and attestations produced by a dry-run release.
- [ ] MCP server launches with `dnx` on a clean machine and passes the Inspector check.
- [ ] The Action runs green and red in the public sample repository.
- [ ] Rule reference site published; README links the spec's limits and the "not a guarantee" statement.
- [ ] Changelog written; feedback channel open.
