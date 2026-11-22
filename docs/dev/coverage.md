# Coverage

Recorded 2026-11-06 with the same steps as the `coverage` job in `.github/workflows/ci.yml`: a Debug build, then every `tests/*.Tests` project run with coverlet (`--coverlet --coverlet-output-format cobertura`), merged with ReportGenerator into `TestResults/report/Summary.json`. The timing tests (tests/L3Lint.Performance) are not part of it (ADR-0058).

The gate is at least 90% line coverage for Core and for each rule pack (directive gate 3, NFR-013). All six pass.

| Assembly | Line coverage | Covered lines | Coverable lines | Branch coverage | Gate |
| --- | --- | --- | --- | --- | --- |
| L3Lint.Core | 93.5% | 3,803 | 4,067 | 83.7% | 90%, met |
| L3Lint.Rules.General | 99.7% | 448 | 449 | 93.6% | 90%, met |
| L3Lint.Rules.Visa | 93.7% | 90 | 96 | 90.3% | 90%, met |
| L3Lint.Rules.Mastercard | 97.7% | 87 | 89 | 88.7% | 90%, met |
| L3Lint.Rules.Amex | 100% | 34 | 34 | 100% | 90%, met |
| L3Lint.Rules.Discover | 100% | 3 | 3 | no branches | 90%, met |
| L3Lint.Adapters | 96.7% | 1,876 | 1,940 | 90.7% | none |
| L3Lint.Reporting | 95.6% | 1,808 | 1,891 | 86.8% | none |
| L3Lint.Hosting | 90.2% | 932 | 1,033 | 85.8% | none |
| l3lint (CLI) | 96.4% | 739 | 766 | 73.1% | none |
| l3lint-mcp | 86.3% | 323 | 374 | 80.0% | none |

The rule packs are mostly YAML data: their coverable lines are the few C# checks and pack classes. The rules themselves are covered by the 156 rule examples and the corpus (docs/dev/traceability.md).

Two assemblies in the merged report are not l3lint code and are left out of the table: Humanizer (8,315 coverable lines, 0% covered), a dependency of JsonPointer.Net that JsonSchema.Net brings in, and L3Lint.Fuzz, the fuzz harness that the fuzz smoke tests drive (99.3%). They are why the report's overall figure (53.6%) is far below the per-assembly numbers.

## How to reproduce

```bash
dotnet tool restore
dotnet build L3Lint.slnx -c Debug
for project in tests/*.Tests/*.Tests.csproj; do
  name=$(basename "$project" .csproj)
  dotnet test --project "$project" --no-build --coverlet --coverlet-output-format cobertura \
    --coverlet-file-prefix "$name" --results-directory TestResults/coverage
done
dotnet tool run reportgenerator -reports:"TestResults/coverage/*.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"JsonSummary;MarkdownSummaryGithub"
```

The per-assembly numbers are in `TestResults/report/Summary.json` (`.coverage.assemblies[]`).
