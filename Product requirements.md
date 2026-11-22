# 1. Product requirements

## Problem and opportunity

Merchants overpay on commercial cards because enhanced data fails quietly, and since October 2025 Visa validates the content of that data instead of only checking that fields are present. A pre-submission linter turns a feedback loop measured in weeks into one measured in seconds.

**Five forces behind the problem**

1. **Failures are invisible at checkout.** A transaction with bad enhanced data still approves; the cost shows up later as card-not-present or standard commercial categories on the statement ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/)).
2. **Rules are fragmented.** Each brand, processor and gateway has its own required fields, lengths and code lists. Unit of measure alone is 3 characters for Mastercard and 12 for Visa at several processors, and 2 characters for American Express Direct ([Cybersource](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html)).
3. **CEDP moved the bar from presence to quality.** Generic descriptions, zeros and filler text are disqualified, line-item math must reconcile, and tax-exempt sales need an explicit flag ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program), [Versapay](https://www.versapay.com/cedp)). Visa runs machine-learning checks after each settlement batch ([Curbstone](https://curbstone.com/new-data-requirements-cedp/)).
4. **Old automation now backfires.** Many integrations auto-filled line items to qualify. Under CEDP that filler is exactly what gets flagged ([Payroc](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program)).
5. **Feedback is slow.** Verified status takes either 500 successful CEDP transactions or 20 consecutive days of them ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/)), and lagged interchange credits post roughly 10 to 15 days after settlement ([Merchant Cost Consulting](https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/)).

**The opportunity.** The gap between Visa's Product 3 rate and its card-not-present rate is 0.95 points on corporate and purchasing cards; on one $50,000 invoice that is about $450 after the CEDP fee ([weAudit](https://www.weaudit.com/level-3-credit-card-processing/)). A web search in October 2026, covering GitHub projects and gateway documentation, found validation only inside individual gateways and billing platforms, such as Zuora's CEDP formulas for its Checkout.com connector ([Zuora](https://docs.zuora.com/en-us/zuora-payments/manage-payment-gateway-integrations-and-payment-methods/set-up-payment-gateway-integrations/checkout.com-payment-gateway/level-2-and-level-3-data-processing-on-checkout.com)). No standalone, brand-neutral, open-source linter turned up.

**Why now.** Visa resumed merchant verification on February 24, 2026 and added a process to exclude merchants with consistently poor data ([TabaPay](https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update)). Merchants that cannot prove data quality now risk losing access to the program, not just a lower rate.

## Users and jobs to be done

Five personas share one engine; each reaches it through a different surface.

| Persona | Context | Job to be done | Surface | Success looks like |
| --- | --- | --- | --- | --- |
| Integration developer | Builds payment requests from invoices at an ISV, merchant or gateway | Know before release that every payload shape meets brand requirements | CLI locally, GitHub Action on pull requests, library at runtime | Zero l3lint errors in CI; no downgrade tickets after launch |
| Payments solutions engineer | Reviews client payloads and answers "why did this downgrade?" | Diagnose a payload in minutes and cite the requirement | CLI `lint` and `explain`, MCP inside an AI chat | A written answer with field paths and sources, in one sitting |
| Finance or AR analyst | Exports invoices from an ERP as CSV | Find which invoices will downgrade and why, without writing code | CLI with a CSV mapping file | A sortable report of invoices with problems and fixes |
| AI coding agent | Writes or edits payment integration code | Validate the payloads it generates and correct them itself | MCP server tools | Seeded defects fixed within two tool calls |
| Rule maintainer | Open-source contributor tracking brand changes | Update rules when brands publish changes, with evidence | `l3lint rules` commands, rule pack test suite | A rule update with sources that passes the suite and ships within 30 days |

**Jobs the product deliberately leaves to people.** Choosing commodity codes, writing real item descriptions and deciding tax treatment need business knowledge. l3lint flags the problem and explains the requirement, but it never invents these values.

## Goals, non-goals and success metrics

The product succeeds when it catches documented problems reliably, explains them clearly and stays current, without ever touching card data.

**Goals**

- **G1. Coverage.** Detect every documented, statically checkable enhanced-data requirement for Visa and Mastercard, plus the format requirements for American Express and Discover.
- **G2. Explanation.** Every finding names the field path, the requirement, the source and a concrete fix.
- **G3. Reach.** Terminal, CI, AI agent and runtime library give identical results for identical input.
- **G4. Freshness.** Rule packs update within 30 days of a public brand change.
- **G5. Safety.** No card data, no network calls, no telemetry.

**Non-goals for v1**

- **NG1.** Calculating tax. l3lint checks tax data for presence, range and consistency only.
- **NG2.** Submitting transactions or calling any gateway.
- **NG3.** Predicting issuer or network behavior beyond documented rules.
- **NG4.** A hosted web service. v1 is local tooling; a hosted version is a later decision.
- **NG5.** Programs outside the US. EU, UK and Canada enhanced data come later.
- **NG6.** Industry addenda such as airline, lodging, car rental and fleet fuel data.

**Success metrics**

| Metric | Target | How it is measured |
| --- | --- | --- |
| Recall on seeded defects | At least 95% of seeded defects detected | Known-bad fixture corpus in CI |
| False positives | At most 2% of findings on known-good payloads | Known-good fixture corpus in CI |
| Single-payload latency | p95 under 50 ms for a payload with up to 100 line items | Benchmark suite (BenchmarkDotNet) |
| Batch throughput | At least 2,000 payloads per second on 4 cores | Benchmark suite |
| Rule freshness | Rule pack release within 30 days of a public brand change | Changelog dates against source dates |
| Agent repair rate | At least 90% of seeded defects fixed by an agent within two MCP tool calls | Agent evaluation harness |
| Adoption, 6 months after v1.0 | 500 tool installs and 25 public repositories using the Action | NuGet stats, GitHub dependency graph |

## User stories with acceptance criteria

Fourteen stories define done for v1.0. Each acceptance criterion becomes at least one automated test in the Test and delivery tab.

1. **US-01 Lint one payload.** As an integration developer, I want to lint a canonical JSON payload from the terminal so that I see problems before deploying.
   - `l3lint lint payload.json` prints findings grouped by severity, then a one-line summary.
   - Exit code is 0 with no errors, 1 with at least one error, 2 on usage or input errors.
   - Each finding shows rule ID, JSON path, message, fix and source link.
2. **US-02 Lint a gateway format.** As a developer, I want to lint a Stripe, Adyen, Braintree, Cybersource, Authorize.net or Checkout.com request body directly.
   - `--format <name>` selects an adapter; without it, auto-detection picks one.
   - Auto-detection is correct on at least 99% of the fixture corpus; when two adapters tie, the CLI exits 2 and lists the candidates.
   - Findings point at paths in the original format, not only the canonical model.
3. **US-03 Context-aware eligibility.** As an analyst, I want to state brand, card product and MCC so that eligibility rules apply.
   - `--brand visa --card-product purchasing --mcc 5045` runs eligibility rules.
   - A consumer card or an excluded MCC yields one informational finding ("no enhanced-data rate available") and suppresses rate-related errors.
4. **US-04 Batch lint.** As an analyst, I want to lint a folder, a glob or an NDJSON file.
   - 10,000 payloads finish with a summary table of counts per rule.
   - `--max-findings` caps output; the summary still counts everything.
5. **US-05 Explain a rule.** As anyone, I want `l3lint explain VISA-L3-004` to tell me what a rule requires and why.
   - Output shows requirement, rationale, source URL, confidence label, effective dates, one passing and one failing example.
6. **US-06 Safe fixes.** As a developer, I want deterministic fixes applied for me.
   - `l3lint fix --dry-run` prints an RFC 6902 JSON Patch; `--write` applies it to a copy.
   - Only mechanical fixes run (recompute line totals, normalize unit codes, trim to length, set explicit indicators). Descriptions, commodity codes and tax amounts are never invented.
7. **US-07 CI gate.** As a team lead, I want pull requests to fail when fixture payloads regress.
   - The Action uploads SARIF to code scanning and annotates changed files.
   - `fail-on: error | warning | none` controls the job result.
8. **US-08 Agent validation.** As an AI coding agent, I want a tool that validates a payload and returns structured findings.
   - `lint_payload` returns within 200 ms for 100 line items.
   - Invalid input comes back as a tool execution error the model can read and correct, not a protocol error.
9. **US-09 Runtime check.** As a backend developer, I want to call the linter in my .NET service before submitting.
   - `ILinter.Lint(payload, options)` is thread-safe and needs no I/O after startup.
   - A 10-line payload lints in under 1 ms on a warm process.
10. **US-10 Cost estimate.** As an analyst, I want an estimated cost of each downgrade.
    - Estimates require a user-supplied rate table (`--rates rates.yaml`); none ship with the tool.
    - Output is labeled "estimate" and states the rate table's name and date.
11. **US-11 Rule updates.** As a maintainer, I want to ship rule changes without a code release.
    - Rule packs are separate versioned packages, validated by schema and checksum on load.
    - `l3lint rules list` shows each pack's version, effective dates and source count.
12. **US-12 Card number safety.** As a security reviewer, I want card numbers blocked.
    - Any value that passes a Luhn check and matches a card-number pattern raises `GEN-PCI-001`.
    - The value is masked to the last four digits in every output; `--strict-pci` stops processing instead.
13. **US-13 Lint as of a date.** As an analyst, I want to check a payload against the rules in force on a given date.
    - `--as-of 2026-03-01` applies rules effective on that date, so Visa Level 2 rules still apply.
14. **US-14 ERP CSV.** As an AR analyst, I want to lint invoice exports.
    - `l3lint lint invoices.csv --format csv --map erp-map.yaml` maps columns to canonical fields and groups rows into transactions by invoice ID.

## Functional requirements

Thirty-six requirements; 21 are MVP and 15 land in v1.0. The Release column drives the backlog order in the Test and delivery tab.

| ID | Area | Requirement | Priority | Release | Stories |
| --- | --- | --- | --- | --- | --- |
| FR-001 | Input | Accept canonical JSON (schema v1) from a file, stdin, a glob or NDJSON. | MUST | MVP | US-01, US-04 |
| FR-002 | Input | Validate input against the canonical JSON Schema; report schema errors as `GEN-FMT` findings with JSON paths. | MUST | MVP | US-01 |
| FR-003 | Input | Adapters for Stripe PaymentIntent, Adyen payments and Braintree transaction sale requests. | MUST | MVP | US-02 |
| FR-004 | Input | Adapters for Cybersource REST payments, Authorize.net createTransaction and Checkout.com payments. | MUST | v1.0 | US-02 |
| FR-005 | Input | Auto-detect the input format with a confidence score; a tie exits with code 2 and lists candidates. | SHOULD | v1.0 | US-02 |
| FR-006 | Input | CSV input driven by a YAML column map; rows grouped into transactions by invoice ID. | MUST | v1.0 | US-14 |
| FR-007 | Input | Report every finding at the path in the original format, using the adapter's path map. | MUST | MVP | US-02 |
| FR-008 | Input | Accept major or minor currency units per adapter; take the exponent from an embedded ISO 4217 table. | MUST | MVP | US-01 |
| FR-009 | Context | Accept brand, card product (consumer, business, corporate, purchasing, fleet, unknown), MCC, merchant country, processor profile and transaction date. | MUST | MVP | US-03 |
| FR-010 | Context | `--as-of <date>` selects the rules in force on that date. | MUST | MVP | US-13 |
| FR-011 | Context | Processor profiles carry limits such as line-item caps and field lengths. | SHOULD | v1.0 | US-02 |
| FR-012 | Rules | Rule engine loads rule packs; each rule has ID, title, brands, level, default severity, applicability, check, message, fix, sources, confidence and effective dates. | MUST | MVP | All |
| FR-013 | Rules | Visa CEDP pack and Mastercard Data Rate II and III pack. | MUST | MVP | US-01 |
| FR-014 | Rules | American Express and Discover packs (format, lengths, limits, no-benefit notices). | MUST | v1.0 | US-03 |
| FR-015 | Rules | Arithmetic checks with configurable tolerance and rounding mode. | MUST | MVP | US-01 |
| FR-016 | Rules | Per-payload data quality heuristics: placeholder text, zero-filled values, merchant name used as description. | MUST | MVP | US-01 |
| FR-017 | Rules | Batch heuristics: identical description or product code across a configurable share of transactions. | SHOULD | v1.0 | US-04 |
| FR-018 | Rules | Config can override severity, disable rules, or run only selected rules. | MUST | MVP | US-07 |
| FR-019 | Rules | Validate rule pack schema and SHA-256 checksum on load; refuse invalid packs with exit code 3. | MUST | MVP | US-11 |
| FR-020 | Rules | `l3lint rules test` runs each rule's embedded pass and fail examples. | SHOULD | v1.0 | US-11 |
| FR-021 | Output | Human-readable text with color that honors `NO_COLOR`, grouped by transaction and severity. | MUST | MVP | US-01 |
| FR-022 | Output | JSON output that conforms to the published results schema, including engine and pack versions. | MUST | MVP | US-08 |
| FR-023 | Output | SARIF 2.1.0 output within GitHub's code scanning subset. | MUST | v1.0 | US-07 |
| FR-024 | Output | JUnit XML and Markdown outputs. | SHOULD | v1.0 | US-07 |
| FR-025 | Output | Exit codes: 0 clean, 1 errors found, 2 usage or input error, 3 internal or rule pack error. | MUST | MVP | US-01 |
| FR-026 | Output | `explain <ruleId>` prints requirement, rationale, sources, confidence, dates and examples. | MUST | MVP | US-05 |
| FR-027 | Output | `fix` produces an RFC 6902 JSON Patch (dry run) or a patched copy, mechanical fixes only. | SHOULD | v1.0 | US-06 |
| FR-028 | Output | Cost estimates from a user-supplied rate table, labeled as estimates. | SHOULD | v1.0 | US-10 |
| FR-029 | Interfaces | MCP server with lint, explain, list, map, fix and schema tools over stdio and Streamable HTTP. | MUST | v1.0 | US-08 |
| FR-030 | Interfaces | GitHub Action that runs the CLI, uploads SARIF and writes a job summary. | MUST | v1.0 | US-07 |
| FR-031 | Interfaces | .NET library with `ILinter` and dependency injection registration. | MUST | MVP | US-09 |
| FR-032 | Interfaces | `l3lint init` writes `.l3lint.yaml` and sample payloads. | SHOULD | v1.0 | US-07 |
| FR-033 | Safety | Detect and mask card numbers everywhere; never write payload values to logs unless `--log-values` is set. | MUST | MVP | US-12 |
| FR-034 | Safety | Input limits: 25 MB per file, 1,000 line items per transaction, JSON depth 64; all configurable. | MUST | MVP | US-04 |
| FR-035 | Safety | No network calls from Core or CLI; the MCP HTTP transport binds to localhost unless told otherwise. | MUST | MVP | US-08 |
| FR-036 | Docs | Generate a reference page for every rule from its rule pack entry. | MUST | v1.0 | US-05 |

## Non-functional requirements

Determinism and privacy matter as much as speed: the same input must give the same findings everywhere, and no payload data may leave the process.

| ID | Quality | Requirement | Verified by |
| --- | --- | --- | --- |
| NFR-001 | Determinism | Identical input, options and rule pack versions produce byte-identical JSON and SARIF output. Findings sort by transaction index, then JSON path, then rule ID. | Golden-file tests run twice per CI job |
| NFR-002 | Performance | p95 under 50 ms to lint one payload with up to 100 line items in a warm process; cold CLI start under 300 ms with ReadyToRun. | BenchmarkDotNet, `hyperfine` in CI |
| NFR-003 | Throughput | At least 2,000 payloads per second on 4 cores in batch mode, with bounded memory (under 500 MB for a 1 GB NDJSON file). | Benchmark suite |
| NFR-004 | Portability | Runs on Windows, macOS and Linux, x64 and arm64. Ships as a .NET tool and as self-contained single-file binaries. | CI matrix on all six targets |
| NFR-005 | Privacy | No telemetry. No network I/O in Core, CLI or Action. Logs carry rule IDs and paths, never field values, unless `--log-values` is set. | Network-deny test harness, log scanning test |
| NFR-006 | Security | Hard limits on file size, line items and JSON depth; streaming parsers for NDJSON; no dynamic code execution from rule packs. | Fuzz tests (SharpFuzz), limit tests |
| NFR-007 | PCI | Card-number values are masked before any rule, log or output sees them. | PAN fixture tests across every output format |
| NFR-008 | Supply chain | Packages are signed; releases include an SBOM (CycloneDX) and build provenance attestation; builds are reproducible. | Release workflow checks |
| NFR-009 | Compatibility | Targets .NET 10 LTS. The MCP server follows the protocol revision implemented by the ModelContextProtocol C# SDK 1.x. | CI against the MCP Inspector |
| NFR-010 | Accessibility | Color is never the only signal; severity words always print. `NO_COLOR` and `--no-color` disable color. | Snapshot tests |
| NFR-011 | Localization | All user-facing strings live in resource files; v1 ships English. | Resource completeness test |
| NFR-012 | Observability | Structured logs go to stderr as JSON lines with `--log-format json`; stdout carries only results. | Output stream tests |
| NFR-013 | Maintainability | Core has no dependency on CLI, MCP or Action code; line coverage at least 90% in Core and rule packs. | Architecture tests (NetArchTest), coverage gate |
| NFR-014 | Documentation | Every command, option, rule and output field is documented and generated from source where possible. | Docs build fails on missing entries |

## Scope by release

The MVP proves the engine on Visa and Mastercard through the CLI and library; v1.0 adds every surface and all four brands; later releases widen geography and industries.

| Area | MVP | v1.0 | Later |
| --- | --- | --- | --- |
| Brands | Visa CEDP, Mastercard Data Rate II and III | Plus American Express and Discover | Regional programs (EU, UK, Canada) |
| Inputs | Canonical JSON, Stripe, Adyen, Braintree | Plus Cybersource REST, Authorize.net, Checkout.com, CSV with mapping | PayPal, J.P. Morgan, Worldpay; ERP-specific mapping templates |
| Checks | Fields, formats, arithmetic, per-payload data quality, eligibility | Batch data quality, processor profiles | Industry addenda (fleet, lodging, airline, car rental) |
| Outputs | Text, JSON, exit codes, `explain` | SARIF, JUnit, Markdown, `fix`, cost estimates | HTML report |
| Surfaces | CLI, .NET library | MCP server, GitHub Action, `init`, rule reference site | VS Code extension, hosted API |
| Rule operations | Packs embedded in the tool | Packs as separate NuGet packages, `rules test` | Signed community packs, automated change watch on public sources |

**Release gates.** The MVP ships when US-01, US-03, US-05, US-09, US-12 and US-13 pass and the fixture corpus meets the recall and precision targets. v1.0 ships when all 14 stories pass, the Action runs green in a public sample repository, and the MCP server passes the MCP Inspector checks.

## Risks and open questions

The largest risk is that Visa's exact CEDP validation logic is not public, so some checks will always be inferred; the confidence labels exist to say so honestly.

| ID | Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| R1 | Visa's machine-learning validation catches problems no static rule can see | High | Medium | Label inferred rules; position l3lint as reducing risk, not guaranteeing results; let users feed back real downgrade outcomes as new fixtures |
| R2 | Public sources disagree (for example April 17 versus April 18, 2026 for the Level 2 sunset) | High | Low | Record every source on the rule; pick the most conservative reading; flag disagreements in `explain` |
| R3 | Brands change rules every April and October | Certain | Medium | Rule packs ship separately from code; effective dates on every rule; a calendar reminder to review sources twice a year |
| R4 | Employer conflict of interest or IP claim | Medium | High | Decision D2: personal time and equipment, written clearance, public sources only, no employer gateway adapters |
| R5 | Users paste real card numbers | Medium | High | Luhn-based detection and masking before processing (US-12); documentation and CLI warnings |
| R6 | False positives erode trust | Medium | High | Precision target of at most 2% false positives; every heuristic is a warning by default, never an error |
| R7 | Gateway request formats change | Medium | Medium | Adapter contract tests against published examples; adapters versioned independently |
| R8 | The MCP protocol evolves quickly | Medium | Low | Depend on the official C# SDK; keep tools thin over the shared engine |

**Open questions**

- [ ] Does the owner's employer clear publishing this project? Who signs off, and is any adapter off limits?
- [ ] Which public source best documents Visa's line-item tolerance for rounding? Until found, default to $0.01 per line and per total.
- [ ] Should batch heuristics (FR-017) flag at 80% or 95% identical descriptions? Decide from fixture experiments.
- [ ] Is Mastercard's post-October 2025 unit-of-measure list the same as the code list J.P. Morgan publishes? Confirm against a second processor.
- [ ] Should rule packs carry a license separate from the code (for example CC BY 4.0)?
- [ ] Final product name, after the collision check.
