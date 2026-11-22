# Reporting dependencies (WP8)

WP8 adds no NuGet package and no runtime dependency. The SARIF, JUnit and Markdown writers use only the .NET base library (`System.Text.Json.Utf8JsonWriter`, `System.Xml.XmlWriter`, `System.Security.Cryptography.SHA256`), as Architecture.md asks ("SARIF, JUnit and the Jaro-Winkler similarity are hand-written").

## Test-only files

These schemas are vendored under `tests/L3Lint.Reporting.Tests/Schemas/` and used only by tests. They are not shipped in any package. Details, hashes and license notes are in that folder's README.md.

| Name | Source URL | Version | License |
| --- | --- | --- | --- |
| OASIS SARIF 2.1.0 JSON schema (`sarif-schema-2.1.0.json`) | https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json | SARIF 2.1.0 Plus Errata 01, OASIS Standard, 28 August 2023 (SHA-256 `c3b4bb2d6093897483348925aaa73af03b3e3f4bd4ca38cef26dcb4212a2682e`) | OASIS IPR Policy: copies may be made and distributed with the copyright notice; the file is unmodified |
| Jenkins xUnit plugin JUnit schema (`junit-10.xsd`) | https://github.com/jenkinsci/xunit-plugin/blob/master/src/main/resources/org/jenkinsci/plugins/xunit/types/model/xsd/junit-10.xsd | `master` branch, downloaded 2026-10-26 (SHA-256 `a1a816f58d1bf95ebabf371994df0b9246dee66ea9572fbec4f9296f1b2c0ff6`) | MIT (Copyright (c) 2014, Gregory Boissinot), notice inside the file |

## Test packages

The reporting test project uses packages already pinned in `Directory.Packages.props`: xunit.v3, Shouldly, coverlet.MTP, JsonSchema.Net 8.0.5 (MIT, ADR-0045) and Microsoft.Extensions.TimeProvider.Testing.

`Verify.XunitV3` 33.3.4 was removed from the reporting test project. Its build fails with SponsorCheck error SC021 until the consuming project declares a license property for the Verify maintenance fee, and that declaration is the owner's decision, not a contributor's (ADR-0053). Snapshot tests compare with plain checked-in files under `tests/L3Lint.Reporting.Tests/Expected/`. The lead engineer removes the Verify pin from `Directory.Packages.props` in the main repository; WP8 does not edit that file.

## Pages consulted

- GitHub Docs, "SARIF support for code scanning": https://docs.github.com/en/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning (limits: 10 MB gzipped per upload, 25,000 results per run with the top 5,000 shown; `runAutomationDetails.id` category parsing; required descriptor and result properties; relative URIs).
- GitHub Docs, GHES 3.1 version of the same page, as cited in Interfaces.md: https://docs.github.com/en/enterprise-server@3.1/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning
- OASIS SARIF 2.1.0 Plus Errata 01, complete specification and Notices: https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/sarif-v2.1.0-errata01-os-complete.html
- OASIS IPR Policy: https://www.oasis-open.org/policies-guidelines/ipr/
- OASIS SARIF TC repository license terms: https://github.com/oasis-tcs/sarif-spec/blob/main/LICENSE.md
- Jenkins xUnit plugin repository (license): https://github.com/jenkinsci/xunit-plugin
- windyroad JUnit schema, considered and not used: https://github.com/windyroad/JUnit-Schema (Apache-2.0)
- Verify maintenance fee terms: https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md
