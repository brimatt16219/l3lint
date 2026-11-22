# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The tool, the library packages and the MCP server share one version. Rule packs have their own versions, listed with each release.

## [1.0.0] - Unreleased

The first release. The owner sets the date when it is published.

### Added

- **Rule engine** with 78 rules in five rule packs, each pack at version 1.0.0: general (23 rules), Visa (26), Mastercard (17), American Express (9) and Discover (3). Every rule cites public sources and carries a confidence label, effective dates, a passing and a failing example, and a reference page.
- **Visa Commercial Enhanced Data Program checks**: the Product 3 fields, placeholder and filler detection, explicit tax exemption, and the retired Level 2 rules, kept with an end date of 2026-04-17 so `--as-of` can check older payloads.
- **Mastercard Data Rate II and III checks**, including the Mastercard unit-of-measure list. American Express and Discover format and limit checks, with a note that no enhanced-data rate applies.
- **Arithmetic checks** for line totals, the transaction total and header tax, with configurable tolerance and rounding.
- **Likely tier per brand** (Product 3, Data Rate III and so on) with the statuses meets, at risk, blocked and no benefit.
- **Card-number masking** before any rule reads a payload, GEN-PCI-001, `--strict-pci` and `pci.allowFields`.
- **Input adapters** for Stripe, Adyen, Braintree, Cybersource, Authorize.net and Checkout.com requests and for CSV with a YAML column map, with format detection and source paths for every finding.
- **CLI** (`l3lint`) with `lint`, `explain`, `fix`, `convert`, `rules list`, `rules show`, `rules test`, `schema`, `init` and `version`. Inputs: files, folders, globs, NDJSON and standard input.
- **Outputs**: text, JSON (`results.schema.json`), SARIF 2.1.0 within GitHub's limits, JUnit XML and Markdown, plus GitHub workflow annotations.
- **Configuration** with `.l3lint.yaml` (validated, unknown keys are errors) and `.l3lint.lock` to pin rule pack versions.
- **Mechanical fixes** as an RFC 6902 JSON Patch or a patched copy. Descriptions, commodity codes and tax amounts are never invented.
- **Cost estimates** from a user-supplied rate table, labeled as estimates. No rates ship with the tool.
- **Ten processor profiles**: generic, stripe, adyen, braintree, cybersource, authorizenet, checkoutcom, jpmorgan, mastercard-gateway-amex and amex-advice-text.
- **MCP server** (`l3lint-mcp`) with six read-only tools, resources and prompts, over stdio and a loopback-only Streamable HTTP transport with Host, Origin and bearer token checks.
- **GitHub Action** that lints payload files, uploads SARIF, writes a job summary and annotations, and fails the job by `fail-on`.
- **.NET library** with `ILinter`, `LinterBuilder`, `AddL3Lint()` and `LintRunner`, plus extension points for custom rules, checks, adapters and writers.
- **Documentation site** with a generated page for every rule, a generated CLI reference and generated schema references.

[1.0.0]: https://github.com/brimatt16219/l3lint/releases/tag/v1.0.0
