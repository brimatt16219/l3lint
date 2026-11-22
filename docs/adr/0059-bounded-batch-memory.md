# ADR-0059: Batch results are kept compressed and reports stream

- Status: Accepted
- Date: 2026-11-05

## Context

NFR-003 asks for under 500 MB of memory on a 1 GB NDJSON file. The reader already streamed one record at a time, but the run kept every `TransactionResult` as objects (about 1 KB each with one finding), and the JSON, SARIF and JUnit writers built the whole document in memory before writing it. The WP14 memory probe measured 1,131 MB peak for 128 MB of NDJSON and 2,463 MB for 256 MB, about 10 GB projected for 1 GB (docs/dev/benchmarks.md, security review D1). The NDJSON reader also collected a whole line before the per-record size check (D2), so one huge line without a newline sat in memory.

The report must still list every transaction (results schema), outputs must stay byte-identical for identical input (NFR-001), and the public `LintReport` and writer APIs should not change.

## Decision

- `LintRunner` puts finished results into `TransactionResultStore` (src/L3Lint.Hosting/Runs). It implements `IReadOnlyList<TransactionResult>`, so `LintReport.Transactions` keeps its type. Every full block of 1,024 results is serialized with source-generated System.Text.Json and compressed with zlib (fastest level). The last, partial block stays as objects, so runs under 1,024 transactions never serialize anything. Reading decompresses one block at a time; the indexer caches the last block read.
- The summary is counted in one streaming pass, and `--max-findings` truncation builds a new store instead of copying lists.
- The JSON and JUnit writers stream to the output through `Utf8TextWriterStream`, a write-only stream that decodes UTF-8 onto the `TextWriter`. JUnit counts each suite in a first pass and builds one test case at a time.
- `ReportIndex` keeps row numbers, not transactions, and reads transactions back from the report on demand.
- SARIF picks the findings it keeps (GitHub's result limit, ADR-0050) with two passes: count findings per severity, then keep the earliest findings of each severity up to its quota. With three severities this is the same choice as sorting by severity and position, without holding every finding.
- Markdown counts transactions per brand with `CountBy` and walks failing transactions lazily; its page size limit (ADR-0052) already bounds the text it keeps.
- `NdjsonLines` stops collecting a line once it passes the record limit, keeps counting its bytes, and returns it marked as too large, so the runner reports an input error for that line (ADR-0048).

## Alternatives considered

- Spilling results to a temporary file: works for any size, but leaves files behind when the process is killed and needs cleanup rules per platform. Compressed blocks in memory cost about 64 bytes per transaction, which keeps a 1 GB file far below the target.
- Writing the JSON report while linting: the results schema puts `inputs` (with each file's hash and transaction count) before `transactions`, so the header is only known after the last record. Reordering properties would change the published output.
- Capping the number of transactions in a report: loses data the user asked for.

## Consequences

Measured on the WP14 machine with the memory probe (tests/L3Lint.Benchmarks, `memory <MB>`): a 1,026.6 MB NDJSON file with 921,666 payloads peaks at 158 MB working set and keeps 57 MB live, at 5,791 payloads per second with 8 workers. Every writer gives the same bytes from the store as from plain objects; `tests/L3Lint.Corpus.Tests/ResultStoreTests.cs` checks this over the whole corpus with 7-result blocks, including `--max-findings`. Reading a report's transactions twice decompresses them twice, which costs CPU, not memory. A library user who builds a `LintReport` by hand is unaffected.

## Spec IDs affected

NFR-003, NFR-006, NFR-001, FR-022, FR-023, FR-024
