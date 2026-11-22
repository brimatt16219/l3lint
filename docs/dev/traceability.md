# Traceability

Each functional requirement (FR), non-functional requirement (NFR) and user story (US) from the spec, the code that implements it, and the tests that prove it. Paths are relative to the repository root; test names are classes (tests/&lt;project&gt;/&lt;class&gt;.cs). Rule-level traceability (78 rules, each with sources, a pass example and a fail example) is in the packs and is checked by `l3lint rules test` (tests/L3Lint.Rules.Tests/RuleExampleTests.cs) and the canonical corpus (tests/L3Lint.Corpus.Tests/CorpusTests.cs).

Projects named "Architecture", "Fuzz", "Security" and "Benchmarks" below come from work package WP14; see docs/dev/quality-wp14-report.md.

## Functional requirements

| ID | Requirement | Code | Tests |
| --- | --- | --- | --- |
| FR-001 | Canonical JSON from file, stdin, glob or NDJSON | src/L3Lint.Hosting/Runs/InputExpander.cs, LintRunner.cs, InputFile.cs, NdjsonLines.cs; src/L3Lint.Core/Input/RawDocument.cs | Cli.Tests LintCommandTests (stdin, NDJSON, folders and globs) |
| FR-002 | Schema validation as GEN-FMT findings with paths | src/L3Lint.Core/Schemas/SchemaValidator.cs, SchemaMessages.cs; schemas/canonical.schema.json; GEN-FMT-001 (Rules.General/Checks/SchemaCheck.cs) | Core.Tests SpecExampleSchemaTests; Rules.Tests OneValueOneFindingTests; RuleExampleTests (GEN-FMT-001) |
| FR-003 | Stripe, Adyen, Braintree adapters | src/L3Lint.Adapters/StripeAdapter.cs, AdyenAdapter.cs, BraintreeAdapter.cs, Json/* | Adapters.Tests StripeAdapterTests, AdyenAdapterTests, BraintreeAdapterTests, FixtureTests |
| FR-004 | Cybersource, Authorize.net, Checkout.com adapters | src/L3Lint.Adapters/CybersourceAdapter.cs, AuthorizeNetAdapter.cs, CheckoutComAdapter.cs | Adapters.Tests CybersourceAdapterTests, AuthorizeNetAdapterTests, CheckoutComAdapterTests, FixtureTests |
| FR-005 | Auto-detection with scores; tie exits 2 with candidates | src/L3Lint.Core/Adapters/AdapterRegistry.cs, FormatDetectionException.cs; src/L3Lint.Cli/CommandContext.cs | Adapters.Tests DetectionTests (70 of 70 fixtures, tie case); Mcp.Tests ToolTests |
| FR-006 | CSV with a YAML column map, grouped by invoice | src/L3Lint.Adapters/CsvAdapter.cs, Csv/*; schemas/csvmap.schema.json | Adapters.Tests CsvAdapterTests, FixtureTests (csv) |
| FR-007 | Findings at source paths through the path map | src/L3Lint.Core/Adapters/PathMap.cs, PathMapBuilder.cs; src/L3Lint.Core/Engine/FindingFactory.cs; src/L3Lint.Reporting/Mapping/MappingJson.cs | Adapters.Tests PathMapTests, FixtureTests; Cli.Tests FixConvertInitTests (convert) |
| FR-008 | Major or minor units; exponent from ISO 4217 | src/L3Lint.Core/Model/Iso4217.cs, Currency.cs, Money.cs; src/L3Lint.Adapters/Json/SourceConverters.cs | Core.Tests MoneyAndCurrencyTests; Adapters.Tests UnitConversionTests |
| FR-009 | Context: brand, product, MCC, country, processor, as-of | src/L3Lint.Core/LintOptions.cs, ContextDefaults.cs, Engine/ContextOverrides.cs; src/L3Lint.Cli/ContextOptions.cs; src/L3Lint.Hosting/SettingsResolver.cs | Core.Tests ContextOverridesTests (CoreUnitTests.cs); Cli.Tests LintCommandTests, ConfigTests |
| FR-010 | `--as-of` selects rules in force | src/L3Lint.Core/Engine/RuleSelection.cs, Rules/RuleMetadata.cs (IsEffective) | Cli.Tests LintCommandTests (As_of_before_a_rule_takes_effect_skips_it); Corpus seeded tax-ratio fixtures (Visa as of 2026-04-17) |
| FR-011 | Processor profiles with limits | src/L3Lint.Core/Profiles/*.yaml, ProcessorProfiles.cs; schemas/profile.schema.json | Corpus seeded too-many-lines fixtures; Mcp.Tests ToolTests (built-in profiles only) |
| FR-012 | Rule pack engine and rule metadata | src/L3Lint.Core/Packs/RulePackLoader.cs, Checks/*, Engine/LintEngine.cs; schemas/rulepack.schema.json | Core.Tests RulePackLoaderTests (every check kind, error paths); Rules.Tests RuleExampleTests |
| FR-013 | Visa CEDP and Mastercard Data Rate packs | src/L3Lint.Rules.Visa, src/L3Lint.Rules.Mastercard | Rules.Tests RuleExampleTests, SpecExampleBehaviorTests; Corpus.Tests |
| FR-014 | Amex and Discover packs | src/L3Lint.Rules.Amex, src/L3Lint.Rules.Discover | Rules.Tests RuleExampleTests, AmexAdviceTextTests; Corpus.Tests |
| FR-015 | Arithmetic with tolerance and rounding | src/L3Lint.Core/Engine/DerivedFactsBuilder.cs; Rules.General/Checks/LineTotalCheck.cs, TransactionTotalCheck.cs, TaxConsistencyCheck.cs | Rules.Tests SpecExampleBehaviorTests (worked arithmetic), OneValueOneFindingTests; property tests (Core.Tests/Properties) |
| FR-016 | Per-payload data quality heuristics | src/L3Lint.Core/Detectors/*; Rules.General/Checks/Placeholder*.cs, MerchantNameDescriptionCheck.cs; Rules.Visa/Checks/VisaDescriptionQualityCheck.cs | Rules.Tests RuleExampleTests (GEN-DQ-001 to 006, VISA-DQ); Corpus seeded placeholder fixtures |
| FR-017 | Batch heuristics | src/L3Lint.Core/Engine/BatchAggregator.cs; Rules.General/Checks/StaticBatchDataRule.cs | Rules.Tests LibraryApiTests (LintManyAsync batch result); Corpus GEN-DQ-007 fixture |
| FR-018 | Config overrides severity, disables, only | src/L3Lint.Hosting/L3LintConfiguration.cs, SettingsResolver.cs; src/L3Lint.Core/Engine/FindingFactory.cs | Cli.Tests ConfigTests, LintCommandTests (Only_disable_and_severity) |
| FR-019 | Pack schema and SHA-256 checksum; exit 3 | src/L3Lint.Core/Packs/RulePackLoader.cs, PackHashing.cs, PackManifest.cs; src/L3Lint.Hosting/LockFile.cs | Core.Tests RulePackLoaderTests; Cli.Tests ConfigTests (lock mismatch), RuleCommandTests (broken local pack exits 3); Cli.Tests MoreOptionTests (tampered local pack exits 3); Security.Tests PathTraversalTests |
| FR-020 | `rules test` runs examples | src/L3Lint.Core/RuleExampleTester.cs; src/L3Lint.Cli/Commands/RulesCommand.cs | Cli.Tests RuleCommandTests; Rules.Tests RuleExampleTests |
| FR-021 | Text output with color and NO_COLOR | src/L3Lint.Reporting/Text/TextReportWriter.cs; src/L3Lint.Cli/CommandContext.cs | Cli.Tests OutputTests (Color_only_on_a_terminal_without_no_color), LintCommandTests |
| FR-022 | JSON output to the results schema | src/L3Lint.Reporting/Json/JsonReportWriter.cs; schemas/results.schema.json | Cli.Tests OutputTests; Reporting.Tests ConformanceTests; Mcp.Tests ToolTests |
| FR-023 | SARIF 2.1.0 within GitHub's subset | src/L3Lint.Reporting/Sarif/SarifReportWriter.cs | Reporting.Tests SarifWriterTests, ConformanceTests (OASIS schema), DeterminismTests |
| FR-024 | JUnit XML and Markdown | src/L3Lint.Reporting/Junit/JunitReportWriter.cs, Markdown/MarkdownReportWriter.cs | Reporting.Tests JunitWriterTests (XSD), MarkdownWriterTests, SnapshotTests |
| FR-025 | Exit codes 0 to 3 | src/L3Lint.Cli/ExitCodes.cs, CommandContext.cs; src/L3Lint.Hosting/Runs/LintRunner.cs | Cli.Tests LintCommandTests, ConfigTests, OutputTests (exit 3 on unwritable output) |
| FR-026 | `explain` | src/L3Lint.Cli/Commands/ExplainCommand.cs; src/L3Lint.Reporting/Rules/RuleMarkdown.cs, RuleJson.cs | Cli.Tests RuleCommandTests |
| FR-027 | `fix`: RFC 6902 patch or patched copy, mechanical only | src/L3Lint.Core/Engine/PatchTranslator.cs, Checks/MechanicalFixes.cs, Fixes/JsonPatch.cs; src/L3Lint.Hosting/Fixes/FixPlanner.cs; src/L3Lint.Cli/Commands/FixCommand.cs | Cli.Tests FixConvertInitTests; Core.Tests JsonPatchTests; Mcp.Tests ToolTests (suggest_fixes) |
| FR-028 | Cost estimates from a user rate table | src/L3Lint.Core/Estimates/*; schemas/rates.schema.json | Cli.Tests OutputTests (Cost_estimates_need_a_rate_table); Rules.Tests RuleExampleTests (VISA-LT-001) |
| FR-029 | MCP server, six tools, stdio and Streamable HTTP | src/L3Lint.Mcp/* | Mcp.Tests ToolTests and ResourceAndPromptTests (HTTP), TransportTests (every tool over stdio; HTTP safeguards) |
| FR-030 | GitHub Action | action/action.yml, action/scripts/run.sh | Cli.Tests ActionScriptTests (green, red, path escapes, injection) |
| FR-031 | .NET library with ILinter and DI | src/L3Lint.Core/ILinter.cs, LinterBuilder.cs; src/L3Lint.Hosting/L3LintServiceCollectionExtensions.cs | Rules.Tests LibraryApiTests |
| FR-032 | `init` | src/L3Lint.Cli/Commands/InitCommand.cs, Templates/* | Cli.Tests FixConvertInitTests (Init_*) |
| FR-033 | Mask card numbers; no values in logs without --log-values | src/L3Lint.Core/Pci/*; src/L3Lint.Core/Input/RawDocument.cs (masked text); src/L3Lint.Adapters/CsvAdapter.cs | Core.Tests CardNumberTests; Cli.Tests LintCommandTests (Card_numbers_are_masked_in_every_output); Corpus.Tests (No_output_format_ever_contains_a_test_card_number); Security.Tests CardNumberOutputTests, LogPrivacyTests |
| FR-034 | Size, line and depth limits | src/L3Lint.Core/Input/InputLimits.cs, RawDocument.cs; src/L3Lint.Core/Engine/LintEngine.cs | Core.Tests RawDocumentTests (CoreUnitTests.cs); Rules.Tests LibraryApiTests (Too_many_line_items); Mcp.Tests ToolTests (2 MB); Security.Tests JsonHardeningTests, CliLimitTests (per-record NDJSON limit with its line) |
| FR-035 | No network from Core or CLI; MCP HTTP binds localhost | src/L3Lint.Mcp/McpApp.cs (127.0.0.1), LocalRequestGuard.cs | Mcp.Tests TransportTests; Architecture tests (no network types) |
| FR-036 | Reference page for every rule | tools/L3Lint.DocsGen; src/L3Lint.Reporting/Rules/RuleMarkdown.cs | `DocsGen all --check` in ci.yml (docs job) |

## Non-functional requirements

| ID | Requirement | Code | Tests |
| --- | --- | --- | --- |
| NFR-001 | Byte-identical JSON and SARIF; fixed sort | src/L3Lint.Core/Engine/FindingOrder.cs, Fields/PointerComparer.cs (total order); src/L3Lint.Hosting/Runs/LintRunner.cs (ordered collector), TransactionResultStore.cs; src/L3Lint.Core/LinterBuilder.cs (parallel pack loads used in order, ADR-0060) | Corpus.Tests (1 vs 8 threads over 600 payloads; ResultStoreTests: every writer byte-identical from compressed results); Reporting.Tests DeterminismTests; Core.Tests PointerComparerProperties; Cli.Tests OutputTests |
| NFR-002 | Latency; cold start under 300 ms | engine as a whole; SchemaValidator quick pass; parallel pack loading and deferred console logger (ADR-0060) | tests/L3Lint.Performance LatencyTests (ADR-0058); benchmarks and cold-start timings (docs/dev/benchmarks.md); Cli.Tests DeferredLoggerFactoryTests |
| NFR-003 | Throughput and memory for large NDJSON | src/L3Lint.Hosting/Runs/LintRunner.cs, NdjsonLines.cs (streaming, bounded channel, per-record limit), TransactionResultStore.cs; streaming JSON and JUnit writers, ReportIndex rows (ADR-0059) | benchmarks (10,000 records) and the memory probe (1 GB file, 158 MB peak); Corpus.Tests ResultStoreTests; Cli.Tests NdjsonLinesTests |
| NFR-004 | Platforms and packaging | src/L3Lint.Cli/L3Lint.Cli.csproj (single-file, ReadyToRun) | ci.yml matrix (three operating systems); packaging dry run (artifacts/) |
| NFR-005 | No telemetry; logs without values | src/L3Lint.Cli/Commands/LintCommand.cs (--log-values); logger messages carry paths and IDs | Security.Tests LogPrivacyTests (process console captured), NoNetworkTests; Architecture.Tests NoTelemetryTests, NoNetworkTests |
| NFR-006 | Hard limits; streaming; no dynamic code from packs | src/L3Lint.Core/Yaml/YamlJson.cs (tags refused, alias budget, depth limit); src/L3Lint.Core/Input/RawDocument.cs (size, depth, UTF-8); RulePackLoader (declarative checks only); DirectoryRulePackSource (listed files only) | Core.Tests YamlJsonTests, RawDocumentTests; Security.Tests YamlHardeningTests, JsonHardeningTests, PathTraversalTests; Fuzz.Tests (json, config, csv, rulepack) |
| NFR-007 | Card numbers masked before rules, logs and outputs | src/L3Lint.Core/Engine/LintEngine.cs (scrub before map) | as FR-033 |
| NFR-008 | Signed packages, SBOM, attestations | .github/workflows/release.yml | not testable locally; SBOM generated in the packaging dry run |
| NFR-009 | .NET 10; MCP SDK 1.x | global.json; Directory.Packages.props (ADR-0046) | build |
| NFR-010 | Color never the only signal | src/L3Lint.Reporting/Text/TextReportWriter.cs (severity words) | Cli.Tests OutputTests, LintCommandTests |
| NFR-011 | Strings in resource files | Resources/Strings.resx in every project (src/Directory.Build.targets) | build (strongly typed resources); Architecture tests |
| NFR-012 | JSON logs to stderr; stdout only results | src/L3Lint.Cli/CommandContext.cs (LogToStandardErrorThreshold) | Cli.Tests LintCommandTests (Logs_go_to_standard_error_only) |
| NFR-013 | Core independent; 90% coverage | project references; ci.yml coverage job | Architecture.Tests DependencyDirectionTests, PublicApiFilesTests, ShippedSourceTests; docs/dev/coverage.md |
| NFR-014 | Everything documented, generated where possible | tools/L3Lint.DocsGen (rules, CLI, schemas) | `DocsGen all --check` fails on a missing description |

## User stories

| ID | Story | Proven by |
| --- | --- | --- |
| US-01 | Lint one canonical payload from the terminal | Cli.Tests LintCommandTests (spec example, exit codes) |
| US-02 | Lint a gateway format | Adapters.Tests FixtureTests, DetectionTests; Cli.Tests LintCommandTests (Braintree with --currency) |
| US-03 | Context-aware eligibility | Cli.Tests LintCommandTests (Card_product_flag_overrides_the_payload_and_says_so: one informational finding) |
| US-04 | Batch lint, --max-findings | Cli.Tests LintCommandTests (folders, globs, NDJSON, Max_findings_caps_output) |
| US-05 | Explain a rule | Cli.Tests RuleCommandTests |
| US-06 | Safe fixes | Cli.Tests FixConvertInitTests; Mcp.Tests ToolTests (suggest_fixes never touches the amount) |
| US-07 | CI gate | Cli.Tests ActionScriptTests; Reporting.Tests (SARIF, Markdown) |
| US-08 | Agent validation within 200 ms | Mcp.Tests ToolTests (errors as tool results); tests/L3Lint.Performance LatencyTests; tools/L3Lint.AgentEval |
| US-09 | Runtime check in a .NET service, under 1 ms | Rules.Tests LibraryApiTests (DI, thread safety); tests/L3Lint.Performance LatencyTests |
| US-10 | Cost estimate | Cli.Tests OutputTests (Cost_estimates_need_a_rate_table_and_are_labeled); Rules.Tests RuleExampleTests (VISA-LT-001) |
| US-11 | Rule updates: versioned packs validated on load; `rules list` shows version, dates, source count | Core.Tests RulePackLoaderTests (schema and checksum); Cli.Tests RuleCommandTests (rules list), ConfigTests (lock file) |
| US-12 | Card number safety: GEN-PCI-001, masked everywhere, `--strict-pci` stops | Core.Tests CardNumberTests; Corpus.Tests PCI set; Cli.Tests LintCommandTests (Strict_pci_stops_with_exit_one), FixConvertInitTests (fix copy and convert masked) |
| US-13 | Lint as of a date: Visa Level 2 rules apply as of 2026-03-01 | Rules.Tests RuleExampleTests (VISA-L2-001 to 003 examples as of 2026-03-01); Corpus seeded tax-ratio fixtures (Visa as of 2026-04-17); Cli.Tests LintCommandTests (As_of_before_a_rule_takes_effect_skips_it) |
| US-14 | ERP CSV with a column map, rows grouped by invoice | Cli.Tests LintCommandTests (Csv_with_a_column_map_groups_rows_into_transactions); Adapters.Tests CsvAdapterTests, FixtureTests (csv fixtures with .map.yaml) |
