# Progress

Read this file, PLAN.md, SPEC-DIGEST.md and RULE-DESIGN.md first after any context reset.

Last updated: 2026-11-06.

## Status

The build directive is complete; docs/dev/IMPLEMENTATION_REPORT.md is the final report. `dotnet test --solution L3Lint.slnx -c Release` passes 1,680 tests (0 skipped); the timing tests (tests/L3Lint.Performance, ADR-0058) pass when run alone in Release.

| WP | State | Notes |
| --- | --- | --- |
| WP0 to WP12 | done | engine, five packs, adapters, all writers, CLI, Hosting, MCP server, Action (ADR-0001 to ADR-0058) |
| WP13 | done | `FixtureGen corpus`: 156 rule examples, 400 seeded, 200 known-good, 10 PCI; tests/L3Lint.Corpus.Tests: recall 100% (420 of 420), false positives 0 of 200, 1 vs 8 threads identical (ADR-0057) |
| WP14 | done (merged) | architecture, fuzz, security and property tests, benchmarks, Stryker. Its tests found G1 to G7, D1, D2, a non-transitive `PointerComparer` and an ignored `!!str` tag; all fixed in `src` (docs/dev/security-review.md) |
| WP15 | done (merged) | DocsGen, DocFX site, 18 articles, root files, package READMEs; docfx builds with 0 warnings |
| WP16 | done | ci.yml, release.yml, docs.yml and the weekly mutation.yml written (never run); packaging dry run repeated on the final code (packaging-dry-run.md) |
| WP17 | done | tools/L3Lint.AgentEval builds and exits 0 without credentials; not run against the API (it spends tokens) |
| WP18 | done | traceability, verification, dependencies, cli-options, coverage (Core 93.5%, packs 93.7% to 100%), benchmarks (NFR-003 met by ADR-0059, NFR-002 cold start met by ADR-0060), mutation testing (Core 85.16%, packs 86.84% to 100%), README quick start rechecked on the release binary, IMPLEMENTATION_REPORT.md |

## Next steps

The owner's steps, in order, are in docs/dev/IMPLEMENTATION_REPORT.md, section 5.

## Decisions made while building (beyond the ADR list)

- Test runner: xunit.v3 4.x runs on Microsoft.Testing.Platform, so `global.json` opts in (`"test": {"runner": "Microsoft.Testing.Platform"}`) and coverage uses `coverlet.MTP`. Run tests with `dotnet test --project <proj>`.
- JsonSchema.Net pinned to 8.0.5 (ADR-0045, license); ModelContextProtocol pinned to 1.4.1 (ADR-0046).
- Resource strings: `Resources/Strings.resx` per project with MSBuild strongly typed generation (`src/Directory.Build.targets`). Reference them as `Resources.Strings.Name` without a using directive, because `dotnet format` removes the using before generation runs.
- `.editorconfig` suppresses CA1720 and CA1863, each with a written reason.
- Spec examples are copied verbatim to `tests/L3Lint.Core.Tests/SpecExamples/`.
- The NuGet ID of the hosting library is `L3Lint.Hosting` because NuGet IDs are case-insensitive and the tool is `l3lint` (ADR-0031).
- A masked `RawDocument` returns masked JSON from `GetText()`; the original bytes stay internal for positions and hashing. Found by the CLI fix test.
- Input arguments reject tokens that start with a dash, because System.CommandLine otherwise reads an unknown option as a file name.
- A file named by a flag that cannot be read is a usage error (exit 2); one named by the config file is a configuration error (exit 3).
- Text output always prints override notes (Interfaces tab: "Every override prints a one-line note").

## Research inputs

- `docs/dev/research/adapters.md`: done.
- `docs/dev/research/amex-uom.csv`: done (720 codes).
- `docs/dev/research/facts-and-profiles.md`: done (Visa Level 2 sunset confirmed, profiles sourced; unconfirmed items listed at its end).

## Open risks

- The workflows (ci, release, docs, mutation) have never run; the first run on GitHub may need small fixes.
- Cold start meets NFR-002 with about 20 ms to spare on the reference machine (ADR-0060); a slower machine may miss it.
- Stryker 5.0.0's Microsoft.Testing.Platform runner is a preview and leaves some mutants untested ("failed to test"); those are left out of the score.
- One run of `rules test` inside the Cli tests exited 3 once under full-suite load and did not recur in repeated runs (a regex timeout under CPU load is the main suspect, unproven). Watch for it on the first CI runs.
- Subagents stop at usage limits; resume them with SendMessage instead of starting new ones.
