# WP14 report: quality harnesses

Date: 2026-10-30. Covers T-43 (fuzzing), T-44 (benchmarks), T-45 (security review), the NFR-013 architecture tests, the property tests and the Stryker configuration. Paths are relative to the repository root.

Status on 2026-11-05: this report describes the state when WP14 was handed over. Since then the gaps it found (G1 to G7, D1, D2, the `PointerComparer` order and the `!!str` tag) are fixed in `src`, every test it added passes, and NFR-003 memory is met (ADR-0059). docs/dev/security-review.md and docs/dev/benchmarks.md carry the current state.

## What was built

| Area | Where | What it checks |
| --- | --- | --- |
| Architecture tests | `tests/L3Lint.Architecture.Tests` | Dependency direction per assembly (references and NetArchTest type dependencies, offending types listed); no network API or assembly in Core, packs, Adapters, Reporting, Hosting or Cli; only the MCP server uses ASP.NET Core and it makes no outbound calls; MCP tool and content code has no file system API; no telemetry or update-check assembly anywhere in the load closure of any shipped assembly; every library has both PublicAPI files; no open-work markers, unimplemented members, empty catch blocks or em and en dashes in `src`, `docs` and `schemas`; every third-party Action and workflow step pinned by commit SHA. Positive controls show the checks find dependencies that do exist |
| Property tests | `tests/L3Lint.Core.Tests/Properties`, `tests/L3Lint.Rules.Tests/Properties` | FsCheck, 1,000 cases each: money rounding for every ISO 4217 currency (exponents 0, 2, 3 and 4); canonical JSON round trip (equal transaction and byte-identical rewrite); `PointerComparer` is a total order with numeric index order; JSON Patch add-then-remove, add-at-pointer and replace-back; payloads built from formula 1 (with formulas 2 and 3 for the header) never get GEN-ARITH-001, -002 or -003, a line total moved beyond the tolerance fails GEN-ARITH-001 on that line only, and a move of one minor unit still passes |
| Fuzz harness | `tests/L3Lint.Fuzz` | SharpFuzz targets `json` (parse, map, lint, the stdin batch path and all five writers), `config`, `csv` (fixed map) and `rulepack` (loader with a matching manifest, then a lint). libFuzzer, AFL and repro modes; README for Linux |
| Fuzz smoke | `tests/L3Lint.Fuzz.Tests` | 5,000 seeded mutations for json, config and csv and 2,000 for rulepack, from the repository's fixtures, spec examples and packs; only a result or a documented exception may come out. Failures are grouped by exception and first l3lint frame, with the smallest input saved for `repro` |
| Security tests | `tests/L3Lint.Security.Tests` | Log privacy at trace level in text and JSON log formats; card numbers in every output format, fix, convert, a Stripe body and a CSV cell; YAML tags, alias bombs, duplicate keys and deep nesting; JSON depth, duplicates, size, line items, UTF-8; NDJSON per-record limit; rule pack path traversal; MCP `get_schema` names; a runtime no-network probe |
| Benchmarks | `tests/L3Lint.Benchmarks`, results in `docs/dev/benchmarks.md` | Spec example, 10 and 100 lines, library 10-line transaction, 100 lines plus JSON rendering, Stripe detect and map, scrubbing, 10,000-record NDJSON batch with 4 and 8 workers; a memory probe for NFR-003 |
| Mutation testing | `stryker-config.json`, `docs/dev/mutation-testing.md` | Core with Core.Tests and Rules.Tests, threshold 80 |
| Security review | `docs/dev/security-review.md` | Every threat in the Architecture tab with its control, its tests and its gaps |

## Test runs (Debug build, `dotnet test --project <proj> --no-build`)

| Project | Total | Passed | Failed | Note |
| --- | --- | --- | --- | --- |
| Architecture.Tests | 105 | 105 | 0 | New |
| Core.Tests | 137 | 137 | 0 | 16 new property tests |
| Rules.Tests | 107 | 106 | 1 | 3 new property tests pass. The failure is the existing US-09 timing test (`LatencyTests`), which also fails when run alone (median 2.1 to 3.9 ms in Debug on a loaded machine; the Release benchmark gives 0.39 ms). The lead is moving it to `tests/L3Lint.Performance` |
| Security.Tests | 66 | 57 | 9 | New; the 9 failures are gaps G1, G2, G5, G6 and G7 below |
| Fuzz.Tests | 11 | 8 | 3 | New; the json, config and rulepack smoke runs fail on gaps G1, G3, G4 and G5 below; csv passes |
| Adapters.Tests | 749 | 749 | 0 | Unchanged |
| Reporting.Tests | 110 | 110 | 0 | Unchanged |
| Cli.Tests | 89 | 89 | 0 | Unchanged |
| Mcp.Tests | 40 | 40 | 0 | Unchanged |

`dotnet build L3Lint.slnx --no-incremental` succeeds with no warnings. `dotnet format <project> --verify-no-changes` reports nothing for the seven projects WP14 touched. Benchmarks: see docs/dev/benchmarks.md. Stryker: see docs/dev/mutation-testing.md (General pack 51.27% from a partial test set; Core not finished in 20 minutes).

## Findings that need a fix in src

Each one has a failing test, named in `docs/dev/security-review.md`, until it is fixed.

1. G1: YAML nested more than 63 levels escapes as `JsonException` (config, rule packs, CSV maps).
2. G2: YAML nested a few thousand levels deep crashes the process with a stack overflow inside YamlDotNet.
3. G3: YamlDotNet's `ArgumentException` and `InvalidOperationException` on malformed YAML escape `YamlJson.Parse`.
4. G4: config integers beyond Int32 or Int64 throw `OverflowException`; a NUL in a config path throws `ArgumentException`.
5. G5: invalid UTF-8 inside a JSON string throws `InvalidOperationException` from the scrubber or a detector (exit 3, should be exit 2).
6. G6: `DirectoryRulePackSource.Open` lets a sibling folder that shares the pack folder's name prefix through.
7. G7: an oversized NDJSON record is reported without its line number.

Design gaps without a failing test: D1, batch memory grows with the number of records (NFR-003 missed); D2, an NDJSON line is buffered whole before the per-record limit applies.

Other notes for the lead:

- `PointerComparer` is a total order on pointers whose segments are array indexes or names that start with a letter (every canonical and gateway path). It is not transitive when siblings mix digit-led names and indexes: "/2" < "/10" (numbers), "/10" < "/1a" and "/1a" < "/2" (ordinal). Only reachable through unmapped keys such as a card number under an object key "1a". Output stays deterministic for the same input, so this is recorded, not tested.
- An explicit `!!str` tag in YAML is ignored (`mcc: !!str 5047` becomes a number and fails the schema).
- The console logger writes to `Console.Error`, not to the CLI environment's error writer, so `tests/L3Lint.Cli.Tests` never saw log lines. `tests/L3Lint.Security.Tests` captures the process console instead.
- The Security and Fuzz test projects use only public APIs. The in-process CLI tests call the `l3lint` entry point through reflection on `Assembly.EntryPoint`, because `CliApplication` and `CliEnvironment` are internal to Cli.Tests.
