# Architecture decision records

| ADR | Title |
| --- | --- |
| [ADR-0001](0001-public-sources-only.md) | Rules come only from public sources |
| [ADR-0002](0002-no-employer-material.md) | No employer material |
| [ADR-0003](0003-offline-core.md) | The shipped tool is offline |
| [ADR-0004](0004-no-card-numbers.md) | Card numbers are masked before anything reads them |
| [ADR-0005](0005-advisory-language.md) | Advisory language only |
| [ADR-0006](0006-rules-as-data.md) | Rules are versioned data plus small C# predicates |
| [ADR-0007](0007-one-dotnet-codebase.md) | One .NET 10 codebase |
| [ADR-0008](0008-canonical-model.md) | A canonical payload model sits in the middle |
| [ADR-0009](0009-decimal-money.md) | Money is decimal with an explicit ISO 4217 exponent |
| [ADR-0010](0010-apache-license.md) | Apache-2.0 for code and rule packs |
| [ADR-0011](0011-canonical-model-shape.md) | The C# model mirrors the canonical JSON |
| [ADR-0012](0012-field-selectors.md) | Field selectors use JSON Pointer with one wildcard |
| [ADR-0013](0013-required-field-semantics.md) | What counts as a missing field |
| [ADR-0014](0014-arithmetic-defaults.md) | Arithmetic defaults and edge cases |
| [ADR-0015](0015-visa-detail-indicator.md) | Visa detail indicator is checked for presence only |
| [ADR-0016](0016-single-processor-rules.md) | Single-processor rules use a profiles attribute |
| [ADR-0017](0017-processor-profiles.md) | Processor profiles are embedded YAML |
| [ADR-0018](0018-visa-large-ticket.md) | VISA-LT-001 runs only with a rate table |
| [ADR-0019](0019-brand-selection.md) | Brand selection: auto and all |
| [ADR-0020](0020-tier-rollup.md) | Tier rollup |
| [ADR-0021](0021-sarif-locations.md) | SARIF locations come from the token index |
| [ADR-0022](0022-mechanical-fixes.md) | Mechanical fixes only |
| [ADR-0023](0023-card-number-detection.md) | Card-number detection follows the Architecture algorithm exactly |
| [ADR-0024](0024-pack-integrity.md) | Rule pack integrity: manifest and lock file |
| [ADR-0025](0025-amex-unit-codes.md) | Amex unit codes |
| [ADR-0026](0026-asterisked-adapter-fields.md) | Asterisked adapter fields |
| [ADR-0027](0027-action-tool-source.md) | Action input tool-source |
| [ADR-0028](0028-agent-eval-harness.md) | Agent evaluation harness |
| [ADR-0029](0029-initial-versions.md) | Initial versions and pack dates |
| [ADR-0030](0030-spec-export-location.md) | Spec export location |
| [ADR-0031](0031-meta-package.md) | The Hosting project ships as the meta-package L3Lint.Hosting |
| [ADR-0032](0032-eligibility-gating.md) | Eligibility gating and supersession |
| [ADR-0033](0033-engine-diagnostics.md) | Engine diagnostics outside the rule catalog |
| [ADR-0034](0034-batch-findings.md) | Batch findings |
| [ADR-0035](0035-resource-strings.md) | Where user-facing text lives |
| [ADR-0036](0036-tolerant-mapping.md) | Tolerant canonical mapping |
| [ADR-0037](0037-input-errors.md) | Input errors versus findings |
| [ADR-0038](0038-check-kind-parameters.md) | Check kind parameters |
| [ADR-0039](0039-visa-l3-010-once.md) | VISA-L3-010 reports once per transaction |
| [ADR-0040](0040-generated-rule-examples.md) | Rule examples come from compact definitions |
| [ADR-0041](0041-lock-file.md) | Lock file creation |
| [ADR-0042](0042-context-overrides.md) | Context overrides build an effective transaction |
| [ADR-0043](0043-schema-strictness.md) | Canonical schema strictness |
| [ADR-0044](0044-project-url.md) | Project and docs URLs |
| [ADR-0045](0045-jsonschema-net-version.md) | JsonSchema.Net pinned to 8.0.5 |
| [ADR-0046](0046-mcp-sdk-version.md) | ModelContextProtocol SDK pinned to 1.x |
| [ADR-0047](0047-fix-and-convert-output.md) | Output shapes for fix and convert |
| [ADR-0048](0048-ndjson-size-limit.md) | The file size limit applies per NDJSON record |
| [ADR-0049](0049-cli-text-output.md) | CLI text output uses LF and ASCII tables |
| [ADR-0050](0050-sarif-output-details.md) | SARIF output details beyond the mapping table |
| [ADR-0051](0051-junit-mapping.md) | JUnit XML mapping, batch findings and input errors |
| [ADR-0052](0052-markdown-job-summary.md) | Markdown job summary layout, escaping and size limit |
| [ADR-0053](0053-snapshot-tests-without-verify.md) | Snapshot tests use checked-in expected files, not Verify |
| [ADR-0054](0054-report-rule-metadata.md) | Reports carry the metadata of the rules that fired |
| [ADR-0055](0055-sarif-schema-validation.md) | The OASIS SARIF schema is validated with draft-07 rules |
| [ADR-0056](0056-default-currency.md) | Default currency for payloads that carry none |
| [ADR-0057](0057-corpus-metrics.md) | How the corpus counts recall and false positives |
| [ADR-0058](0058-timing-tests-run-alone.md) | Timing tests run alone, in their own project |
| [ADR-0059](0059-bounded-batch-memory.md) | Batch results are kept compressed and reports stream |
| [ADR-0060](0060-cold-start.md) | Cold start keeps pack validation and loads packs in parallel |
| [ADR-0061](0061-action-location.md) | The Action lives in action/ until the owner moves it to l3lint-action |
