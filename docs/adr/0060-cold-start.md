# ADR-0060: Cold start keeps pack validation and loads packs in parallel

- Status: Accepted
- Date: 2026-11-05

## Context

NFR-002 asks for a cold CLI start under 300 ms with ReadyToRun. On the reference machine (docs/dev/benchmarks.md) the win-x64 single-file binary took 327 ms (median of 15) to lint the spec example and 272 ms for `l3lint version`; `l3lint --help` took 111 ms, so about 160 ms went to building the linter. Timing inside the process showed where: validating the five embedded packs against `rulepack.schema.json` took about 70 ms (57 ms of it on the first pack, mostly one-time setup inside JsonSchema.Net), YAML parsing about 26 ms, and compiling rules the rest. Setting up the console logger took another 20 ms, although at the default `warn` level most runs log nothing. JIT compilation of code that ReadyToRun did not cover was about 38 ms in all.

The fastest change, skipping schema validation for the embedded packs, conflicts with FR-019 and the Architecture tab ("At startup the loader validates each pack against `rulepack.schema.json`"), so it was not taken.

## Decision

- `LinterBuilder.Build` loads packs on thread-pool tasks (read, hash, parse, validate) and then uses the results in the order the packs were added. The first failure in that order is the one reported, so results never depend on thread timing (NFR-001). The schema cache builds each schema once even when several threads ask for it.
- The CLI's logger factory (`DeferredLoggerFactory`) answers `IsEnabled` from the minimum level and creates the console logger on the first message that passes it.

## Alternatives considered

- Skipping runtime validation of embedded packs, since tests validate them: rejected, FR-019 is a MUST.
- Turning off dynamic code so JsonSchema.Net's `RegexOptions.Compiled` patterns are interpreted: measured, no gain (each compiled pattern costs about 2 ms).
- Warming code paths with dummy data on a background thread: rejected as fragile.

## Consequences

Measured on the same machine after the change (median of 21 runs): `lint` of the spec example 280 ms, `version` 223 ms, `--help` 109 ms. The margin under 300 ms is small, so a slower machine may miss it; the CI timing step does not measure cold start. Building a linter in a library now starts thread-pool work.

## Spec IDs affected

NFR-002, FR-019, NFR-001
