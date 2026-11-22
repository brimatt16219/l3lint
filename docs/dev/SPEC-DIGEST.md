# l3lint spec digest

This file restates every requirement, rule, list and interface contract in the l3lint spec set, with the section it comes from. It is the checklist for implementation and for the final audit. When this digest and the spec disagree, the spec wins; fix the digest.

## 0. Spec files and abbreviations

The owner's export landed in the repository root, not in `./spec` (see ADR-0030). The seven files are read-only.

| Abbreviation | File | Tab |
| --- | --- | --- |
| OV | `Overview.md` | Overview |
| PR | `Product requirements.md` | 1. Product requirements |
| CBR | `Card brand rules.md` | 2. Card brand rules |
| AR | `Architecture.md` | 3. Architecture |
| IF | `Interfaces.md` | 4. Interfaces |
| TD | `Test and delivery.md` | 5. Test and delivery |
| SRC | `Sources.md` | Sources |

Precedence (directive section 1): hard constraints, then normative spec tables, then directive clarifications (ADR-0011 to ADR-0029), then spec prose, then engineering judgment.

## 1. Product summary [OV]

- Lints commercial card Level 2 and Level 3 payloads against Visa, Mastercard, American Express and Discover before submission. Explains each likely downgrade and its fix.
- Surfaces: CLI `l3lint`, MCP server `l3lint-mcp`, GitHub Action `l3lint-action`, .NET library `L3Lint.Core`. One rule engine.
- Not a gateway, not a guarantee, not a tax engine, not a card data store, not a rate database [OV "What it is not"].
- Brand landscape [OV table]: Visa CEDP (Product 3 since Oct 2025, Level 2 sunset April 2026); Mastercard Data Rate I, II, III (UOM tightened Oct 2025); Amex Level 2 by agreement, no public L3 program, advice text 4 by 40; Discover no L2/L3 pricing, 98 line-item limit.
- Key dates [OV]: 2026-04 (17 or 18) Visa L2 sunset; 2026-02-24 verification resumes; 2025-10 MC UOM; 2025-10-17/18 CEDP Product 3; 2025-04 CEDP launch with 0.05% fee; 2024-10 CEDP announced.
- Conventions [OV]: RFC 2119 keywords; IDs `FR-nnn`, `NFR-nnn`, `US-nn`; rule IDs `<SCOPE>-<AREA>-<nnn>` with scope GEN, VISA, MC, AMEX, DISC and area FMT, L2, L3, ARITH, DQ, ELIG, PCI (catalog also uses LT in VISA-LT-001); money in examples is USD major units.

## 2. Decisions D1 to D10 [OV "Key decisions"]

| ID | Decision | ADR |
| --- | --- | --- |
| D1 | Rules only from public sources; every rule cites its URL | ADR-0001 |
| D2 | No employer material; no adapters for employer gateways | ADR-0002 |
| D3 | Core offline; no network, no telemetry | ADR-0003 |
| D4 | No card numbers; Luhn-passing values rejected or masked before processing | ADR-0004 |
| D5 | Advisory language only ("likely to downgrade", "meets the documented requirement", never "qualifies") | ADR-0005 |
| D6 | Rules are versioned data plus small C# predicates; ID, brand, effective dates, severity, confidence, source | ADR-0006 |
| D7 | One .NET 10 / C# 14 codebase for CLI, MCP, Action, library | ADR-0007 |
| D8 | Canonical payload model in the middle; gateways are adapters | ADR-0008 |
| D9 | Money is `decimal` with ISO 4217 currency and exponent; minor units converted at adapter boundary | ADR-0009 |
| D10 | Apache-2.0 for code and rule packs | ADR-0010 |

## 3. Goals, non-goals, metrics [PR]

- G1 Coverage, G2 Explanation (field path, requirement, source, fix), G3 Reach (identical results on every surface), G4 Freshness (30 days), G5 Safety (no card data, no network, no telemetry).
- NG1 no tax calculation; NG2 no submission; NG3 no issuer prediction; NG4 no hosted service; NG5 US only; NG6 no industry addenda.
- Metrics: recall at least 95% on seeded defects; false positives at most 2% on known-good; single payload p95 under 50 ms (100 lines); batch at least 2,000 payloads/s on 4 cores; rule freshness 30 days; agent repair rate at least 90% within two MCP tool calls; adoption (not testable).
- Jobs left to people: commodity codes, real descriptions, tax treatment are never invented [PR "Users and jobs"].

## 4. User stories [PR "User stories"]

| ID | Story | Acceptance criteria |
| --- | --- | --- |
| US-01 | Lint one canonical payload from the terminal | `l3lint lint payload.json` prints findings grouped by severity then a one-line summary; exit 0 no errors, 1 at least one error, 2 usage or input error; each finding shows rule ID, JSON path, message, fix, source link |
| US-02 | Lint a gateway format | `--format <name>` selects an adapter, else auto-detect; detection correct on at least 99% of corpus; tie exits 2 and lists candidates; findings point at paths in the original format |
| US-03 | Context-aware eligibility | `--brand visa --card-product purchasing --mcc 5045` runs eligibility rules; consumer card or excluded MCC yields one informational finding ("no enhanced-data rate available") and suppresses rate-related errors |
| US-04 | Batch lint | folder, glob or NDJSON; 10,000 payloads finish with a summary table of counts per rule; `--max-findings` caps output, summary still counts everything |
| US-05 | Explain a rule | `l3lint explain VISA-L3-004` shows requirement, rationale, source URL, confidence label, effective dates, one passing and one failing example |
| US-06 | Safe fixes | `l3lint fix --dry-run` prints RFC 6902 JSON Patch; `--write` applies to a copy; only mechanical fixes (line totals, unit codes, trim to length, explicit indicators); never invents descriptions, commodity codes, tax amounts |
| US-07 | CI gate | Action uploads SARIF and annotates changed files; `fail-on: error, warning, none` controls job result |
| US-08 | Agent validation | `lint_payload` returns within 200 ms for 100 line items; invalid input is a tool execution error (`isError`), not a protocol error |
| US-09 | Runtime check | `ILinter.Lint(payload, options)` thread-safe, no I/O after startup; 10-line payload under 1 ms warm |
| US-10 | Cost estimate | requires user rate table `--rates rates.yaml`; none shipped; output labeled "estimate" with rate table name and date |
| US-11 | Rule updates | packs are separate versioned packages validated by schema and checksum on load; `l3lint rules list` shows each pack's version, effective dates, source count |
| US-12 | Card number safety | Luhn-passing value matching a card pattern raises `GEN-PCI-001`; masked to last four in every output; `--strict-pci` stops processing |
| US-13 | Lint as of a date | `--as-of 2026-03-01` applies rules effective then, so Visa Level 2 rules still apply |
| US-14 | ERP CSV | `l3lint lint invoices.csv --format csv --map erp-map.yaml` maps columns and groups rows by invoice ID |

MVP release gate [PR "Scope by release"]: US-01, US-03, US-05, US-09, US-12, US-13 pass and corpus meets recall and precision. v1.0: all 14 stories, Action green in sample repo, MCP passes Inspector checks.

## 5. Functional requirements [PR "Functional requirements"]

| ID | Area | Requirement | Pri | Release | Stories |
| --- | --- | --- | --- | --- | --- |
| FR-001 | Input | Canonical JSON (schema v1) from file, stdin, glob or NDJSON | MUST | MVP | US-01, US-04 |
| FR-002 | Input | Validate against canonical JSON Schema; schema errors are `GEN-FMT` findings with JSON paths | MUST | MVP | US-01 |
| FR-003 | Input | Adapters: Stripe PaymentIntent, Adyen payments, Braintree transaction sale | MUST | MVP | US-02 |
| FR-004 | Input | Adapters: Cybersource REST payments, Authorize.net createTransaction, Checkout.com payments | MUST | v1.0 | US-02 |
| FR-005 | Input | Auto-detect format with confidence score; tie exits 2 and lists candidates | SHOULD | v1.0 | US-02 |
| FR-006 | Input | CSV input driven by YAML column map; rows grouped by invoice ID | MUST | v1.0 | US-14 |
| FR-007 | Input | Report every finding at the path in the original format via the adapter path map | MUST | MVP | US-02 |
| FR-008 | Input | Major or minor units per adapter; exponent from embedded ISO 4217 table | MUST | MVP | US-01 |
| FR-009 | Context | Accept brand, card product (consumer, business, corporate, purchasing, fleet, unknown), MCC, merchant country, processor profile, transaction date | MUST | MVP | US-03 |
| FR-010 | Context | `--as-of <date>` selects rules in force on that date | MUST | MVP | US-13 |
| FR-011 | Context | Processor profiles carry limits (line-item caps, field lengths) | SHOULD | v1.0 | US-02 |
| FR-012 | Rules | Engine loads packs; rule has ID, title, brands, level, default severity, applicability, check, message, fix, sources, confidence, effective dates | MUST | MVP | All |
| FR-013 | Rules | Visa CEDP pack; Mastercard Data Rate II and III pack | MUST | MVP | US-01 |
| FR-014 | Rules | Amex and Discover packs (format, lengths, limits, no-benefit notices) | MUST | v1.0 | US-03 |
| FR-015 | Rules | Arithmetic checks with configurable tolerance and rounding mode | MUST | MVP | US-01 |
| FR-016 | Rules | Per-payload data quality heuristics: placeholder text, zero-filled values, merchant name as description | MUST | MVP | US-01 |
| FR-017 | Rules | Batch heuristics: identical description or product code across a configurable share of transactions | SHOULD | v1.0 | US-04 |
| FR-018 | Rules | Config overrides severity, disables rules, or runs only selected rules | MUST | MVP | US-07 |
| FR-019 | Rules | Validate pack schema and SHA-256 checksum on load; refuse invalid packs with exit 3 | MUST | MVP | US-11 |
| FR-020 | Rules | `l3lint rules test` runs each rule's embedded pass and fail examples | SHOULD | v1.0 | US-11 |
| FR-021 | Output | Human-readable text with color honoring `NO_COLOR`, grouped by transaction and severity | MUST | MVP | US-01 |
| FR-022 | Output | JSON conforming to the results schema, including engine and pack versions | MUST | MVP | US-08 |
| FR-023 | Output | SARIF 2.1.0 within GitHub's code scanning subset | MUST | v1.0 | US-07 |
| FR-024 | Output | JUnit XML and Markdown | SHOULD | v1.0 | US-07 |
| FR-025 | Output | Exit codes 0 clean, 1 errors found, 2 usage or input error, 3 internal or pack error | MUST | MVP | US-01 |
| FR-026 | Output | `explain <ruleId>` prints requirement, rationale, sources, confidence, dates, examples | MUST | MVP | US-05 |
| FR-027 | Output | `fix` produces RFC 6902 patch (dry run) or patched copy, mechanical only | SHOULD | v1.0 | US-06 |
| FR-028 | Output | Cost estimates from user rate table, labeled estimates | SHOULD | v1.0 | US-10 |
| FR-029 | Interfaces | MCP server with lint, explain, list, map, fix, schema tools over stdio and Streamable HTTP | MUST | v1.0 | US-08 |
| FR-030 | Interfaces | GitHub Action runs CLI, uploads SARIF, writes job summary | MUST | v1.0 | US-07 |
| FR-031 | Interfaces | .NET library with `ILinter` and DI registration | MUST | MVP | US-09 |
| FR-032 | Interfaces | `l3lint init` writes `.l3lint.yaml` and sample payloads | SHOULD | v1.0 | US-07 |
| FR-033 | Safety | Detect and mask card numbers everywhere; never log payload values unless `--log-values` | MUST | MVP | US-12 |
| FR-034 | Safety | Limits: 25 MB per file, 1,000 line items per transaction, JSON depth 64; configurable | MUST | MVP | US-04 |
| FR-035 | Safety | No network calls from Core or CLI; MCP HTTP binds localhost unless told otherwise | MUST | MVP | US-08 |
| FR-036 | Docs | Generate a reference page for every rule from its pack entry | MUST | v1.0 | US-05 |

## 6. Non-functional requirements [PR "Non-functional requirements"]

| ID | Quality | Requirement | Verified by |
| --- | --- | --- | --- |
| NFR-001 | Determinism | Identical input, options, pack versions give byte-identical JSON and SARIF; sort by transaction index, JSON path, rule ID | Golden tests run twice |
| NFR-002 | Performance | p95 under 50 ms for one payload up to 100 lines, warm; cold CLI start under 300 ms with ReadyToRun | BenchmarkDotNet, hyperfine |
| NFR-003 | Throughput | At least 2,000 payloads/s on 4 cores; under 500 MB memory for 1 GB NDJSON | Benchmark suite |
| NFR-004 | Portability | Windows, macOS, Linux; x64 and arm64; .NET tool plus self-contained single-file binaries | CI matrix, six targets |
| NFR-005 | Privacy | No telemetry; no network I/O in Core, CLI or Action; logs carry rule IDs and paths, never values unless `--log-values` | Network-deny test, log scan |
| NFR-006 | Security | Hard limits on size, lines, depth; streaming NDJSON; no dynamic code from packs | Fuzz tests (SharpFuzz), limit tests |
| NFR-007 | PCI | Card numbers masked before any rule, log or output sees them | PAN fixtures across every format |
| NFR-008 | Supply chain | Signed packages; CycloneDX SBOM; provenance attestation; reproducible builds | Release workflow |
| NFR-009 | Compatibility | .NET 10 LTS; MCP protocol revision of ModelContextProtocol C# SDK 1.x | MCP Inspector in CI |
| NFR-010 | Accessibility | Color never the only signal; severity words always print; `NO_COLOR` and `--no-color` | Snapshot tests |
| NFR-011 | Localization | All user-facing strings in resource files; v1 English | Resource completeness test |
| NFR-012 | Observability | JSON-line logs to stderr with `--log-format json`; stdout only results | Output stream tests |
| NFR-013 | Maintainability | Core has no dependency on CLI, MCP, Action; coverage at least 90% in Core and packs | NetArchTest, coverage gate |
| NFR-014 | Documentation | Every command, option, rule, output field documented, generated where possible | Docs build fails on missing entries |

## 7. Scope, risks, open questions [PR]

- Scope by release table: MVP (Visa, MC; canonical, Stripe, Adyen, Braintree; text, JSON, exit codes, explain; CLI, library; packs embedded) and v1.0 (adds Amex, Discover; Cybersource, Authorize.net, Checkout.com, CSV; batch DQ, processor profiles; SARIF, JUnit, Markdown, fix, cost; MCP, Action, init, rule reference site; packs as NuGet packages, `rules test`). Later items are out of scope.
- Risks R1 (Visa ML validation, label inferred rules), R2 (sources disagree: record all, pick conservative, flag in `explain`), R3 (twice-yearly changes: separate packs, effective dates), R4 (employer conflict), R5 (users paste card numbers), R6 (false positives: heuristics are warnings by default), R7 (gateway formats change: contract tests), R8 (MCP evolves: official SDK).
- Open questions: employer clearance (owner); Visa tolerance (default $0.01 per line and total); batch threshold 80% or 95% (decide from fixtures, ADR-0045); MC UOM list second source (verification.md); separate pack license (ADR-0010); final name (owner).

## 8. Canonical field dictionary [CBR "Canonical field dictionary"]

52 fields. Legend: R required for rate, C conditional, O optional, ctx context only, `-` unused. Columns: Visa, MC, Amex, Disc.

Transaction level (34):

| Path | Type | Constraint | V | MC | AX | D |
| --- | --- | --- | --- | --- | --- | --- |
| `schemaVersion` | string | "1.0" | ctx | ctx | ctx | ctx |
| `id` | string | 1 to 64 chars | ctx | ctx | ctx | ctx |
| `amount` | decimal | greater than 0; no more decimals than exponent | R | R | R | R |
| `currency` | string | ISO 4217 alpha-3 | R | R | R | R |
| `transactionDate` | date | ISO 8601; drives effective dates | ctx | ctx | ctx | ctx |
| `channel` | enum | cardPresent, cardNotPresent | ctx | ctx | ctx | ctx |
| `card.brand` | enum | visa, mastercard, amex, discover, unknown | ctx | ctx | ctx | ctx |
| `card.product` | enum | consumer, business, corporate, purchasing, fleet, commercialPrepaid, unknown | ctx | ctx | ctx | ctx |
| `card.productCode` | string | up to 3 chars (MBS, MEF) | - | ctx | - | - |
| `card.isFederalGovernment` | boolean | default false | ctx | - | - | - |
| `card.last4` | string | exactly 4 digits | O | O | O | O |
| `merchant.mcc` | string | 4 digits | ctx | ctx | ctx | ctx |
| `merchant.name` | string | used by DQ checks | ctx | ctx | ctx | ctx |
| `merchant.country` | string | ISO 3166-1 alpha-2 | ctx | ctx | ctx | ctx |
| `merchant.taxId` | string | up to 15 | O | C | O | O |
| `customer.reference` | string | up to 25 | C | R | C | O |
| `order.purchaseOrderNumber` | string | up to 25 (22 Amex) | O | R | C | O |
| `order.invoiceNumber` | string | up to 25 | O | O | O | O |
| `order.orderDate` | date | ISO 8601 | O | O | O | O |
| `tax.amount` | decimal | 0 or more | R | R | C | O |
| `tax.indicator` | enum | taxable, exempt, notProvided | R | R | C | C |
| `tax.rate` | decimal | 0 to 1 | O | O | O | O |
| `tax.exemptionId` | string | required with exempt under Amex profiles | O | O | C | O |
| `amounts.discount` | decimal | 0 or more, header discount | R | O | O | O |
| `amounts.freight` | decimal | 0 or more | R | C | O | O |
| `amounts.duty` | decimal | 0 or more | R | O | O | O |
| `amounts.shippingTax` | decimal | 0 or more | C | O | O | O |
| `amounts.handling` | decimal | 0 or more | O | O | O | O |
| `shipping.fromPostalCode` | string | up to 10 | C | O | O | O |
| `shipping.toPostalCode` | string | up to 10 | C | R | R | O |
| `shipping.toCountry` | string | ISO 3166-1 alpha-2 | C | O | O | O |
| `billing.postalCode` | string | up to 10 | C | O | O | O |
| `amex.requestorName` | string | up to 40 | - | - | C | - |
| `amex.adviceText` | string array | at most 4 lines of 40 | - | - | C | - |

Line item level (`lineItems[]`, 1 to 998 entries) (18):

| Path | Type | Constraint | V | MC | AX | D |
| --- | --- | --- | --- | --- | --- | --- |
| `sequence` | integer | 1-based contiguous; adapters fill from order | R | O | O | O |
| `description` | string | Visa statements show 26 | R | R | C | O |
| `productCode` | string | up to 12 | R | R | O | O |
| `commodityCode` | string | up to 12 alphanumeric, no spaces | R | O | O | O |
| `commodityScheme` | enum | unspsc, nigp, hs, naics, other, unknown | ctx | ctx | ctx | ctx |
| `quantity` | decimal | greater than 0 | R | R | C | O |
| `unitOfMeasure` | string | Visa up to 12; MC list; Amex 2-char X12 | R | R | C | O |
| `unitCost` | decimal | 0 or more, any precision | R | R | C | O |
| `discountAmount` | decimal | 0 or more | R | C | O | O |
| `discountIndicator` | boolean | set when a discount applies | - | C | - | - |
| `taxAmount` | decimal | 0 or more | C | R | O | O |
| `taxRate` | decimal | 0 to 1 | C | C | O | O |
| `taxable` | boolean | line tax indicator | O | R | O | O |
| `taxTypeCode` | string | processor tax type code | - | C | O | - |
| `amountIncludesTax` | boolean | default false; changes arithmetic | ctx | ctx | ctx | ctx |
| `total` | decimal | extended amount | R | R | O | O |
| `debitCreditIndicator` | enum | debit (default), credit | O | R | O | O |
| `detailIndicator` | string | Visa detail indicator, 1 char | C | - | - | - |

Example canonical payload: `INV-20417`, amount 280.36 USD, Visa purchasing, MCC 5047, two lines (gloves 10 x 12.50 less 5.00 = 120.00; gowns 2 x 64.00 = 128.00), tax 17.36, freight 15.00 [CBR]. Expected: no Visa errors, exactly one warning `VISA-L3-010`; under Mastercard it fails `MC-L3-005` (BX and CA not on list); `fix` proposes PCE for both and asks a person to confirm.

## 9. Code lists [CBR, AR]

- Mastercard UOM (32 codes) [CBR "Unit-of-measure code list"]: ACR, ARE, CLT, CMT, EAC, FOT, GLI, ONZ, PTI, QTI, GRM, HAR, INH, KGM, KMT, LTR, MTR, NMI, MLI, MMT, PCE, LBR, CMK, MTK, MMK, FTK, INK, KMK, MIK, YDK, TNE, YRD.
- `fix` free-text unit map [CBR]: each to EAC; piece or pc to PCE; lb to LBR; gal to GLI; ambiguous left to a person.
- Amex UOM: 2-character ANSI X12 list, embedded as `amex-uom.csv` [CBR "American Express requirements"].
- Placeholder list (29 terms, `placeholders.txt` in GEN pack, editable via config) [CBR]: item, items, product, products, service, services, goods, merchandise, misc, miscellaneous, n/a, na, none, null, undefined, test, default, sale, purchase, order, invoice, fee, charge, various, other, general, description, line.
- Visa Product 3 excluded MCC [CBR "Visa Eligibility"]: 3000 to 3999, 4000 to 4010, 4112, 4411, 4511, 4722, 5812, 5814, 7011, 7512, 7513.
- Visa federal government excluded MCC: 8011, 8021, 8031, 8041, 8042, 8043, 8046, 8050, 8062, 8071.
- Visa Level 2 fuel MCC (tax ratio exempt; VISA-L2-003): 4468, 5499, 5541, 5542, 5983.
- Mastercard Level 2 excluded MCC: 3000 to 3350, 3351 to 3500, 3501 to 3999, 4112, 4511, 5812, 7011, 7512, 7513, 7519. Level 3 adds 4468, 5499, 5541, 5542, 5983, 8398.
- Mastercard Level 2 tax-exempt MCC: 4111, 4131, 4215, 4468, 4784, 5499, 5541, 5542, 5983, 7511, 8211, 8220, 8398, 8661, 9211, 9222, 9311, 9399, 9402, 9752.
- Card prefixes [AR "Card-number detection"]: Visa 4; Mastercard 51 to 55 and 2221 to 2720; Amex 34 and 37; Discover 6011, 644 to 649, 65.
- ISO 4217 table embedded (USD 2, JPY 0, BHD 3) [CBR "Arithmetic"].

## 10. Arithmetic [CBR "Arithmetic and reconciliation rules"]

- All math in `decimal` at full precision; round once at comparison to the currency exponent.
- Formula 1 (GEN-ARITH-001): expected_i = round_c(q_i x u_i) - d_i + t_i x I_i, I = 1 when `amountIncludesTax`. Round half away from zero. Fails when reported differs by more than line tolerance.
- Formula 2 (GEN-ARITH-002): A = sum_i (total_i - t_i x I_i) + T + F + D + S - H (header tax T, freight F, duty D, shipping tax S, header discount H).
- Formula 3 (GEN-ARITH-003): when any line carries tax, |T - sum t_i| <= total tolerance.
- Formula 4 (VISA-L2-001, MC-L2-001): r = T / A within 0.001 to 0.22 (Visa) or 0.001 to 0.30 (MC), unless MCC exempt or `tax.indicator` is exempt.
- Settings: `arithmetic.lineTolerance` 1 minor unit; `arithmetic.totalTolerance` 1 minor unit; `arithmetic.toleranceMode` strict or accumulated (1 minor unit per line, capped at 5); `arithmetic.rounding` awayFromZero or toEven.
- A value with more decimals than its exponent raises GEN-FMT-003 before arithmetic; unit cost exempt.
- Worked example: line 1 125.00 - 5.00 = 120.00 pass; line 2 128.00 pass; total 248.00 + 17.36 + 15.00 + 0 + 0 - 0 = 280.36 pass. If line 1 reported 125.00: GEN-ARITH-001 expected 120.00, reported 125.00, difference 5.00; GEN-ARITH-002 also fails and its message names the line failure as the likely cause.

## 11. Data quality detectors [CBR "Data quality heuristics"]

Normalization: trim, collapse inner whitespace, lowercase, strip punctuation except inner hyphens, remove trailing sequence markers ("item 1", "item #2" become "item").

| Detector | Fires when | Default | Rules |
| --- | --- | --- | --- |
| Placeholder text | normalized description in list, 1 char, all digits, or one repeated char | warning; error under Visa | GEN-DQ-001, VISA-DQ-001 |
| Merchant name as description | equals `merchant.name` or Jaro-Winkler at least 0.92 | warning; error under Visa | GEN-DQ-002, VISA-DQ-001 |
| Placeholder code | product code or UOM is "0", "00", all zeros, "na", "n/a", "none", "sku", or equals line sequence | warning; error under Visa | GEN-DQ-003, VISA-DQ-002 |
| Filler customer code | customer code or PO is 111111, 123456, 000000, po, none, n/a, na | warning | GEN-DQ-004 |
| One-line invoice | one line, quantity 1, line total equals amount minus tax and freight | warning | GEN-DQ-005 |
| Generic commodity code | all zeros; UNSPSC not 8 digits, segment outside 10 to 95, or ends in 000000 or 0000 | warning | GEN-DQ-006 |
| Duplicate lines | two lines identical in description, product code, quantity, unit cost | information | GEN-DQ-008 |
| Static batch data | at least 20 transactions, one description or product code on at least 80% | warning | GEN-DQ-007 |
| Zero tax without exemption | `tax.amount` 0 and indicator not exempt | error under Visa | VISA-L3-012 |

Visa escalation sources: Merchant Cost Consulting, Verisave, Finix. Rest of list, similarity threshold and batch percentages are `inferred`. UNSPSC check is structural only.

## 12. Rule catalog (78 rules) [CBR "Rule catalog"]

Severity, check, fix, confidence and effective dates transcribed exactly. "Always" means no date bounds.

### GEN (23)

| Rule | Sev | Check | Fix | Conf | Effective |
| --- | --- | --- | --- | --- | --- |
| GEN-PCI-001 | Error | value 13 to 19 digits, passes Luhn, matches a major brand prefix and length (warning in code fields) | Remove the card number; send only the last four in `card.last4` | policy | Always |
| GEN-FMT-001 | Error | input fails canonical JSON Schema | Correct the field named in the path | policy | Always |
| GEN-FMT-002 | Error | currency missing or not ISO 4217 | Send a 3-letter code such as USD | policy | Always |
| GEN-FMT-003 | Error | amount not positive, or more decimals than currency allows | Round to the currency exponent before sending | policy | Always |
| GEN-FMT-004 | Warning | descriptive text outside letters, digits, space, apostrophe, period, hyphen | Replace them; some brands print them as spaces (Braintree) | processor-doc | Always |
| GEN-FMT-005 | Error | more line items than brand or processor allows | Consolidate lines or split the invoice | processor-doc | Always |
| GEN-FMT-006 | Warning | field longer than brand or processor maximum | Shorten it; otherwise it is truncated | processor-doc | Always |
| GEN-FMT-007 | Error | line sequence numbers missing, duplicated, not contiguous | Number lines 1, 2, 3 in order | processor-doc | Always |
| GEN-ARITH-001 | Error | line total does not match formula 1 | Recompute the line total | processor-doc | Always |
| GEN-ARITH-002 | Error | transaction amount does not match formula 2 | Recompute the amount, or fix the line or summary values | processor-doc | Always |
| GEN-ARITH-003 | Warning | header tax differs from sum of line taxes | Make header tax the sum of line taxes | processor-doc | Always |
| GEN-ARITH-004 | Error | negative quantity or unit cost on a debit line | Mark returns as credit lines with positive values | processor-doc | Always |
| GEN-ARITH-005 | Warning | header discount equals sum of line discounts (counted twice) | Send the discount at one level only | processor-doc | Always |
| GEN-ELIG-001 | Information | consumer card | None; enhanced data earns no rate on consumer cards | processor-doc | Always |
| GEN-ELIG-002 | Information | card product unknown | Pass `card.product` to enable eligibility checks | policy | Always |
| GEN-DQ-001 | Warning | placeholder description | Use the real product or service name | inferred | Always |
| GEN-DQ-002 | Warning | description is the merchant name | Describe the item, not the seller | inferred | Always |
| GEN-DQ-003 | Warning | placeholder product code or unit | Send the real SKU and unit | inferred | Always |
| GEN-DQ-004 | Warning | filler customer code or PO | Ask the buyer for their real reference | inferred | Always |
| GEN-DQ-005 | Warning | one-line invoice that mirrors the total | Send the invoice's real lines | inferred | Always |
| GEN-DQ-006 | Warning | generic or malformed commodity code | Use the most specific 8-digit UNSPSC code | inferred | Always |
| GEN-DQ-007 | Warning | same description or product code on 80% or more of a batch | Pull line data from the invoice, not a template | inferred | Always |
| GEN-DQ-008 | Information | duplicate lines in one transaction | Merge them or confirm they are separate | inferred | Always |

### VISA (26)

| Rule | Sev | Check | Fix | Conf | Effective |
| --- | --- | --- | --- | --- | --- |
| VISA-ELIG-001 | Information | card product not business, corporate, purchasing or fleet | None; no Product 3 rate | processor-doc | From 2025-10-17 |
| VISA-ELIG-002 | Information | MCC on Product 3 exclusion list | None; no Product 3 rate | processor-doc | From 2025-10-17 |
| VISA-ELIG-003 | Information | commercial prepaid card | None; listed as ineligible by one processor | processor-doc | Always |
| VISA-ELIG-004 | Information | federal government card at excluded medical MCC | None | processor-doc | From 2025-10-17 |
| VISA-ELIG-005 | Warning | card not present and no billing postal code | Send the billing postal code for address verification (Adyen) | processor-doc | Always |
| VISA-L2-001 | Error | tax ratio outside 0.1% to 22%, MCC not exempt, not marked exempt | Correct the tax amount or mark the sale exempt | processor-doc | Until 2026-04-17 |
| VISA-L2-002 | Error | tax indicator missing | Send `taxable` or `exempt` | processor-doc | Until 2026-04-17 |
| VISA-L2-003 | Error | purchasing card at fuel MCC with no customer code | Send the customer code | processor-doc | Until 2026-04-17 |
| VISA-L3-001 | Error | no line items | Send the invoice lines | processor-doc | From 2025-10-17 |
| VISA-L3-002 | Error | line item descriptor missing | Add the item name | processor-doc | From 2025-10-17 |
| VISA-L3-003 | Error | product code missing | Add the SKU or part number | processor-doc | From 2025-10-17 |
| VISA-L3-004 | Error | commodity code missing | Add the UNSPSC code | processor-doc | From 2025-10-17 |
| VISA-L3-005 | Error | quantity missing or not above 0 | Send the real quantity | processor-doc | From 2025-10-17 |
| VISA-L3-006 | Error | unit of measure missing | Send a unit such as EA or BX | processor-doc | From 2025-10-17 |
| VISA-L3-007 | Error | unit cost missing | Send the unit price | processor-doc | From 2025-10-17 |
| VISA-L3-008 | Error | line total missing | Send the extended amount | processor-doc | From 2025-10-17 |
| VISA-L3-009 | Error | line discount missing | Send 0 when there is no discount | processor-doc | From 2025-10-17 |
| VISA-L3-010 | Warning | line-item detail indicator missing | Confirm your processor sets it | processor-doc | From 2025-10-17 |
| VISA-L3-011 | Error | summary discount, freight or duty missing | Send 0 for each that does not apply | processor-doc | From 2025-10-17 |
| VISA-L3-012 | Error | tax neither above 0 nor marked exempt | Send the tax, or set `tax.indicator` to `exempt` | processor-doc | From 2025-10-17 |
| VISA-L3-013 | Warning | freight above 0 but ship-from, ship-to or destination country missing | Send the shipping postal codes and country | processor-doc | From 2025-10-17 |
| VISA-L3-014 | Warning | descriptor longer than 26 characters | Shorten it; statements show 26 | processor-doc | Always |
| VISA-L3-015 | Error | commodity code over 12 chars or not alphanumeric without spaces | Send the bare code, for example 42132203 | processor-doc | Always |
| VISA-DQ-001 | Error | placeholder description or merchant name as description | Use the real item name | processor-doc | From 2025-10-17 |
| VISA-DQ-002 | Error | zero or filler in a required code field | Send real values | processor-doc | From 2025-10-17 |
| VISA-LT-001 | Information | amount high enough that Large Ticket rates may be cheaper | Ask your processor about Large Ticket | processor-doc | Always |

### MC (17)

| Rule | Sev | Check | Fix | Conf | Effective |
| --- | --- | --- | --- | --- | --- |
| MC-ELIG-001 | Information | business card: Level 2 only | None; Data Rate III needs corporate or purchasing | processor-doc | Always |
| MC-ELIG-002 | Information | card product code MBS or MEF | None; not Level 3 eligible | processor-doc | Always |
| MC-ELIG-003 | Information | MCC on Level 2 or Level 3 exclusion list | None | processor-doc | Always |
| MC-L2-001 | Error | tax ratio outside 0.1% to 30%, MCC not exempt, not marked exempt | Correct the tax amount or mark the sale exempt | processor-doc | Always |
| MC-L2-002 | Error | customer code and PO both missing or blank | Send the buyer's reference | processor-doc | Always |
| MC-L2-003 | Error | tax indicator missing | Send `taxable` or `exempt` | processor-doc | Always |
| MC-L2-004 | Warning | merchant tax ID missing | Send your tax ID | processor-doc | Always |
| MC-L3-001 | Error | no line items | Send the invoice lines | processor-doc | Always |
| MC-L3-002 | Error | description missing | Add the item name | processor-doc | Always |
| MC-L3-003 | Error | product code missing | Add the SKU | processor-doc | Always |
| MC-L3-004 | Error | quantity missing or not above 0 | Send the quantity | processor-doc | Always |
| MC-L3-005 | Error | unit of measure not on the Mastercard list | Map to a listed code such as EAC or PCE | processor-doc | From 2025-10-01 |
| MC-L3-006 | Error | unit price or extended amount missing | Send both | processor-doc | Always |
| MC-L3-007 | Warning | debit or credit indicator missing | Send `debit` or `credit` | processor-doc | Always |
| MC-L3-008 | Warning | line tax indicator, discount indicator or tax type missing | Send the indicators | processor-doc | Always |
| MC-L3-009 | Warning | ship-to postal code missing | Send it when goods ship | processor-doc | Always |
| MC-FMT-001 | Warning | amount longer than 9 digits | Check the value; it may be truncated | processor-doc | Always |

### AMEX (9)

| Rule | Sev | Check | Fix | Conf | Effective |
| --- | --- | --- | --- | --- | --- |
| AMEX-ELIG-001 | Information | any Amex transaction | None; no public Level 3 rate, Level 2 pricing needs an Amex agreement | processor-doc | Always |
| AMEX-FMT-001 | Error | more than 4 advice lines, or a line over 40 characters | Condense the text | processor-doc | Always |
| AMEX-FMT-002 | Warning | more than 4 line items under a profile that forwards only 4 | Put the most important lines first | processor-doc | Always |
| AMEX-L2-001 | Error | ship-to postal code missing | Send it | processor-doc | Always |
| AMEX-L2-002 | Warning | PO number over 22 characters | Shorten it | processor-doc | Always |
| AMEX-L2-003 | Warning | customer reference over 20 characters | Shorten it; some files keep only 17 | processor-doc | Always |
| AMEX-L2-004 | Error | tax exempt without `tax.exemptionId` | Send the tax registration ID | processor-doc | Always |
| AMEX-L3-001 | Error | unit of measure not a 2-character ANSI X12 code | Map to a code such as EA | processor-doc | Always |
| AMEX-L3-002 | Error | Level 3 prerequisites missing (requestor name, customer reference, tax amount, ship-to postal code) | Send the missing fields | processor-doc | Always |

### DISC (3)

| Rule | Sev | Check | Fix | Conf | Effective |
| --- | --- | --- | --- | --- | --- |
| DISC-ELIG-001 | Information | any Discover transaction | None; no Level 2 or 3 rate exists | processor-doc | Always |
| DISC-FMT-001 | Error | more than 98 line items | Consolidate lines | processor-doc | Always |
| DISC-L2-001 | Warning | taxable flag missing | Send `tax.indicator` | processor-doc | Always |

Single-processor rules run only when that processor's profile is selected or with `--strict-profiles`, otherwise as warnings [CBR end of catalog; clarification 6, ADR-0016].

Rule sources cited in CBR prose per area (each rule cites at least one, see `docs/dev/RULE-DESIGN.md` for the per-rule list).

## 13. Provenance [CBR "Provenance"]

- Confidence labels: `public-brand` (brand publications; none in v1.0), `processor-doc`, `inferred`, `policy`.
- Source policy: link most specific public page (brand, then processor, then guide); on disagreement record all, encode stricter reading, note in `notes`; never non-public or employer material; recheck each April and October and bump pack version.
- Facts to verify (8): Visa tolerance; L2 sunset date (pack uses April 17 as last day); Visa detail indicator; MC UOM second source; MC description max length; Amex requestor name max length; Discover program; Large Ticket break-even (compute from user's rate table).

## 14. Architecture [AR]

- Solution: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `schemas/` (canonical, results, rulepack, config; JSON Schema 2020-12), 11 src projects (Core, Rules.General, Rules.Visa, Rules.Mastercard, Rules.Amex, Rules.Discover, Adapters, Reporting, Hosting, Cli, Mcp), `action/action.yml`, `fixtures/`, 9 test projects (Core.Tests, Rules.Tests, Adapters.Tests, Reporting.Tests, Cli.Tests, Mcp.Tests, Architecture.Tests, Benchmarks, Fuzz), `docs/`.
- Dependency direction: Rules, Adapters, Reporting depend on Core; Hosting depends on those three; Cli and Mcp depend on Hosting; nothing depends on Cli or Mcp; enforced by architecture test (NFR-013).
- Packages: ModelContextProtocol 1.x and .AspNetCore (Mcp); System.CommandLine 2.x (Cli); Spectre.Console (Cli); JsonSchema.Net (Core); YamlDotNet (Core, Hosting); CsvHelper (Adapters); Microsoft.Extensions.Hosting, Logging, Options; xUnit v3, Shouldly, Verify, FsCheck; BenchmarkDotNet, NetArchTest.Rules, SharpFuzz. SARIF, JUnit and Jaro-Winkler hand-written.
- Build: nullable, warnings as errors, STJ source generation, `[GeneratedRegex]`, ReadyToRun single-file for six RIDs plus framework-dependent tool.
- Model: immutable records mirroring canonical JSON; absent is not zero; `Currency`, `Money` (`Rounded`, `HasExcessPrecision`); enums CardBrand, CardProduct, TaxIndicator, DebitCredit, Channel, CommodityScheme; `FieldPath(Canonical, Source)`; `Severity` (Information, Warning, Error); `Confidence` (Policy, PublicBrand, ProcessorDoc, Inferred); `Finding(RuleId, Severity, TransactionIndex, Path, Message, Expected, Actual, Fix, Patch, Confidence, Sources, Impact)`; `LintResult(Findings, Summary, EngineVersion, RulePackVersions, InputSha256)`. JSON is authoritative for shape (clarification 1).
- Pipeline (10 stages): 1 Read (size limit: exit 2); 2 Detect format (no adapter at least 0.8, or two within 0.2: exit 2); 3 Scrub (`--strict-pci`: exit 1); 4 Map (unmappable: GEN-FMT-001); 5 Validate schema (findings only); 6 Normalize (derived facts); 7 Select rules (invalid pack: exit 3); 8 Evaluate (throwing rule becomes internal error finding); 9 Enrich (messages, fixes, patches, impact, sources); 10 Report (write failure: exit 3). Sort by transaction index, canonical path, rule ID. Exit 1 when any finding meets `--fail-on`.
- Rule selection: brand, card product, level, processor profile, effective date (`--as-of`, else `transactionDate`, else today), config.
- Batch rules (GEN-DQ-007) run after the whole input over aggregated counts in a fixed-size dictionary.
- Concurrency: reader task for stages 1 to 3; `Environment.ProcessorCount` workers for 4 to 9; bounded channels capacity 256; reporter restores order.
- Check kinds: `required` (field, allowZero), `maxLength` (field, max, profileKey), `pattern` (field, regex, 100 ms timeout), `range` (field, min, max, exclusive), `inList` (field, list CSV, caseInsensitive), `ratio` (numerator, denominator, min, max), `maxCount` (collection, max, profileKey), `anyOf` (fields), `custom` (check name).
- Pack YAML: `pack` (id, version, brand, publishedOn, lists) and `rules[]` (id, title, level, severity, confidence, effective, appliesTo {cardProducts, mccNotIn}, check, message with `{index}` and `{actual}`, fix, impact, sources, examples {pass, fail}).
- Interfaces: `IRule` (Metadata, AppliesTo, Evaluate), `ICustomCheck` (Name, Evaluate), `IBatchRule` (Metadata, Observe, Complete). `RuleContext` carries transaction, derived facts, options, profile, as-of date. Rules are stateless singletons; batch rules fresh per run.
- Selectors: JSON Pointer with one `*` wildcard; each failing index reported separately.
- Pack versioning: semver; effective dates handle timelines. Distribution: embedded plus NuGet content packages; validate schema and SHA-256 manifest at startup (exit 3); `--rule-pack <path>`; `.l3lint.lock` pins versions.
- Adapters: `IPayloadAdapter` (Name, Kind, Detect, Map), `AdapterResult(Transaction, PathMap, DerivedFields, Notes)`. Minor units for Stripe, Adyen, Checkout.com; decimal strings for Braintree, Cybersource, Authorize.net. Record derived values (Stripe line total). Fill sequence from order. Note ambiguity with `AdapterNote`.
- Adapter mapping table (6 gateways by 26 rows, asterisks to confirm): see AR "Field mapping" and `docs/dev/verification.md`.
- CSV adapter: YAML map with column names, `const`, `from`, `map`, `default`; `groupBy`; transaction-level columns must agree or GEN-FMT-001.
- Impact classes: `blocksLevel3`, `blocksLevel2`, `riskVisaValidation`, `truncation`, `noBenefit`, `hygiene`.
- Tier rollup: from brand top tier, step down past tiers blocked by error findings; `riskVisaValidation` marks Product 3 "at risk".
- Cost estimates opt in with `--rates`; rate table (name, effective, currency, programFees, categories); printed as "Estimated, using <name> (effective <date>)"; VISA-LT-001 computes break-even from Large Ticket categories.
- Security: threats and controls table; card-number algorithm (6 steps); `pci.allowFields`; privacy commitments.
- Errors: finding at or above fail-on (1); usage error (2); pack or config error (3); internal error (3). Logging to stderr via M.E.Logging; levels error, warn, info, debug, trace; `--log-format json`; event IDs 1000 to 1999 input, 2000 to 2999 rules, 3000 to 3999 output.
- Performance plan: Utf8JsonReader, derived facts once, GeneratedRegex, FrozenSet ordinal-ignore-case, no allocation on passing path, benchmark regression gate 15%.
- Extension points: custom rules (`AddL3Lint(o => o.AddRule<T>())`), adapters, `IReportWriter`, config, profiles `profiles/*.yaml`, `--rule-pack`. CLI loads no plugin assemblies.

## 15. Interfaces [IF]

### CLI commands (8)

`lint <inputs...>`, `explain <ruleId>`, `fix <input>` (`--dry-run` default, `--write <path>`), `convert <input> --to canonical`, `rules list | show <id> | test`, `schema <name>`, `init`, `version`.

### `lint` options

| Option | Values | Default |
| --- | --- | --- |
| `--format` | auto, canonical, stripe, adyen, braintree, cybersource, authorizenet, checkoutcom, csv | auto |
| `--map` | path (required with csv) | none |
| `--brand` | auto, visa, mastercard, amex, discover, all | auto |
| `--card-product` | consumer, business, corporate, purchasing, fleet, commercialPrepaid | payload (override prints a note) |
| `--mcc` | 4 digits | payload |
| `--merchant-country` | alpha-2 | US |
| `--processor` | profile name | generic |
| `--as-of` | YYYY-MM-DD | transactionDate, else today |
| `--config` | path | nearest `.l3lint.yaml` walking up |
| `--rule-pack` | path, repeatable | embedded |
| `--only`, `--disable` | IDs or globs | none |
| `--severity` | RULE=level, repeatable | pack defaults |
| `--fail-on` | error, warning, information, none | error |
| `--output` | text, json, sarif, junit, markdown; `kind=path`; repeatable | text |
| `--max-findings` | integer | unlimited |
| `--rates` | path | none |
| `--strict-pci`, `--strict-profiles` | flags | off |
| `--no-color`, `--quiet`, `--verbose` | flags | |
| `--log-level`, `--log-format` | warn, text defaults | |
| `--annotations` | github, none | none |
| `--log-values` | flag (FR-033) | off |
| `--fix-amount` | flag on `fix` (clarification 12) | off |

Precedence: flags over payload over config over defaults; every override prints a note. Exit codes 0, 1, 2, 3. Example session format [IF "Example session"].

### Config keys (`.l3lint.yaml`)

`version`; `context.*` (brand, cardProduct, mcc, merchantCountry, processor); `inputs.format`; `inputs.csvMap`; `inputs.limits.maxFileBytes|maxLineItems|maxJsonDepth`; `rules.disable`; `rules.severity`; `rules.placeholders.add|remove`; `rules.batch.minTransactions|sameValueShare`; `arithmetic.lineTolerance|totalTolerance|toleranceMode|rounding`; `pci.strict`; `pci.allowFields`; `output.failOn`; `output.formats`; `output.maxFindings`; `rates`. Relative paths resolve from config folder. Unknown keys are errors (exit 3).

### Outputs

- JSON: `tool {name, version}`, `rulePacks {id: version}`, `asOf`, `inputs[] {path, format, sha256, transactions}`, `transactions[] {index, input, id, brand, cardProduct, likelyTier {best, likely, status}, findings[] {ruleId, severity, impact, path {canonical, source}, location {line, column}, message, expected, actual, fix, patch, confidence, sources}}`, `summary {transactions, errors, warnings, information, byRule}`, `exitCode`.
- SARIF mapping: driver name/version/informationUri; rules[] for fired rules (id, name, shortDescription, fullDescription, helpUri, defaultConfiguration.level, properties.tags brand, level, confidence); results ruleId, level (error, warning, note); message text plus "Fix:"; physicalLocation (repo-relative URI, startLine, startColumn); logicalLocations fullyQualifiedName = source pointer; partialFingerprints "l3lint/v1" = SHA-256 of rule ID, source pointer, transaction ID; properties transactionIndex, expected, actual, confidence, sources, impact; automationDetails.id `l3lint/` plus config hash. Max 5,000 results, 10 MB gzipped; overflow keeps errors then warnings and adds one note with the drop count.
- JUnit: one testsuite per input file; one testcase per transaction named by ID; error findings become `failure`; warnings and information in `system-out`.
- Markdown: summary table (transactions, errors, warnings by brand) then a collapsible section per failing transaction.
- Text: as in the CLI example; severity words always print.

### MCP server

Tools (all readOnly, idempotent; lint_payload also closed world):

| Tool | Input | Output |
| --- | --- | --- |
| `lint_payload` | payload (object or string, max 2 MB), format?, context? {brand, cardProduct, mcc, merchantCountry, processor, asOf}, failOn?, maxFindings? | JSON result for one transaction: findings, summary, likely tier |
| `explain_rule` | ruleId | title, requirement, rationale, severity, confidence, effective dates, sources, pass and fail examples |
| `list_rules` | brand?, level?, query? | IDs, titles, severities |
| `map_payload` | payload, format? | canonical JSON, path map, derived fields, adapter notes |
| `suggest_fixes` | payload, format?, context? | RFC 6902 patch in source format, findings needing a human |
| `get_schema` | name: canonical, results, config, rulepack | the schema |

Every tool also returns a short text summary. Invalid JSON, unknown format, unknown rule ID: `isError: true` with a corrective message. Resources: `l3lint://rules/{ruleId}` (Markdown page), `l3lint://schemas/{name}`, `l3lint://examples/{brand}/{name}`. Prompts: `review_payload`, `explain_downgrade`. Transports: stdio (default, logs to stderr) and Streamable HTTP `--http --port 7341` (127.0.0.1, Origin check, optional `--token` bearer; stateless). Client config with `dnx L3Lint.Mcp --yes`. Package type McpServer, `.mcp/server.json`, README `mcp-name` comment.

### GitHub Action

Inputs: `paths` (required), `format` (auto), `brand`, `card-product`, `mcc`, `processor`, `as-of`, `config` (.l3lint.yaml), `fail-on` (error), `sarif-file` (l3lint.sarif), `upload-sarif` (true), `annotations` (true), `version` (Action VERSION file), `dotnet-version` (10.0.x), plus `tool-source` (clarification 17). Outputs: `errors`, `warnings`, `exit-code`, `sarif-file`. Composite: setup-dotnet, install tool, `scripts/run.sh` (text, sarif, markdown to `$GITHUB_STEP_SUMMARY`, `--annotations github`, counts to `$GITHUB_OUTPUT`, always exit 0), upload-sarif v4 (category l3lint), final step re-applies exit code. Third-party actions pinned by SHA. Example workflow with `permissions: contents read, security-events write`.

### Library

Packages: `L3Lint` (meta: Core, Hosting, Adapters, Reporting, packs), `L3Lint.Core`, `L3Lint.Rules.General|Visa|Mastercard|Amex|Discover`, `L3Lint.Adapters`, `L3Lint.Reporting`. `ILinter` (`Lint(Transaction, LintOptions?)`, `Lint(RawDocument, LintOptions?)`, `LintManyAsync`, `Explain`, `Rules`). `LintOptions` (Brand, CardProduct, Mcc, Processor, AsOf, FailOn, Arithmetic, DisabledRules). `AddL3Lint(options => ...)`. Guarantees: thread-safe singleton, no I/O after startup, under 1 ms warm for 10 lines, exceptions only for programming errors.

## 16. Test and delivery [TD]

- Test layers: unit (90% coverage), rule examples (all 78), property-based (FsCheck 1,000 cases), golden corpus (recall 95%, FP 2%), determinism (2 runs, 1 and 8 threads), adapter contracts, output conformance (SARIF schema, results schema, JUnit XSD), CLI end to end, MCP (in-memory client, isError, Inspector), Action (green and red), security (fuzz, masking, no network), performance (BenchmarkDotNet, 15% regression). Mutation testing (Stryker, 80%, weekly). Agent evaluation (nightly, 90%).
- Fixture corpus (about 830 files): layout `fixtures/canonical/<brand>/{valid,invalid}`, `fixtures/adapters/<gateway>`, `fixtures/adapters/csv/*.csv` and `*.map.yaml`, `fixtures/seeded`, `fixtures/known-good`, `fixtures/pci`; sibling `*.expect.yaml` with description, context, expect {exitCode, findings, absent, likelyTier}.
- Size targets: 156 rule examples, 70 adapter fixtures (10 per adapter), 400 seeded (40 bases by 10 operators, fixed seed), 200 known-good, 10 PCI.
- Mutation operators (10): drop a required field; zero a required field; placeholder description; unlisted unit; shift a line total one cent beyond tolerance; discount at both levels; tax ratio out of range; test card number in free text; overflow a field length; exceed a line-item limit.
- Fixture rules: synthetic only; public test card numbers only; explicit context.
- CI (`ci.yml`): locked restore, build on three OS; all test layers; coverage and corpus gates; short benchmark; self-lint with the Action; docs build failing on missing rule pages.
- Release (`release.yml`, tag vX.Y.Z): pack tool, libraries, five pack packages, `L3Lint.Mcp` (McpServer); six single-file R2R binaries with checksums; sign, SBOM, attestations; push NuGet; `mcp-publisher publish`; update Action VERSION and tags; publish docs and changelog.
- Channels: NuGet tool `l3lint`; GitHub Releases binaries; NuGet libraries; NuGet MCP plus registry; Marketplace Action.
- Versioning: tool, library, MCP share one version; packs independent.
- Backlog T-01 to T-50 [TD "Backlog"]: T-01 name check (owner), T-02 clearance (owner), T-03 scaffold, T-04 CI, T-05 schemas, T-06 model, T-07 reader, T-08 scrub, T-09 schema stage, T-10 normalize, T-11 engine, T-12 check kinds, T-13 arithmetic, T-14 detectors, T-15 GEN, T-16 VISA, T-17 MC, T-18 Stripe/Adyen/Braintree, T-19 text/JSON, T-20 CLI basics, T-21 library, T-22 corpus, T-23 MVP prerelease, T-24 Cybersource/Authorize.net/Checkout.com, T-25 CSV, T-26 detection, T-27 profiles, T-28 SARIF, T-29 JUnit/Markdown, T-30 fix, T-31 cost, T-32 convert/schema, T-33 MCP stdio, T-34 MCP HTTP, T-35 MCP packaging, T-36 Action, T-37 init, T-38 docs, T-39 AMEX, T-40 DISC, T-41 GEN-DQ-007, T-42 rules test, T-43 fuzz, T-44 benchmarks, T-45 security review, T-46 facts to verify, T-47 agent eval, T-48 release pipeline, T-49 v1.0 release (owner), T-50 launch (owner).
- Definition of done and v1.0 launch checklist [TD "Definition of done"]; user-facing messages under 25 words per sentence and state the fix as an action.
