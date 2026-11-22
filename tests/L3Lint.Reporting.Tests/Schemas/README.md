# Vendored output schemas

The reporting tests validate SARIF and JUnit output against these published schemas (Test and delivery, "Output conformance"). Both files are unmodified copies; the tests read them from the build output and never fetch anything over the network. The JSON output is validated against the repository's own `schemas/results.schema.json`, which the test project links in.

| File | What it is | Source | Version | SHA-256 | License |
| --- | --- | --- | --- | --- | --- |
| `sarif-schema-2.1.0.json` | OASIS SARIF 2.1.0 JSON schema | https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/schemas/sarif-schema-2.1.0.json (identical to https://github.com/oasis-tcs/sarif-spec/blob/main/sarif-2.1/schema/sarif-schema-2.1.0.json) | SARIF 2.1.0 Plus Errata 01, OASIS Standard, 28 August 2023 | `c3b4bb2d6093897483348925aaa73af03b3e3f4bd4ca38cef26dcb4212a2682e` | OASIS IPR Policy (see below) |
| `junit-10.xsd` | JUnit XML schema used by the Jenkins xUnit plugin (the Ant and Surefire report shape) | https://github.com/jenkinsci/xunit-plugin/blob/master/src/main/resources/org/jenkinsci/plugins/xunit/types/model/xsd/junit-10.xsd | `master` branch, downloaded 2026-10-26 | `a1a816f58d1bf95ebabf371994df0b9246dee66ea9572fbec4f9296f1b2c0ff6` | MIT, notice inside the file |

## License notes

**SARIF schema.** Copyright © OASIS Open 2023. All Rights Reserved. The schema is part of the OASIS work product "Static Analysis Results Interchange Format (SARIF) Version 2.1.0 Plus Errata 01". The Notices section of that specification (https://docs.oasis-open.org/sarif/sarif/v2.1.0/errata01/os/sarif-v2.1.0-errata01-os-complete.html) lets anyone copy and distribute the work and derivative works that help implement it, on condition that the copyright notice and the Notices section travel with the copies and the work itself is not modified. The full policy is the OASIS IPR Policy at https://www.oasis-open.org/policies-guidelines/ipr/. This copy is unmodified and is used only to test l3lint's own output; it is not shipped in any package.

**JUnit schema.** Copyright (c) 2014, Gregory Boissinot, MIT License. The license text is the comment at the top of `junit-10.xsd` and stays with the file. The Jenkins xUnit plugin repository is also MIT licensed. It was chosen over the windyroad `JUnit.xsd` (Apache-2.0) because its license travels inside the file and because it describes the attributes CI dashboards read (`tests`, `failures`, `errors`, `system-out`) without requiring time stamps or host names.

## How the tests load them

`Schemas.cs` in the test project loads both files from the build output.

- The SARIF file declares JSON Schema draft-04, which JsonSchema.Net 8 does not implement. The test replaces the `$schema` URI with the draft-07 URI in memory before building the schema. This is safe for this file: it uses only local `$ref`s and none of the draft-04 forms that changed in later drafts (boolean `exclusiveMinimum` or `exclusiveMaximum`); the test asserts both facts each time it loads the file. Format checks (`uri`, `uri-reference`, `date-time`) are turned on. See ADR-0055.
- The JUnit file is loaded into an `XmlSchemaSet` and documents are validated with `XmlReaderSettings.ValidationType = Schema`, reporting warnings as well as errors.

## Updating

Download the file again from the source above, check the license has not changed, update the SHA-256 in this table and in `docs/dev/dependencies-reporting.md`, and run the reporting tests.
