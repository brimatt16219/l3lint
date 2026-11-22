# Packaging dry run

First run on 2026-10-30; repeated on 2026-11-06 on the final code, which is what `./artifacts/` now holds. Both ran on Windows (x64) with .NET SDK 10.0.400, into `./artifacts/` (gitignored). Nothing was pushed or published. The release workflow (`.github/workflows/release.yml`) runs the same commands on GitHub-hosted runners.

## Commands

```bash
dotnet pack L3Lint.slnx -c Release -o artifacts/packages
dotnet publish src/L3Lint.Cli -c Release -r <rid> -o artifacts/binaries/<rid>   # for each of the six RIDs
dotnet tool run dotnet-CycloneDX -- src/L3Lint.Cli/L3Lint.Cli.csproj --recursive --include-project-references \
  --exclude-filter Microsoft.CodeAnalysis.PublicApiAnalyzers --output artifacts/sbom --filename l3lint.cdx.json --output-format Json
dotnet tool run dotnet-CycloneDX -- src/L3Lint.Mcp/L3Lint.Mcp.csproj --recursive --include-project-references \
  --exclude-filter Microsoft.CodeAnalysis.PublicApiAnalyzers --output artifacts/sbom --filename l3lint-mcp.cdx.json --output-format Json
```

Archives (`.zip` for Windows, `.tar.gz` with mode 755 for Linux and macOS) and their SHA-256 files were then made from each binary. All six runtime identifiers cross-published from one Windows machine with ReadyToRun. The whole sequence, version taken from `action/VERSION`, is `dotnet pack` with `-p:Version`, then for each runtime `dotnet publish` with `-p:Version` and the archive step, then the two SBOM commands.

## Results

| Artifact | Count | Checks |
| --- | --- | --- |
| NuGet packages | 11 (`l3lint` tool, `L3Lint.Core`, `L3Lint.Hosting`, `L3Lint.Reporting`, `L3Lint.Adapters`, five `L3Lint.Rules.*` packs, `L3Lint.Mcp`), each with a symbol package | Every package carries README.md, LICENSE, NOTICE and the Apache-2.0 expression. `l3lint` has package type DotnetTool; `L3Lint.Mcp` has DotnetTool and McpServer, includes `.mcp/server.json`, and its README carries `mcp-name: io.github.brimatt16219/l3lint`. |
| Single-file binaries | 6 (win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64) | One file each (about 97 MB uncompressed; archives 37.7 MB for linux-arm64 to 41.6 MB for win-x64), no loose symbol or XML files. The win-x64 archive matched its SHA-256 file, and the extracted binary passed every README quick-start command and started a lint in 276 ms (median of 21). |
| SBOMs | 2 (CycloneDX 1.7 JSON: the tool with 30 components, the MCP server with 23) | Runtime dependencies and project references only; the build-only analyzer is excluded. |

## Smoke tests of the packed artifacts

- `dotnet tool install l3lint --tool-path <temp> --add-source artifacts/packages --version 1.0.0` installed the tool; `l3lint version` printed 1.0.0 and the five packs; linting the spec example printed one warning and exited 0.
- `dotnet dnx L3Lint.Mcp@1.0.0 --add-source artifacts/packages --yes -- --version` ran the MCP server package and printed `l3lint-mcp 1.0.0`, the way the spec's client configuration launches it (`dnx L3Lint.Mcp --yes`).

## Not done locally

Package signing, artifact attestations, the NuGet push, the MCP Registry publish and the GitHub release need the owner's secrets and repository; `release.yml` holds those steps.
