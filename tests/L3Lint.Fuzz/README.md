# l3lint fuzz harness

This project is the SharpFuzz harness for T-43 (NFR-006): "Fuzzed parsers and adapters", with "24 hours of fuzzing without a crash" on the v1.0 launch checklist. It is a console program, not a test project. A short deterministic run of the same targets is part of every `dotnet test` in `tests/L3Lint.Fuzz.Tests`.

## Targets

| Target | Input | What it runs |
| --- | --- | --- |
| `json` | JSON or NDJSON bytes | `RawDocument.ParseJson`, then `Map` and `Lint`, then the CLI's standard input batch path (`LintRunner`) and all five report writers |
| `config` | YAML text | `L3LintConfiguration.Parse`, the `.l3lint.yaml` loader |
| `csv` | CSV bytes | The CSV adapter with one fixed column map (`fixtures/adapters/csv/01-erp-two-invoices.map.yaml`, embedded), then `Map`, `Lint`, the batch path and the writers |
| `rulepack` | A `*.rules.yaml` body | The rule pack loader and compiler. The harness writes a matching manifest and supplies the list and example files of the shipped packs, so inputs get past the checksum. A pack that loads lints the spec example |

A target may refuse an input only with a documented exception: `InputException` (including `FormatDetectionException`), `YamlFormatException`, `ConfigurationException` or `RulePackException`. Any other exception, a hang or a process crash is a finding.

## Build

The fuzzers instrument the l3lint assemblies, so publish first and instrument the published copies.

```bash
dotnet tool install --global SharpFuzz.CommandLine
dotnet publish tests/L3Lint.Fuzz -c Release -o artifacts/fuzz
for dll in artifacts/fuzz/L3Lint.Core.dll artifacts/fuzz/L3Lint.Adapters.dll \
           artifacts/fuzz/L3Lint.Hosting.dll artifacts/fuzz/L3Lint.Reporting.dll \
           artifacts/fuzz/YamlDotNet.dll artifacts/fuzz/CsvHelper.dll; do
  sharpfuzz "$dll"
done
```

Instrument only the code under test. Do not instrument `L3Lint.Fuzz.dll` or `SharpFuzz.dll`.

## Seed corpora

Each target starts from inputs that already exist in the repository.

```bash
mkdir -p corpus/json corpus/config corpus/csv corpus/rulepack
cp tests/L3Lint.Core.Tests/SpecExamples/canonical-example.json fixtures/adapters/*/*.json corpus/json/
cp tests/L3Lint.Core.Tests/SpecExamples/config-example.yaml src/L3Lint.Cli/Templates/l3lint.yaml corpus/config/
cp fixtures/adapters/csv/*.csv corpus/csv/
cp src/L3Lint.Rules.*/Pack/*.rules.yaml corpus/rulepack/
```

## Run with libFuzzer (Linux)

Use the `libfuzzer-dotnet` driver from the SharpFuzz project (it is built from source with clang; see the SharpFuzz README). The driver passes the harness only its assembly path, so the target name comes from the `L3LINT_FUZZ_TARGET` environment variable and the mode defaults to `libfuzzer`.

```bash
L3LINT_FUZZ_TARGET=json ./libfuzzer-dotnet \
  --target_path=dotnet \
  --target_arg=artifacts/fuzz/L3Lint.Fuzz.dll \
  -max_total_time=86400 -timeout=10 -rss_limit_mb=2048 \
  -artifact_prefix=findings/json- \
  corpus/json
```

Replace `json` with `config`, `csv` or `rulepack` for the other targets. `-max_total_time=86400` is the 24 hours the launch checklist asks for. libFuzzer writes each crashing input to `findings/`.

## Run with AFL++ (Linux)

```bash
AFL_SKIP_BIN_CHECK=1 afl-fuzz -i corpus/json -o findings/json -t 10000 -m none -- \
  dotnet artifacts/fuzz/L3Lint.Fuzz.dll json afl
```

`AFL_SKIP_BIN_CHECK=1` is needed because `dotnet` is not an instrumented binary; SharpFuzz reports coverage itself. Run one `afl-fuzz` per target.

## Reproduce a finding

```bash
dotnet artifacts/fuzz/L3Lint.Fuzz.dll json repro findings/json-crash-1234
```

`repro` runs one input once without the fuzzer and prints `Accepted` or `Rejected`. An undocumented exception ends the process with its stack trace, the way the fuzzer saw it. The smoke tests save the smallest failing input of each kind under the temp folder (`l3lint-fuzz-smoke/<target>/`) and print the matching `repro` command.

## Known limits

- YAML nested a few thousand levels deep overflows the stack inside YamlDotNet and ends the process. The smoke tests keep YAML nesting at or below 300 levels so `dotnet test` itself survives; `L3Lint.Security.Tests` covers the deep case in a child process. See `docs/dev/security-review.md`.
- Every finding the smoke run reports is listed in `docs/dev/quality-wp14-report.md` with the input that triggers it.
