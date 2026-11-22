# l3lint v1.0 implementation report

Date: 2026-11-06. This report closes the v1.0 build directive. It covers what was built, every quality gate with its numbers, every ADR and spec deviation, the facts that stay unconfirmed, and what the owner does next. Paths are relative to the repository root. Nothing was committed, pushed, tagged or published; packages exist only in `./artifacts/`.

## 1. What was built

### Shipped projects

| Project | What it is | C# files | All files |
| --- | --- | --- | --- |
| src/L3Lint.Core | Engine: reader and limits, card-number scrub, canonical model, schema validation, rule loading and compilation, eight declarative check kinds, tiers, cost estimates, fixes, processor profiles | 178 | 195 |
| src/L3Lint.Rules.General | GEN pack: 23 rules, lists (placeholders, ISO 4217) and its custom checks | 20 | 74 |
| src/L3Lint.Rules.Visa | VISA pack: 26 rules, MCC lists, tier ladder | 6 | 69 |
| src/L3Lint.Rules.Mastercard | MC pack: 17 rules, unit and MCC lists | 4 | 49 |
| src/L3Lint.Rules.Amex | AMEX pack: 9 rules, 720 unit codes | 2 | 28 |
| src/L3Lint.Rules.Discover | DISC pack: 3 rules | 1 | 14 |
| src/L3Lint.Adapters | Stripe, Adyen, Braintree, Cybersource, Authorize.net, Checkout.com and CSV adapters with detection | 29 | 36 |
| src/L3Lint.Reporting | Text, JSON, SARIF 2.1.0, JUnit and Markdown writers; fix patches; mapping JSON; rule pages | 19 | 25 |
| src/L3Lint.Hosting | `AddL3Lint()`, configuration, settings resolution, batch runner, fix planner (meta-package `L3Lint.Hosting`, ADR-0031) | 20 | 26 |
| src/L3Lint.Cli | `l3lint` tool: lint, explain, fix, convert, rules list, rules show, rules test, schema, init, version | 20 | 27 |
| src/L3Lint.Mcp | `l3lint-mcp`: six tools, three resource templates, two prompts, over stdio and Streamable HTTP | 12 | 21 |

All 78 rules exist with metadata, sources, confidence, effective dates, pass and fail examples, and a generated page under `docs/rules/` (41 errors, 24 warnings, 13 information).

### Tests

| Project | Tests | What it covers |
| --- | --- | --- |
| tests/L3Lint.Adapters.Tests | 749 | Detection and mapping for 70 gateway fixtures, every mapped field, path maps |
| tests/L3Lint.Architecture.Tests | 107 | Dependency direction, no networking or telemetry, public API files, no open-work markers or dashes, pinned actions, advisory wording |
| tests/L3Lint.Cli.Tests | 104 | Every command and option end to end, the Action script on green and red samples, NDJSON reader, deferred logger |
| tests/L3Lint.Core.Tests | 191 | Reader, limits, YAML, schemas and their messages, the tolerant canonical reader, card numbers, packs, property tests (money, pointers, JSON Patch, canonical JSON) |
| tests/L3Lint.Corpus.Tests | 176 | 156 rule examples, 400 seeded defects, 200 known-good, 10 PCI payloads, determinism, compressed result store |
| tests/L3Lint.Fuzz.Tests | 11 | 17,000 seeded mutated inputs over four targets |
| tests/L3Lint.Mcp.Tests | 40 | Every tool, resource and prompt over stdio and HTTP with the SDK client; origin, host and token checks |
| tests/L3Lint.Reporting.Tests | 110 | Writer snapshots, SARIF, JSON and JUnit schema conformance, determinism, truncation |
| tests/L3Lint.Rules.Tests | 126 | Rule checks, the full finding of each GEN custom check, GEN-DQ-007 over a run, tiers, library API, line arithmetic properties |
| tests/L3Lint.Security.Tests | 66 | Card numbers in every output, log privacy, YAML and JSON hardening, path traversal, runtime network probe |
| tests/L3Lint.Performance | 2 | Latency targets, run alone in Release (ADR-0058) |

`dotnet test --solution L3Lint.slnx -c Release` runs 1,680 tests: 1,680 passed, 0 failed, 0 skipped. The two timing tests pass when run alone.

### Tools, docs and delivery

- tools/L3Lint.FixtureGen (rule examples, samples, the seeded corpus), tools/L3Lint.PackManifest (pack hashes), tools/L3Lint.DocsGen (94 generated docs files), tools/L3Lint.AgentEval (agent evaluation over MCP; built, not run, because it spends API tokens).
- tests/L3Lint.Benchmarks (BenchmarkDotNet and a memory probe), tests/L3Lint.Fuzz (SharpFuzz harness), `stryker-config.json` (Core) and `stryker-packs.json` (rule packs).
- DocFX site in `docs/`: 18 articles, 78 rule pages, CLI, results, config and canonical references, schemas.
- Root files: README, CONTRIBUTING, SECURITY, CODE_OF_CONDUCT, CHANGELOG, LICENSE (Apache-2.0), NOTICE, `.editorconfig`, `.gitattributes` (LF), `.gitignore`.
- Workflows `ci.yml`, `release.yml`, `docs.yml` and the weekly `mutation.yml` (authored, never run); the composite Action in `action/` and `samples/action-example/`.
- 61 ADRs in `docs/adr/`; working records in `docs/dev/` (SPEC-DIGEST, PLAN, PROGRESS, traceability, verification, dependencies, benchmarks, coverage, security review, cli-options, mutation testing, packaging dry run).

## 2. Gate results

| Gate | Result |
| --- | --- |
| 1. Release build | `dotnet build L3Lint.slnx -c Release`: 0 warnings, 0 errors |
| 2. Format | `dotnet format L3Lint.slnx --verify-no-changes`: exit 0 |
| 3. Tests and coverage | 1,680 of 1,680 pass, 0 skipped. Line coverage in section 2.1 |
| 4. Corpus | Recall 100% (420 of 420 seeded defects found; target 95%). False positives 0 of 200 known-good payloads (target at most 2%). All 156 rule examples correct |
| 5. Determinism | Byte-identical JSON and SARIF across two runs and across 1 and 8 workers over 600 payloads; every writer byte-identical from compressed results (ADR-0059) |
| 6. Architecture | Dependency direction holds; no `System.Net.Http` or other networking in Core, packs, Adapters, Reporting, Hosting or Cli; no telemetry packages in any shipped closure |
| 7. Conformance | JSON validates against `results.schema.json`, SARIF against the vendored OASIS 2.1.0 schema, JUnit against the Jenkins xUnit XSD |
| 8. Docs | DocFX with `--warningsAsErrors`: 0 warnings, 0 errors; 78 rule pages; `DocsGen all --check`: 94 files current. All five README quick-start commands give exactly the documented output (checked on the final binary) |
| 9. Benchmarks | Section 2.2 and docs/dev/benchmarks.md |
| 10. CLI commands and options | Every command and option, short aliases included, runs in an end-to-end test (docs/dev/cli-options.md) |
| 11. MCP | All six tools over stdio and over HTTP through the SDK client, plus resources, prompts and the HTTP safeguards |
| 12. Action | `ActionScriptTests` run `action/scripts/run.sh` against the built CLI: the green sample passes with SARIF, summary and counts; the red sample reports exit 1 and annotates the file |

### 2.1 Coverage

Line coverage over every test project (docs/dev/coverage.md); the gate is 90% for Core and each pack:

| Assembly | Line | Branch |
| --- | --- | --- |
| L3Lint.Core | 93.5% | 83.7% |
| L3Lint.Rules.General | 99.7% | 93.6% |
| L3Lint.Rules.Visa | 93.7% | 90.3% |
| L3Lint.Rules.Mastercard | 97.7% | 88.7% |
| L3Lint.Rules.Amex | 100% | 100% |
| L3Lint.Rules.Discover | 100% | no branches |
| L3Lint.Adapters | 96.7% | 90.7% |
| L3Lint.Reporting | 95.6% | 86.8% |
| L3Lint.Hosting | 90.2% | 85.8% |
| l3lint (CLI) | 96.4% | 73.1% |
| l3lint-mcp | 86.3% | 80.0% |

### 2.2 Benchmarks (i7-6700K, 4 cores, Windows 10, .NET 10.0.11)

| Target | Spec | Measured | Verdict |
| --- | --- | --- | --- |
| US-09 | 10-line payload under 1 ms, warm | 0.387 ms mean | Met |
| NFR-002 latency | p95 under 50 ms for 100 lines, warm | 3.77 ms mean, 0.04 ms StdDev | Met |
| NFR-002 cold start | Under 300 ms with ReadyToRun | single-file win-x64 binary, median of 21: lint 280 ms, `version` 223 ms, `--help` 109 ms | Met with a small margin (ADR-0060) |
| US-08 | `lint_payload` under 200 ms for 100 lines | 4.36 ms engine work; end to end over MCP stdio checked by the timing test | Met |
| NFR-003 throughput | At least 2,000 payloads per second on 4 cores | 7,037 per second with 4 workers; 8,354 with 8 | Met |
| NFR-003 memory | Under 500 MB for 1 GB NDJSON | 158 MB peak working set for a 1,026.6 MB file of 921,666 payloads | Met (ADR-0059) |

Two targets were missed when first measured and are now met. Batch memory reached 2,463 MB for 256 MB of NDJSON, because the run kept every result as objects and the writers built whole documents in memory; ADR-0059 fixed it. Cold start was 327 ms for a lint; ADR-0060 brought it to 280 ms without dropping the pack validation that FR-019 requires.

Mutation testing (Test and delivery tab: at least 80% on Core and the rule packs, checked weekly; `.github/workflows/mutation.yml`, docs/dev/mutation-testing.md):

| Target | Score |
| --- | --- |
| L3Lint.Rules.General | 95.43% |
| L3Lint.Rules.Visa | 86.84% |
| L3Lint.Rules.Mastercard | 96.59% |
| L3Lint.Rules.Amex | 100% |
| L3Lint.Rules.Discover | 100% |
| L3Lint.Core | 85.16% |

The General pack first scored 51% (WP14 trial without the rule tests) and then 70%: the rule examples proved that rules fire, not what their findings say. Tests of the full finding of every GEN custom check and of GEN-DQ-007 over a run brought it to 95%. Core scored 79.53% on its first full run; tests that give every canonical field the wrong type and pin each schema keyword to its message brought it to 85%. Stryker's test runner for Microsoft.Testing.Platform is a preview and left 88 Core mutants untested; they are not in the score.

## 3. ADRs and deviations from the spec

### Deviations

| What | Spec | What l3lint does | Why |
| --- | --- | --- | --- |
| Spec location | SPEC_DIR is `./spec` | The seven spec files sit at the repository root and are treated as read-only (ADR-0030) | That is where the export was |
| Asterisked adapter fields | Field mapping table | Eight cells corrected against the gateways' current public references (Cybersource `invoiceDetails.taxable` and `shipFromPostalCode`; Authorize.net `currencyCode`, `discount.amount`, `shipFrom.zip`; Checkout.com `discount_amount`, `shipping_tax_amount`, `from_address_zip`) | The spec marks them "to verify"; docs/dev/verification-adapters.md has each source |
| NuGet ID of the hosting library | `L3Lint` | `L3Lint.Hosting` (ADR-0031) | NuGet IDs ignore case and the tool is `l3lint` |
| Extra CLI option | CLI options table | `--currency` on lint, fix and convert, and `inputs.defaultCurrency` (ADR-0056) | Braintree requests carry no currency, and a guess would break the arithmetic rules |
| Fix and convert output | Not fully specified | RFC 6902 patch per transaction; convert prints canonical JSON with the path map (ADR-0047) | Settled where the spec is silent |
| NDJSON size limit | 25 MB file cap | The cap applies per NDJSON record (ADR-0048) | A 1 GB NDJSON file is a stated use case |
| Verify snapshot tests | Verify named for snapshots | Checked-in expected files (ADR-0053) | Verify 33 fails the build without an owner-only sponsorship setting |
| Action location | Published from its own repository as `<owner>/l3lint-action@v1` | Lives in `action/` and is referenced as `brimatt16219/l3lint/action@v1.0.0` until the owner moves it (ADR-0061) | No repository can be created here; the Action tests need it to work now |
| JsonSchema.Net version | The package table names JsonSchema.Net without a version | Pinned to 8.0.5, the last MIT release (ADR-0045) | Later versions changed license terms |

### All ADRs

| ADR | Decision | Reason in one line |
| --- | --- | --- |
| 0001 | Rules come only from public sources | Hard constraint 3 |
| 0002 | No employer material | Hard constraint 4 (D2) |
| 0003 | The shipped tool is offline | Hard constraint 5 |
| 0004 | Card numbers are masked before anything reads them | Hard constraint 6 (D4) |
| 0005 | Advisory language only | D5; now enforced by `AdvisoryLanguageTests` |
| 0006 | Rules are versioned data plus small C# predicates | Packs never run code (NFR-006) |
| 0007 | One .NET 10 codebase | NFR-009 |
| 0008 | A canonical payload model sits in the middle | One rule set for every gateway |
| 0009 | Money is decimal with an explicit ISO 4217 exponent | Exact arithmetic |
| 0010 | Apache-2.0 for code and rule packs | One license for all |
| 0011 | The C# model mirrors the canonical JSON | The JSON field dictionary wins over the C# sketch |
| 0012 | Field selectors use JSON Pointer with one wildcard | Simple, standard paths |
| 0013 | What counts as a missing field | Null, empty, whitespace and unknown sentinels |
| 0014 | Arithmetic defaults and edge cases | 1 minor unit tolerance, away-from-zero rounding |
| 0015 | Visa detail indicator is checked for presence only | Who sets it is unconfirmed |
| 0016 | Single-processor rules use a profiles attribute | Clarification 6 |
| 0017 | Processor profiles are embedded YAML | Data, with a source per limit |
| 0018 | VISA-LT-001 runs only with a rate table | No hard-coded break-even |
| 0019 | Brand selection: auto and all | Unknown brands still lint |
| 0020 | Tier rollup | Likely tier per brand |
| 0021 | SARIF locations come from the token index | Real line and column |
| 0022 | Mechanical fixes only | Never invent business data |
| 0023 | Card-number detection follows the Architecture algorithm exactly | No guesswork |
| 0024 | Rule pack integrity: manifest and lock file | FR-019 |
| 0025 | Amex unit codes | 720 codes from a public table |
| 0026 | Asterisked adapter fields | Each checked against a public page |
| 0027 | Action input tool-source | Self-lint in CI before the tool is published |
| 0028 | Agent evaluation harness | Measures MCP use by an agent |
| 0029 | Initial versions and pack dates | 1.0.0 everywhere |
| 0030 | Spec export location | Root files are the spec set |
| 0031 | The Hosting project ships as the meta-package L3Lint.Hosting | NuGet case collision |
| 0032 | Eligibility gating and supersession | Rate rules only where eligible |
| 0033 | Engine diagnostics outside the rule catalog | `L3LINT-` IDs |
| 0034 | Batch findings | Anchored to a real transaction |
| 0035 | Where user-facing text lives | Resources and packs (NFR-011) |
| 0036 | Tolerant canonical mapping | Payload content never throws |
| 0037 | Input errors versus findings | Exit 2 versus findings |
| 0038 | Check kind parameters | Small additions to the eight kinds |
| 0039 | VISA-L3-010 reports once per transaction | The spec's worked example gets exactly one warning |
| 0040 | Rule examples come from compact definitions | Non-circular fixtures |
| 0041 | Lock file creation | `init` writes it |
| 0042 | Context overrides build an effective transaction | Flags beat payload beat config |
| 0043 | Canonical schema strictness | Typos caught |
| 0044 | Project and docs URLs | Working names in one place |
| 0045 | JsonSchema.Net pinned to 8.0.5 | License |
| 0046 | ModelContextProtocol SDK pinned to 1.x | NFR-009 |
| 0047 | Output shapes for fix and convert | Spec silent |
| 0048 | The file size limit applies per NDJSON record | Large batches |
| 0049 | CLI text output uses LF and ASCII tables | Byte-identical output on every OS |
| 0050 | SARIF output details beyond the mapping table | GitHub's subset and limits |
| 0051 | JUnit XML mapping, batch findings and input errors | Valid against the Jenkins XSD |
| 0052 | Markdown job summary layout, escaping and size limit | GitHub's 1 MB summary limit |
| 0053 | Snapshot tests use checked-in expected files, not Verify | Verify needs owner action |
| 0054 | Reports carry the metadata of the rules that fired | SARIF and Markdown need it |
| 0055 | The OASIS SARIF schema is validated with draft-07 rules | The vendored file is draft-07 |
| 0056 | Default currency for payloads that carry none | Braintree |
| 0057 | How the corpus counts recall and false positives | Clear gate arithmetic |
| 0058 | Timing tests run alone, in their own project | Contention made them meaningless |
| 0059 | Batch results are kept compressed and reports stream | NFR-003 memory |
| 0060 | Cold start keeps pack validation and loads packs in parallel | NFR-002 cold start without breaking FR-019 |
| 0061 | The Action lives in action/ until the owner moves it to l3lint-action | No repositories are created during the build |

### Found and fixed late

The WP14 security and fuzz tests found seven code gaps and two design gaps, all fixed (docs/dev/security-review.md): deep YAML crashed the process with a stack overflow (a denial of service for the Action), YAML past 63 levels and some malformed YAML ended as internal errors, large config integers and NUL characters in paths threw, invalid UTF-8 in a JSON string was an internal error instead of an input error, `DirectoryRulePackSource.Open` let a sibling folder with the same name prefix through, an oversized NDJSON record lost its line number, batch memory grew linearly, and one long NDJSON line was held whole. The same review found that `PointerComparer` was not a total order for mixed keys such as "2", "10" and "1a", and that a `!!str` tag was ignored; both are fixed with tests.

## 4. Unconfirmed facts and partial data

From docs/dev/verification.md and docs/dev/verification-adapters.md:

- Visa CEDP line and total rounding tolerance: no public Visa source. l3lint keeps the spec's 1 minor unit, configurable.
- Who sets the Visa line-item detail indicator: not stated by Visa. VISA-L3-010 stays a warning with "Confirm your processor sets it."
- Mastercard line description maximum: no public Mastercard page; processors use 26, 35 or 127. Only processor profiles set it.
- Amex requestor name maximum: no public length; GEN-FMT-006 uses the canonical default of 40.
- Discover enhanced-data program: no public announcement; DISC-ELIG-001 reports that no Discover Level 2 or 3 rate exists.
- Adapters: six mappings stay unconfirmed from public pages: Braintree `orderId` as the invoice number, Cybersource `productCode` versus `productSku`, Authorize.net `taxIncludedInTotal` (schema only) and the single-object form of `lineItem`, the base and maximum of Adyen's legacy `itemDetailLine{N}` numbering, and Stripe's top-level `shipping.address.country`, which stays unmapped. Each is marked in docs/articles/adapters.md.
- Processor profiles: Adyen's and most Cybersource processors' line-item maxima are not published, so those profiles leave them unset.
- Confirmed during the build: the Visa Level 2 sunset (rules end on 2026-04-17), the Mastercard unit list against J.P. Morgan, and the Large Ticket break-even from Visa's schedule.
- No shipped list is marked partial.

## 5. What the owner does next

1. **Name check and employer clearance.** Confirm that the name `l3lint`, the package IDs (`l3lint`, `L3Lint.*`), the repository `brimatt16219/l3lint` and the MCP name `io.github.brimatt16219/l3lint` are free, and get written clearance from Fiserv (D2) before anything is public. If a name changes, edit `L3LintProjectUrl` and `L3LintDocsUrl` in `Directory.Build.props` (ADR-0044), `src/L3Lint.Mcp/.mcp/server.json` and `action/README.md`.
2. **Repository creation and first commit.** Create the repository, then from the root: `git init`, `git add .`, review `git status` (spec files at the root are yours to keep or move; `artifacts/`, `TestResults/` and `docs/_site/` are ignored), commit, add the remote and push. Recheck the third-party action SHAs in `action/action.yml` and `.github/workflows/*.yml` against their release pages.
3. **Secrets and package signing.** Add `NUGET_API_KEY`; optionally `NUGET_SIGNING_CERT` (base64 PFX) and `NUGET_SIGNING_PASSWORD`. Create the `release` environment with required reviewers. Enable GitHub Pages from Actions for the docs workflow.
4. **Publishing to NuGet and the MCP Registry.** Set `action/VERSION` to the release version, push tag `v1.0.0`. `release.yml` gates on CI, packs, signs when configured, writes SBOMs, builds the six binaries, attests, pushes to NuGet.org, waits for `L3Lint.Mcp` to appear, publishes `server.json` with `mcp-publisher login github-oidc`, creates the GitHub release and moves the `v1` tag. Confirm the `io.github.brimatt16219` namespace works with GitHub OIDC first. Run the MCP Inspector against `l3lint-mcp` once, as the release gate asks.
5. **Splitting `action/` into its own repository and listing it on the Marketplace.** Move `action/` (action.yml, scripts, README, VERSION) to the spec's `l3lint-action` repository and `samples/action-example` to `l3lint-action-example`. Then change every `brimatt16219/l3lint/action@v...` reference to `<owner>/l3lint-action@v...`; ADR-0061 lists the files, including the `init` workflow template and its test. Tag `v1.0.0` and `v1` there, and publish the Marketplace listing.
6. **Enabling code scanning.** Turn on code scanning in the repository and in a public sample repository, run the Action there, and check that the SARIF upload shows alerts (the release gate "the Action runs green in a public sample repository").

## 6. Commands

```bash
# Build and test (from the repository root)
dotnet tool restore
dotnet restore L3Lint.slnx --locked-mode
dotnet build L3Lint.slnx -c Release
dotnet format L3Lint.slnx --verify-no-changes
dotnet test --solution L3Lint.slnx -c Release
dotnet run --project tests/L3Lint.Performance -c Release --no-build

# Generated files are current
dotnet run --project tools/L3Lint.PackManifest -c Release -- --check
dotnet run --project tools/L3Lint.FixtureGen -c Release -- corpus --check
dotnet run --project tools/L3Lint.DocsGen -c Release -- all --check

# Run the CLI and the MCP server from source
dotnet run --project src/L3Lint.Cli -- lint samples/action-example/fixtures/payments/INV-20418.json
dotnet run --project src/L3Lint.Mcp -- --http --port 7341

# Benchmarks and the memory probe
dotnet run -c Release --project tests/L3Lint.Benchmarks -- --job short --inProcess --filter "*"
dotnet run -c Release --project tests/L3Lint.Benchmarks -- memory 1024

# Mutation testing (long; run from the test project folder)
cd tests/L3Lint.Core.Tests && dotnet stryker --config-file ../../stryker-config.json --skip-version-check && cd ../..
cd tests/L3Lint.Rules.Tests && dotnet stryker --config-file ../../stryker-packs.json --project L3Lint.Rules.General.csproj --skip-version-check && cd ../..

# Docs site
dotnet tool run docfx docs/docfx.json --warningsAsErrors

# Pack and publish binaries into ./artifacts (nothing is pushed)
dotnet pack L3Lint.slnx -c Release -o artifacts/packages
dotnet publish src/L3Lint.Cli -c Release -r linux-x64 -o artifacts/binaries/linux-x64
```
