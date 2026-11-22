# Dependencies

Every package l3lint references, pinned once in `Directory.Packages.props` (Central Package Management) and locked in each project's `packages.lock.json`. CI restores with `--locked-mode`. Licenses were read from each package's `.nuspec` in the local NuGet cache on 2026-10-30; where the metadata names no license, the source is given.

## Shipped (in the tool, libraries or MCP server)

| Package | Version | License | Used by | Why |
| --- | --- | --- | --- | --- |
| CsvHelper | 33.1.0 | MS-PL OR Apache-2.0 | Adapters | CSV parsing for the CSV adapter |
| JsonSchema.Net | 8.0.5 | MIT | Core | JSON Schema validation of payloads, packs, config, profiles, rate tables. 8.x is the last MIT line; 9.x moved to a different license (ADR-0045). Brings Json.More.Net 2.2.0, JsonPointer.Net 6.0.1 and Humanizer.Core 3.0.1 (all MIT). |
| YamlDotNet | 18.1.0 | MIT | Core | YAML for packs, config, profiles, CSV maps and rate tables (tags refused, ADR-0004 hardening) |
| Microsoft.Extensions.Logging.Abstractions | 10.0.14 | MIT | Core, Hosting | Logging without a provider dependency in Core |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.14 | MIT | Hosting | `AddL3Lint` registration |
| Microsoft.Extensions.Options | 10.0.14 | MIT | Hosting | Options binding for `AddL3Lint` |
| Microsoft.Extensions.DependencyInjection | 10.0.14 | MIT | Cli | Service provider for console logging |
| Microsoft.Extensions.Logging.Console | 10.0.14 | MIT | Cli | Logs to standard error, text or JSON lines |
| System.CommandLine | 2.0.14 | MIT | Cli | Command and option parsing |
| Spectre.Console | 0.57.2 | MIT | Cli | Tables in `rules list` (ADR-0049) |
| ModelContextProtocol | 1.4.1 | Apache-2.0 | Mcp | Official MCP C# SDK, 1.x as the spec names (ADR-0046) |
| ModelContextProtocol.AspNetCore | 1.4.1 | Apache-2.0 | Mcp | Streamable HTTP transport |

ASP.NET Core and the base class library come with the .NET 10 shared framework (MIT).

## Build only

| Package | Version | License | Why |
| --- | --- | --- | --- |
| Microsoft.CodeAnalysis.PublicApiAnalyzers | 5.6.0 | MIT | Locks the public API of every library (PublicAPI.*.txt) |

## Tests, benchmarks and tools (never shipped)

| Package | Version | License | Why |
| --- | --- | --- | --- |
| xunit.v3 | 4.0.2 | Apache-2.0 | Test framework on Microsoft.Testing.Platform |
| Shouldly | 4.3.0 | BSD-3-Clause | Assertions |
| coverlet.MTP | 10.1.0 | MIT | Line coverage under Microsoft.Testing.Platform |
| FsCheck, FsCheck.Xunit.v3 | 3.6.0 | BSD-3-Clause | Property tests |
| Microsoft.Extensions.TimeProvider.Testing | 10.11.0 | MIT | Fixed clock (`FakeTimeProvider`) |
| NetArchTest.Rules | 1.3.2 | MIT (from the repository's LICENSE file, https://github.com/BenMorris/NetArchTest; the package metadata names no license) | Architecture tests |
| BenchmarkDotNet | 0.15.8 | MIT | Benchmarks |
| SharpFuzz | 2.3.0 | MIT | Fuzz harness |
| Anthropic | 13.3.0 | Apache-2.0 | Agent evaluation tool (tools/L3Lint.AgentEval) |

Verify is not used: version 33 fails the build unless the consumer declares a sponsorship, license or exemption, which only the owner can do (ADR-0053).

## Local .NET tools (`dotnet-tools.json`)

| Tool | Version | Why |
| --- | --- | --- |
| docfx | 2.81.0 | Documentation site |
| CycloneDX | 6.3.1 | SBOM |
| dotnet-stryker | 5.0.0 | Mutation testing |
| dotnet-reportgenerator-globaltool | 5.5.11 | Merging coverage reports in CI |

## Vendored data and schemas

| Item | Location | Source | License or terms |
| --- | --- | --- | --- |
| ISO 4217 currency table (165 currencies) | src/L3Lint.Core/Data/iso4217.csv | ISO 4217 list published 2026-09-17 | Facts (codes and exponents) |
| ISO 3166 alpha-2 and alpha-3 table (249 codes) | src/L3Lint.Adapters/Data/iso3166.csv | ISO 3166-1 codes | Facts (codes) |
| Amex unit codes (720) | src/L3Lint.Rules.Amex/Pack/lists/amex-uom.csv | Mastercard Gateway public page (see docs/dev/verification.md) | Facts (codes) |
| Mastercard unit codes, MCC lists | src/L3Lint.Rules.*/Pack/lists/*.csv | Public processor pages named in each file | Facts (codes) |
| OASIS SARIF 2.1.0 schema | tests/L3Lint.Reporting.Tests/Schemas/sarif-schema-2.1.0.json | OASIS, SARIF 2.1.0 Errata 01 | Test only; see that folder's README |
| JUnit XSD | tests/L3Lint.Reporting.Tests/Schemas/junit-10.xsd | Jenkins xUnit plugin | MIT, notice inside the file; test only |

Details on the two vendored schemas, with every page consulted, are in [dependencies-reporting.md](dependencies-reporting.md).

## Third-party GitHub Actions (pinned by commit SHA)

| Action | Version | Commit |
| --- | --- | --- |
| actions/checkout | v7.0.1 | 3d3c42e5aac5ba805825da76410c181273ba90b1 |
| actions/setup-dotnet | v6.0.0 | a98b56852c35b8e3190ac28c8c2271da59106c68 |
| actions/upload-artifact | v7.0.2 | cf430e030ddbb5b0abf93d22962f4752f3646cd9 |
| actions/download-artifact | v8.0.2 | 9000827ccba6bdab643e8b6fd33ac0654aef8333 |
| actions/attest-build-provenance | v4.2.2 | 4d101475d8b20a2381f78447822ac1eab6504dd8 |
| actions/configure-pages | v6.0.0 | 45bfe0192ca1faeb007ade9deae92b16b8254a0d |
| actions/upload-pages-artifact | v5.0.0 | fc324d3547104276b827a68afc52ff2a11cc49c9 |
| actions/deploy-pages | v5.0.1 | 368f82528645a54fb793d4d04e342629a3f51346 |
| github/codeql-action/upload-sarif | v4.39.1 | 9b3254085494b55a0f7c4c2b3d2d52cc712ce370 |

Commits were read from each release page on 2026-10-29.
