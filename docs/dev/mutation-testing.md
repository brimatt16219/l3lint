# Mutation testing

The Test and delivery tab asks for Stryker.NET on Core and the rule packs, with a mutation score of at least 80%, checked weekly. `.github/workflows/mutation.yml` runs it every Monday (and on demand), one job per target, and fails a job whose score is under 80%. It does not run in the pull request workflow.

## Configuration

Two configuration files at the repository root, both with `test-runner: mtp` (xunit.v3 on Microsoft.Testing.Platform; preview in Stryker 5.0.0), `mutation-level: Standard`, `coverage-analysis: perTest`, thresholds high 90, low 80, break 80, and `ignore-methods` for logging calls and argument guard helpers, whose mutants say nothing about rule behavior.

- `stryker-config.json` targets `L3Lint.Core` with `tests/L3Lint.Core.Tests` (unit and property tests) and `tests/L3Lint.Rules.Tests` (rule checks and linter-level properties).
- `stryker-packs.json` has no target of its own; the job passes `--project L3Lint.Rules.<Pack>.csproj`. Its test projects are `tests/L3Lint.Rules.Tests` and `tests/L3Lint.Adapters.Tests`.

In project mode the project under test must be a direct reference of the test project Stryker runs from, so `tests/L3Lint.Rules.Tests` references Core and the five packs directly, besides Hosting.

## Running it

Run Stryker from a test project folder, not from the repository root: from the root it finds `L3Lint.slnx`, switches to solution mode and pulls in every test project, including the fuzz and security tests.

```bash
dotnet tool restore                      # dotnet-stryker 5.0.0 from dotnet-tools.json

# Core
cd tests/L3Lint.Core.Tests
dotnet stryker --config-file ../../stryker-config.json --skip-version-check

# One rule pack (General, Visa, Mastercard, Amex or Discover)
cd tests/L3Lint.Rules.Tests
dotnet stryker --config-file ../../stryker-packs.json --project L3Lint.Rules.General.csproj --skip-version-check
```

`--skip-version-check` stops Stryker from calling out to check for a newer version. Reports go to `StrykerOutput/<timestamp>/reports/` (ignored by git) unless `--output` names another folder. Keep the timing tests out of a mutation run: a mutant that only slows code down would fail a timing assertion and count as killed. They live in `tests/L3Lint.Performance`, which neither configuration uses.

Stryker swaps mutated assemblies into the test projects' Debug output while it runs, so do not run those test projects in Debug at the same time.

## Results (2026-11-06)

| Target | Mutants tested | Killed | Timeout | Survived | No coverage | Score | Gate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| L3Lint.Rules.General | 197 | 186 | 2 | 8 | 1 | 95.43% | met |
| L3Lint.Rules.Visa | 38 | 33 | 0 | 2 | 3 | 86.84% | met |
| L3Lint.Rules.Mastercard | 88 | 85 | 0 | 3 | 0 | 96.59% | met |
| L3Lint.Rules.Amex | 2 | 2 | 0 | 0 | 0 | 100% | met |
| L3Lint.Rules.Discover | 1 | 1 | 0 | 0 | 0 | 100% | met |
| L3Lint.Core | 2,790 | 2,353 | 23 | 85 | 329 | 85.16% | met |

Score = (killed + timeout) / (killed + timeout + survived + no coverage). Compile-error and ignored mutants are not scored, and neither are the 88 Core mutants that Stryker's preview test runner reported as "failed to test". The packs are mostly YAML; their mutants sit in the few C# checks.

How the General pack got there: the WP14 trial (2026-10-30) ran from `tests/L3Lint.Adapters.Tests` only and scored 51.27%, because the rule tests could not be included. With the direct references and `tests/L3Lint.Rules.Tests` in the run, it scored 70.05%: the rule examples prove that a rule fires, but most survivors changed what a finding says (message arguments, the actual value, the line number, the message variant, the patch value) or the GEN-DQ-007 batch logic. `tests/L3Lint.Rules.Tests/GeneralCheckDetailTests.cs` now checks the full finding of each GEN custom check, and `StaticBatchDataTests` checks GEN-DQ-007's share threshold (exactly 80% fires), its order of findings, the anchor to the first transaction in run order, and once-per-transaction counting. Each expected message is the rule's template filled with the values the test put in.

After that run, 11 General mutants were left (10 survived, 1 without coverage). Two were real gaps: the tests checked a patch's path and value but not its operation, in `CurrencyCheck` and `SequenceCheck`; both tests now assert `add`, and the table shows the run after that change. The other nine cannot change any output:

- `StaticBatchDataRule` sort order (`OrderByDescending`, `ThenByDescending`): the engine sorts every finding by transaction, path and rule ID afterwards (NFR-001), so the rule's own order never shows.
- The initial values of `StaticBatchDataRule.Entry.Pointer` and `Value`, which `Set` always overwrites.
- `state.OrderKey <= entry.FirstKey`: two transactions never share an order key.
- `_entries.Count <= MaxTrackedValues`: differs only at the 100,001st distinct value.
- The `expected` message argument in `SequenceCheck`: the message fills `{expected}` from the finding's expected value as well.
- `"total"` in `TransactionTotalCheck`'s line components: an invalid line total already leaves no expected amount, so the check stops before the guard matters.
- `CardNumberCheck` (no coverage): a card number in a field that has no canonical path, which no adapter fixture contains.

How Core got there: its first full run (2026-11-06) scored 79.53%, just under the gate. The largest groups of survivors were in the canonical reader and the schema messages. The reader's path prefixes only matter when a value has the wrong type, and no Core test mapped wrongly typed fields; `tests/L3Lint.Core.Tests/CanonicalReaderTests.cs` now gives every canonical field the wrong type and checks the exact set of invalid pointers (ADR-0036), and checks the detection scores. `SchemaMessageTests` pins each JSON Schema keyword to its user message (NFR-011) and each canonical field to its expectation. The rerun scored 85.16%.
