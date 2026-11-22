# ADR-0058: Timing tests run alone, in their own project

- Status: Accepted
- Date: 2026-10-30

## Context

Two user stories set latency targets: US-08 (`lint_payload` within 200 ms for 100 line items) and US-09 (a 10-line payload in under 1 ms on a warm process). `dotnet test --solution` runs every test assembly in parallel, and coverage instrumentation slows every line. Measured inside that run, the 10-line lint took 2 to 3.5 ms; measured alone in Release it takes well under 1 ms. A time taken while other assemblies compete for the CPU says nothing about the code.

## Decision

The two timing tests live in `tests/L3Lint.Performance`, an xunit project marked as neither a test project nor a testing-platform application, so `dotnet test --solution` and the coverage run leave it out. CI runs it as its own step, alone and in Release, after the main suite: `dotnet run --project tests/L3Lint.Performance -c Release`. The thresholds are the spec's (1 ms median of 1,001 warm calls; 200 ms median of 21 warm MCP calls over stdio). The project compiles methods fully optimized on first call (tiered compilation off), so "warm" does not depend on how fast background tier-up runs. BenchmarkDotNet (`tests/L3Lint.Benchmarks`) gives the detailed numbers.

## Alternatives considered

- Keeping the tests in their assemblies with a non-parallel collection: other assemblies still ran at the same time.
- Measuring process CPU time instead of elapsed time: still inflated by contention for shared cores and caches.
- Loosening the thresholds: rejected; the targets come from the spec.

## Consequences

A developer runs the timing tests with one command. They are not part of `dotnet test --solution`, so a local run of the whole solution does not check latency; CI does.

## Spec IDs affected

US-08, US-09, NFR-002
