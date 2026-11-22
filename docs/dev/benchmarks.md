# Benchmarks (T-44)

Recorded 2026-10-30 from `tests/L3Lint.Benchmarks` (BenchmarkDotNet) and its memory probe; the batch and memory numbers were measured again on 2026-11-05 after ADR-0059. Each number is compared with its spec target below.

## Machine

From the BenchmarkDotNet header:

- BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
- Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
- .NET SDK 10.0.400; runtime .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT x86-64-v3
- 16 GB RAM
- Job: ShortRun (3 warmups, 3 iterations, 1 launch), Release build, `InProcessEmitToolchain`

The default out-of-process toolchain failed to build its generated project here: the parallel build of the referenced projects into one `ArtifactsPath` raced (MSB3030, a rule pack DLL "not found" while copying). The in-process toolchain runs the same code in the host process, which loses process isolation but not accuracy at these sizes. Pass `--inProcess` as shown below until that is fixed.

## Results

| Benchmark | What one operation does | Mean | StdDev | Allocated |
| --- | --- | --- | --- | --- |
| `Spec_example_2_lines` | Parse, scrub, detect, map, validate and lint the spec example (2 lines) from JSON bytes | 205.2 us | 0.8 us | 172.7 KB |
| `Library_10_lines_transaction` | `ILinter.Lint` on an in-memory 10-line `Transaction` (US-09) | 386.8 us | 1.3 us | 435.3 KB |
| `Lint_10_lines` | The 10-line payload from JSON bytes | 469.9 us | 9.5 us | 482.0 KB |
| `Lint_100_lines` | The 100-line payload from JSON bytes | 3,768.0 us | 41.6 us | 4,176.6 KB |
| `Lint_100_lines_and_render_json` | 100 lines linted and rendered as the JSON results document (the engine part of `lint_payload`) | 4,355.9 us | 22.7 us | 4,716.6 KB |
| `Stripe_detect_and_map` | A Stripe PaymentIntent body (fixture `stripe/01-complete-level3.json`) through detection and mapping | 47.3 us | 0.5 us | 64.2 KB |
| `Scrub_one_test_card` | Card-number scrub of the spec example with 4111 1111 1111 1111 in a description | 25.7 us | 2.2 us | 25.8 KB |
| `Lint_10000_ndjson_records`, 4 workers | `LintRunner` over a 10,000-record NDJSON file (the CLI batch path) | 1.421 s | 0.022 s | 1.6 GB |
| `Lint_10000_ndjson_records`, 8 workers | The same with 8 workers, the CLI default on this machine | 1.197 s | 0.044 s | 1.6 GB |

The 10-line and 100-line payloads repeat line 1 of the spec example (10 x 12.50 less 5.00 = 120.00) with the header tax and amount recomputed from the spec's formulas, so the arithmetic rules pass and the run measures the normal path. Allocated is the total allocated per operation, not memory held. The batch rows were re-run on 2026-11-05 after ADR-0059 (compressed result blocks); before it they were 1.311 s and 1.223 s.

## Memory probe (NFR-003)

`memory <megabytes>` writes an NDJSON file of that size, lints it with `LintRunner` (8 workers), renders the JSON report to a null stream and prints the peak working set, the GC generation sizes, and the live heap that the finished report keeps after a full collection.

Before ADR-0059 (2026-10-30), when the run kept every result as objects and the JSON writer built the whole document as one string:

| NDJSON file | Records | Lint time | Payloads per second | Peak working set | Managed heap after the run |
| --- | --- | --- | --- | --- | --- |
| 128.3 MB | 115,208 | 16.5 s | 6,965 | 1,131 MB | 684 MB |
| 256.7 MB | 230,416 | 31.7 s | 7,270 | 2,463 MB | 1,898 MB |

Two causes, found with the GC generation sizes: about 1 KB of live objects per transaction result, and the report rendered into a `MemoryStream` and then a string, which sat on the large object heap (136 MB of large objects after the last GC at 64 MB of input).

After ADR-0059 (2026-11-05), with results kept in compressed blocks and every writer streaming:

| NDJSON file | Records | Lint time | Payloads per second | Peak working set | Live heap holding the report |
| --- | --- | --- | --- | --- | --- |
| 256.7 MB | 230,416 | 36.7 s | 6,284 | 107 MB | 17 MB, 75 bytes per transaction |
| 1,026.6 MB | 921,666 | 159.2 s | 5,791 | 158 MB | 57 MB, 64 bytes per transaction |

Peak memory now grows slowly with input size (about 50 MB more for four times the input) instead of linearly.

## Against the spec targets

| Target | Spec | Measured | Verdict |
| --- | --- | --- | --- |
| US-09 | A 10-line payload lints in under 1 ms on a warm process | 0.387 ms mean (`Library_10_lines_transaction`); 0.470 ms from JSON bytes | Met |
| NFR-002 | p95 under 50 ms for one payload with up to 100 line items, warm | 3.77 ms mean, StdDev 0.04 ms (`Lint_100_lines`). ShortRun does not report a p95, but the spread is two orders of magnitude below the target | Met |
| NFR-002 | Cold CLI start under 300 ms with ReadyToRun | win-x64 single-file ReadyToRun binary, median of 21 runs on a quiet machine: `lint` of the spec example 280 ms, `l3lint version` 223 ms, `l3lint --help` 109 ms. Before ADR-0060: 327 ms, 272 ms and 111 ms | Met, with a small margin (ADR-0060) |
| US-08 | `lint_payload` returns within 200 ms for 100 line items | 4.36 ms for the engine work the tool does (`Lint_100_lines_and_render_json`). The MCP transport is not in this number; the end-to-end check over the MCP client is a timing test in the lead's `tests/L3Lint.Performance` | Engine part met; end to end not measured here |
| NFR-003 throughput | At least 2,000 payloads per second on 4 cores in batch mode | 7,037 per second with 4 workers (10,000 / 1.421 s) on 4 physical cores; 8,354 per second with 8 workers; 5,791 per second over the 1 GB probe file | Met |
| NFR-003 memory | Under 500 MB for a 1 GB NDJSON file | 158 MB peak working set for a 1,026.6 MB file (921,666 payloads), JSON report rendered | Met after ADR-0059. Before it: 2,463 MB for 256 MB (security review D1) |

The brief for WP14 called the batch throughput "NFR-002"; in the PRD the throughput and memory targets are NFR-003 and NFR-002 is single-payload latency. Both are covered above.

## How to run

```bash
# All benchmarks, short job (about 3 minutes here)
dotnet run -c Release --project tests/L3Lint.Benchmarks -- --job short --inProcess --filter "*"

# One class
dotnet run -c Release --project tests/L3Lint.Benchmarks -- --job short --inProcess --filter "*BatchBenchmarks*"

# Memory probe for NFR-003 (size in MB)
dotnet run -c Release --project tests/L3Lint.Benchmarks -- memory 256
```

Cold start (NFR-002) was timed on the published single-file binary (`dotnet publish src/L3Lint.Cli -c Release -r win-x64 -o <folder>`), one warm-up run, then 21 timed runs per command, taking the median. `hyperfine` gives the same with less typing where it is installed. In PowerShell:

```powershell
$exe = "<folder>\l3lint.exe"
$times = 1..21 | ForEach-Object { (Measure-Command { & $exe lint tests\L3Lint.Core.Tests\SpecExamples\canonical-example.json | Out-Null }).TotalMilliseconds }
($times | Sort-Object)[10]
```

Use `--artifacts <folder>` to keep the BenchmarkDotNet reports out of the working tree. The spec's CI gate (Test and delivery tab, "Performance": short run every PR, no regression over 15%) compares these numbers with the last release baseline; that comparison is workflow work (WP16).

## Mutation testing

See docs/dev/mutation-testing.md.
