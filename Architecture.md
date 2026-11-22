# 3. Architecture

## At a glance

The CLI, MCP server, GitHub Action and .NET library are thin hosts over one offline engine, so a payload gets the same findings wherever it is linted.

&#91;embedded content: l3lint architecture · 4 surfaces, 10 stages\]

A payload enters at the top, is masked for card numbers before anything else reads it, is mapped by an adapter into the canonical model, and is checked against the selected rule packs; the report stage writes every output format and sets the exit code.

## Solution structure

One repository, one .NET 10 solution, eleven production projects. Core knows nothing about any host, so the CLI, MCP server and library are thin shells over the same engine.

```text
l3lint/
  global.json                    pins the .NET 10 SDK feature band
  Directory.Build.props          nullable, warnings as errors, deterministic, InvariantGlobalization
  Directory.Packages.props       central package versions
  schemas/
    canonical.schema.json        canonical payload, JSON Schema 2020-12
    results.schema.json          JSON output contract
    rulepack.schema.json         rule pack contract
    config.schema.json           .l3lint.yaml contract
  src/
    L3Lint.Core/                 model, money, engine, detectors, arithmetic, rule abstractions
    L3Lint.Rules.General/        GEN pack: YAML + custom checks
    L3Lint.Rules.Visa/           VISA pack
    L3Lint.Rules.Mastercard/     MC pack
    L3Lint.Rules.Amex/           AMEX pack
    L3Lint.Rules.Discover/       DISC pack
    L3Lint.Adapters/             Stripe, Adyen, Braintree, Cybersource, AuthorizeNet, CheckoutCom, Csv
    L3Lint.Reporting/            text, JSON, SARIF, JUnit, Markdown writers
    L3Lint.Hosting/              AddL3Lint() DI registration, options binding
    L3Lint.Cli/                  System.CommandLine host, packed as a .NET tool
    L3Lint.Mcp/                  MCP server, stdio and Streamable HTTP
  action/
    action.yml                   composite GitHub Action
  fixtures/                      golden corpus (Test and delivery tab)
  tests/
    L3Lint.Core.Tests/  L3Lint.Rules.Tests/  L3Lint.Adapters.Tests/
    L3Lint.Reporting.Tests/  L3Lint.Cli.Tests/  L3Lint.Mcp.Tests/
    L3Lint.Architecture.Tests/  L3Lint.Benchmarks/  L3Lint.Fuzz/
  docs/                          generated rule reference and user guide
```

**Dependency direction.** Rules, Adapters and Reporting depend on Core. Hosting depends on those three. Cli and Mcp depend on Hosting. Nothing depends on Cli or Mcp. An architecture test enforces this (NFR-013).

**Third-party packages**

| Package | Used by | Purpose |
| --- | --- | --- |
| ModelContextProtocol 1.x, ModelContextProtocol.AspNetCore | Mcp | Official MCP C# SDK; stdio and HTTP transports ([Microsoft Learn](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)) |
| System.CommandLine 2.x | Cli | Command and option parsing |
| Spectre.Console | Cli | Tables and color in text output |
| JsonSchema.Net | Core | Validating canonical payloads, rule packs and config |
| YamlDotNet | Core, Hosting | Rule packs and `.l3lint.yaml` |
| CsvHelper | Adapters | CSV input |
| Microsoft.Extensions.Hosting, Logging, Options | Hosting, Cli, Mcp | DI, logging, configuration |
| xUnit v3, Shouldly, Verify, FsCheck | tests | Unit, snapshot and property-based tests |
| BenchmarkDotNet, NetArchTest.Rules, SharpFuzz | tests | Performance, architecture rules, fuzzing |

SARIF, JUnit and the Jaro-Winkler similarity are hand-written: each is small, and owning them avoids heavy dependencies. Check each package's current major version and license when the solution is created.

**Build conventions**

- `Nullable` enabled and `TreatWarningsAsErrors` true in every project.
- `System.Text.Json` with source-generated contexts; no reflection-based serialization in hot paths.
- `[GeneratedRegex]` for every pattern.
- CLI published as ReadyToRun single-file binaries for six runtime identifiers (win, osx, linux; x64 and arm64) and as a framework-dependent .NET tool.

## Canonical model

The model is a set of immutable C# records that mirror the canonical JSON one to one. The central design rule: **absent is not zero.** Every optional value is nullable, because "freight not sent" and "freight sent as 0" lead to different findings under Visa.

```csharp
namespace L3Lint.Core.Model;

public readonly record struct Currency(string Code, int Exponent);

public readonly record struct Money(decimal Value, Currency Currency)
{
    public decimal Rounded(MidpointRounding mode) =>
        Math.Round(Value, Currency.Exponent, mode);
    public bool HasExcessPrecision =>
        decimal.Round(Value, Currency.Exponent) != Value;
}

public enum CardBrand { Unknown, Visa, Mastercard, Amex, Discover }
public enum CardProduct { Unknown, Consumer, Business, Corporate, Purchasing, Fleet, CommercialPrepaid }
public enum TaxIndicator { NotProvided, Taxable, Exempt }
public enum DebitCredit { Debit, Credit }
public enum Channel { Unknown, CardPresent, CardNotPresent }
public enum CommodityScheme { Unknown, Unspsc, Nigp, Hs, Naics, Other }

public sealed record Transaction
{
    public required string SchemaVersion { get; init; }
    public string? Id { get; init; }
    public required Money Amount { get; init; }
    public DateOnly? TransactionDate { get; init; }
    public Channel Channel { get; init; }
    public CardInfo Card { get; init; } = new();
    public MerchantInfo Merchant { get; init; } = new();
    public string? CustomerReference { get; init; }
    public OrderInfo Order { get; init; } = new();
    public TaxInfo Tax { get; init; } = new();
    public SummaryAmounts Amounts { get; init; } = new();
    public ShippingInfo Shipping { get; init; } = new();
    public string? BillingPostalCode { get; init; }
    public AmexInfo? Amex { get; init; }
    public IReadOnlyList<LineItem> LineItems { get; init; } = [];
}

public sealed record LineItem
{
    public int? Sequence { get; init; }
    public string? Description { get; init; }
    public string? ProductCode { get; init; }
    public string? CommodityCode { get; init; }
    public CommodityScheme CommodityScheme { get; init; }
    public decimal? Quantity { get; init; }
    public string? UnitOfMeasure { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal? DiscountAmount { get; init; }
    public bool? DiscountIndicator { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? TaxRate { get; init; }
    public bool? Taxable { get; init; }
    public string? TaxTypeCode { get; init; }
    public bool AmountIncludesTax { get; init; }
    public decimal? Total { get; init; }
    public DebitCredit DebitCredit { get; init; } = DebitCredit.Debit;
    public string? DetailIndicator { get; init; }
}
```

`CardInfo`, `MerchantInfo`, `OrderInfo`, `TaxInfo`, `SummaryAmounts`, `ShippingInfo` and `AmexInfo` follow the same pattern, one property per field in the dictionary. Amounts inside a transaction share its currency, so they are `decimal?` rather than `Money`.

**Locations.** Every value can be traced back to where it came from.

```csharp
public readonly record struct FieldPath(string Canonical, string? Source);
// Canonical is an RFC 6901 JSON Pointer such as /lineItems/0/total.
// Source is the same location in the original format, such as
// /amount_details/line_items/0/unit_cost for Stripe, or "row 14, column F" for CSV.
```

**Results.**

```csharp
public enum Severity { Information, Warning, Error }
public enum Confidence { Policy, PublicBrand, ProcessorDoc, Inferred }

public sealed record Finding(
    string RuleId,
    Severity Severity,
    int TransactionIndex,
    FieldPath Path,
    string Message,
    string? Expected,
    string? Actual,
    string Fix,
    FixPatch? Patch,
    Confidence Confidence,
    IReadOnlyList<Uri> Sources,
    Impact Impact);

public sealed record LintResult(
    IReadOnlyList<Finding> Findings,
    LintSummary Summary,
    string EngineVersion,
    IReadOnlyDictionary<string, string> RulePackVersions,
    string InputSha256);
```

**Money rules.** All arithmetic uses `decimal`, never `double`. Rounding happens only at comparisons, with the mode from config (`MidpointRounding.AwayFromZero` by default, `ToEven` optional). The ISO 4217 table ships as embedded data so exponents never depend on the machine's culture settings.

## Processing pipeline

A payload passes through ten stages in a fixed order. The card-number scrub runs before mapping, so no later stage, log line or output ever sees a raw card number.

| # | Stage | Input | Output | On failure |
| --- | --- | --- | --- | --- |
| 1 | Read | File, stdin, glob, NDJSON stream | Raw documents with byte offsets | Size limit exceeded: exit 2 |
| 2 | Detect format | Raw JSON or CSV | Adapter choice and score | No adapter at least 0.8, or two within 0.2: exit 2 with candidates |
| 3 | Scrub card numbers | Raw JSON tokens | Masked document plus `GEN-PCI-001` findings | `--strict-pci`: stop with exit 1 |
| 4 | Map | Masked document | `Transaction`, path map, adapter notes | Unmappable required structure: `GEN-FMT-001` |
| 5 | Validate schema | Canonical JSON | Schema findings | Findings only; pipeline continues where possible |
| 6 | Normalize | `Transaction` | Normalized transaction plus derived facts (expected totals, tax ratio, normalized text) | None |
| 7 | Select rules | Context, config, rule packs | Ordered rule list | Invalid pack: exit 3 |
| 8 | Evaluate | Transaction and rules | Findings | A throwing rule becomes an internal error finding; others still run |
| 9 | Enrich | Findings | Messages, fixes, patches, impact, sources | None |
| 10 | Report | All results | Text, JSON, SARIF, JUnit or Markdown; exit code | Write failure: exit 3 |

**Stage details**

1. **Read.** Streams input; never loads more than one transaction into memory in NDJSON mode. Detects JSON, NDJSON (one object per line) and CSV by the first non-whitespace bytes and the file extension.
2. **Detect format.** Each adapter scores the document from 0 to 1 using cheap shape probes, for example `amount_details.line_items` for Stripe or `additionalData.enhancedSchemeData.*` keys for Adyen. `--format` skips detection.
3. **Scrub.** Walks every string and number token (algorithm in the Security section). Matches are replaced with `************1234` in place, and a finding records the path.
4. **Map.** The adapter builds a `Transaction` and a path map from canonical pointers to source pointers. Unknown fields are ignored, but listed as notes when `--verbose` is set.
5. **Validate schema.** JsonSchema.Net validates the canonical form. Type errors stop rules that need that field; everything else continues so users see all problems in one run.
6. **Normalize.** Computes derived facts once: expected line totals, the expected transaction amount, the tax ratio, normalized descriptions for the detectors. Rules read these instead of recomputing.
7. **Select rules.** Filters by brand, card product, level, processor profile, effective date (`--as-of`, else `transactionDate`, else today) and config. With `card.brand` unknown, the GEN pack runs and each brand pack runs in "report as" mode, labelling its findings by brand.
8. **Evaluate.** Per-transaction rules run first. Batch rules (`GEN-DQ-007`) run after the whole input is read, over aggregated counts kept in a fixed-size dictionary.
9. **Enrich.** Fills message templates, attaches the fix text, builds JSON Patch operations for mechanical fixes, and assigns an impact class.
10. **Report.** Sorts findings by transaction index, canonical path, then rule ID (NFR-001) and writes the requested formats. Exit code: 1 when any finding meets `--fail-on` (default `error`), else 0.

**Concurrency.** Batch mode runs stages 1 to 3 on a reader task and stages 4 to 9 on `Environment.ProcessorCount` workers connected by bounded `System.Threading.Channels` (capacity 256). Each item carries its input index, and the reporter restores order before writing, so output stays deterministic at any thread count.

## Rule engine and rule pack format

A rule is metadata in YAML plus a check. Most checks are one of eight declarative kinds; the rest name a compiled C# class. Rule packs never contain executable code, so loading a pack cannot run anything (NFR-006).

**Declarative check kinds**

| Kind | Parameters | Example rule |
| --- | --- | --- |
| `required` | `field`, `allowZero` | VISA-L3-004 |
| `maxLength` | `field`, `max`, `profileKey` | VISA-L3-014 |
| `pattern` | `field`, `regex` (compiled at load, 100 ms timeout) | VISA-L3-015 |
| `range` | `field`, `min`, `max`, `exclusive` | GEN-FMT-003 |
| `inList` | `field`, `list` (a CSV in the pack), `caseInsensitive` | MC-L3-005 |
| `ratio` | `numerator`, `denominator`, `min`, `max` | MC-L2-001 |
| `maxCount` | `collection`, `max`, `profileKey` | DISC-FMT-001 |
| `anyOf` | `fields` (at least one present) | MC-L2-002 |
| `custom` | `check` (the name of a registered C# class) | GEN-ARITH-001 |

**Rule pack file** (`visa.rules.yaml`, excerpt)

```yaml
pack:
  id: l3lint.rules.visa
  version: 1.2.0
  brand: visa
  publishedOn: 2026-10-19
  lists:
    product3ExcludedMcc: lists/visa-product3-excluded-mcc.csv
rules:
  - id: VISA-L3-004
    title: Commodity code is required for Product 3
    level: L3
    severity: error
    confidence: processor-doc
    effective: { from: 2025-10-17 }
    appliesTo:
      cardProducts: [business, corporate, purchasing, fleet]
      mccNotIn: product3ExcludedMcc
    check:
      kind: required
      field: /lineItems/*/commodityCode
      allowZero: false
    message: "Line {index} has no commodity code; Visa needs one on every line for Product 3."
    fix: "Add the most specific 8-digit UNSPSC code for this item."
    impact: blocksLevel3
    sources:
      - https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program
    examples:
      pass: examples/VISA-L3-004.pass.json
      fail: examples/VISA-L3-004.fail.json
  - id: VISA-L3-012
    title: Tax must be positive or explicitly exempt
    level: L3
    severity: error
    confidence: processor-doc
    effective: { from: 2025-10-17 }
    check:
      kind: custom
      check: ExplicitTaxCheck
    message: "Tax is {actual} and the sale is not marked exempt."
    fix: "Send the tax amount, or set tax.indicator to exempt."
    impact: riskVisaValidation
    sources:
      - https://www.versapay.com/cedp
```

**Engine interfaces**

```csharp
public interface IRule
{
    RuleMetadata Metadata { get; }
    bool AppliesTo(in RuleContext context);
    void Evaluate(in RuleContext context, IFindingSink sink);
}

public interface ICustomCheck
{
    string Name { get; }              // referenced from YAML
    void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink);
}

public interface IBatchRule
{
    RuleMetadata Metadata { get; }
    void Observe(in RuleContext context);          // called per transaction
    void Complete(IFindingSink sink);              // called once at the end
}
```

`RuleContext` carries the transaction, derived facts, `LintOptions`, the selected processor profile and the as-of date. Rules are stateless singletons; batch rules get a fresh instance per run.

**Field selectors.** Paths use JSON Pointer with one wildcard: `/lineItems/*/total` iterates lines and reports each failing index separately.

**Versioning.** Packs use semantic versioning: major for removed rules or changed meaning, minor for new rules, patch for wording and source fixes. Effective dates, not versions, handle brand timelines, so one pack can lint both pre-sunset and post-sunset payloads.

**Distribution and loading.** Packs are embedded in the tool and also published as NuGet content packages (`L3Lint.Rules.Visa`). At startup the loader validates each pack against `rulepack.schema.json`, checks its SHA-256 against the manifest, and refuses to start on any mismatch (exit 3). `--rule-pack <path>` loads a local pack for authoring, and `.l3lint.lock` pins pack versions so CI results stay reproducible.

## Input adapters

An adapter turns one gateway's request body into the canonical model and remembers where each value came from. Seven adapters ship by v1.0; Stripe, Adyen and Braintree are MVP.

```csharp
public interface IPayloadAdapter
{
    string Name { get; }                        // "stripe", "adyen", ...
    InputKind Kind { get; }                     // Json or Csv
    double Detect(in RawDocument document);     // 0.0 to 1.0
    AdapterResult Map(in RawDocument document, AdapterOptions options);
}

public sealed record AdapterResult(
    Transaction Transaction,
    PathMap Paths,                              // canonical pointer to source pointer
    IReadOnlySet<string> DerivedFields,         // values computed, not sent
    IReadOnlyList<AdapterNote> Notes);
```

**Rules every adapter follows**

- Convert minor units to `decimal` with the currency exponent: Stripe, Adyen and Checkout.com send integers in minor units; Braintree, Cybersource and Authorize.net send decimal strings.
- Record derived values. Stripe has no line-total field, so the adapter computes it and lists `/lineItems/*/total` in `DerivedFields`; `GEN-ARITH-001` skips derived values instead of trivially passing them.
- Fill `sequence` from array order when the format has none.
- Never guess brand-specific meaning. If a gateway field is ambiguous, map it and add an `AdapterNote`.

**Field mapping** (ESD means an `additionalData` key starting with `enhancedSchemeData.`; an asterisk marks names to confirm against current docs while building the adapter)

| Canonical | Stripe PaymentIntent | Adyen /payments | Braintree sale | Cybersource REST | Authorize.net | Checkout.com |
| --- | --- | --- | --- | --- | --- | --- |
| `amount` | `amount` (minor) | `amount.value` (minor) | `amount` | `orderInformation.amountDetails.totalAmount` | `transactionRequest.amount` | `amount` (minor) |
| `currency` | `currency` | `amount.currency` | From config | `orderInformation.amountDetails.currency` | From config | `currency` |
| `tax.amount` | `amount_details.tax.total_tax_amount` | ESD `totalTaxAmount` | `taxAmount` | `orderInformation.amountDetails.taxAmount` | `transactionRequest.tax.amount`\* | `processing.tax_amount` |
| `tax.indicator` | Derived from tax | Derived from tax | `taxExempt`\* | Derived from tax\* | `transactionRequest.taxExempt`\* | Derived from tax |
| `customer.reference` | `payment_details.customer_reference` | ESD `customerReference` | `purchaseOrderNumber`\* | `orderInformation.invoiceDetails.purchaseOrderNumber`\* | `transactionRequest.poNumber`\* | `processing.order_id` |
| `order.invoiceNumber` | `payment_details.order_reference` | `reference` | `orderId`\* | `clientReferenceInformation.code`\* | `transactionRequest.order.invoiceNumber`\* | `reference` |
| `amounts.freight` | `amount_details.shipping.amount` | ESD `freightAmount` | `shippingAmount` | `orderInformation.amountDetails.freightAmount` | `transactionRequest.shipping.amount`\* | `processing.shipping_amount` |
| `amounts.duty` | Not supported | ESD `dutyAmount` | Not supported\* | `orderInformation.amountDetails.dutyAmount` | `transactionRequest.duty.amount`\* | `processing.duty_amount` |
| `amounts.discount` | `amount_details.discount_amount` | Not supported | `discountAmount` | `orderInformation.amountDetails.discountAmount` | Not supported\* | Not supported\* |
| `amounts.shippingTax` | Not supported | Not supported | `shippingTaxAmount` | Not supported\* | Not supported | Not supported\* |
| `shipping.fromPostalCode` | `amount_details.shipping.from_postal_code` | ESD `shipFromPostalCode` | `shipsFromPostalCode` | To confirm\* | Not supported | To confirm\* |
| `shipping.toPostalCode` | `amount_details.shipping.to_postal_code` | ESD `destinationPostalCode` | `shipping.postalCode`\* | `orderInformation.shipTo.postalCode`\* | `transactionRequest.shipTo.zip`\* | `shipping.address.zip`\* |
| `shipping.toCountry` | Not supported | ESD `destinationCountryCode` | `shipping.countryCodeAlpha2`\* | `orderInformation.shipTo.country`\* | `transactionRequest.shipTo.country`\* | `shipping.address.country`\* |
| `order.orderDate` | Not supported | ESD `orderDate` (DDMMYY) | Not supported | Not supported | Not supported | Not supported |
| Line items | `amount_details.line_items[]` | ESD `itemDetailLine1.*`, `itemDetailLine2.*` | `lineItems[]` | `orderInformation.lineItems[]` | `transactionRequest.lineItems.lineItem[]` | `items[]` |
| `description` | `product_name` | `description` | `name`\* | `productName` | `name` and `description` | `name` |
| `productCode` | `product_code` | `productCode` | `productCode` | `productCode` or `productSku`\* | `productCode` | `reference` |
| `commodityCode` | `payment_method_options.card.commodity_code` | `commodityCode` | `commodityCode` | `commodityCode` | `commodityCode` | `commodity_code` |
| `quantity` | `quantity` | `quantity` | `quantity` | `quantity` | `quantity` | `quantity` |
| `unitOfMeasure` | `unit_of_measure` | `unitOfMeasure` | `unitOfMeasure` | `unitOfMeasure` | `unitOfMeasure` | `unit_of_measure` |
| `unitCost` | `unit_cost` (minor) | `unitPrice` (minor) | `unitAmount`\* | `unitPrice` | `unitPrice` | `unit_price` (minor) |
| `discountAmount` | `discount_amount` | `discountAmount` | `discountAmount` | `discountAmount` | `discountAmount` | `discount_amount` |
| `taxAmount` | `tax.total_tax_amount` | Not supported | `taxAmount` | `taxAmount` | `taxAmount` | `tax_amount` |
| `total` | Derived | `totalAmount` | `totalAmount` | `totalAmount` | `totalAmount` | `total_amount` |
| `amountIncludesTax` | false | false | false\* | `amountIncludesTax` | `taxIncludedInTotal` | false\* |
| `debitCreditIndicator` | debit | debit | `kind`\* | debit\* | debit | debit\* |

Sources: [Stripe](https://docs.stripe.com/payments/payment-line-items), [Adyen](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3), [Braintree](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/), [Cybersource](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html), [Authorize.net schema](https://rubydoc.info/gems/authorizenet/AuthorizeNet/API/LineItemType), [Checkout.com](https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data). Adyen's own calculation adds line totals, freight and total tax, which is why its line totals map as tax-exclusive ([Adyen](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3.md)).

**CSV adapter.** A YAML map names the column behind each canonical field, plus constants and value maps. Rows sharing `groupBy` form one transaction; transaction-level columns must agree across those rows or `GEN-FMT-001` fires.

```yaml
format: csv
delimiter: ","
groupBy: invoice_number
transaction:
  id: invoice_number
  amount: invoice_total
  currency: { const: USD }
  card.brand: card_brand
  card.product: { const: purchasing }
  merchant.mcc: { const: "5047" }
  merchant.name: { const: Gulfside Medical Supply }
  customer.reference: po_number
  tax.amount: tax_total
  tax.indicator: { from: taxable_flag, map: { Y: taxable, N: exempt } }
  amounts.freight: { from: freight, default: 0 }
lineItems:
  description: item_description
  productCode: sku
  commodityCode: unspsc
  quantity: qty
  unitOfMeasure: uom
  unitCost: unit_price
  discountAmount: { from: line_discount, default: 0 }
  total: line_total
```

## Downgrade explanations and cost estimates

Every finding carries an impact class, and the summary rolls those classes up into the best data tier the payload can still reach for each brand. That rollup is what a user means by "will this downgrade?"

**Impact classes**

| Impact | Meaning | Example rules |
| --- | --- | --- |
| `blocksLevel3` | The payload cannot reach Level 3, Product 3 or Data Rate III | VISA-L3-004, MC-L3-005 |
| `blocksLevel2` | The payload cannot reach Level 2 or Data Rate II either | MC-L2-001, MC-L2-002 |
| `riskVisaValidation` | Fields are present but likely to fail Visa's quality review | VISA-DQ-001, VISA-L3-012 |
| `truncation` | Data will be cut or altered in transit; no rate effect expected | VISA-L3-014, AMEX-L2-003 |
| `noBenefit` | No enhanced-data rate exists for this card, brand or MCC | GEN-ELIG-001, DISC-ELIG-001 |
| `hygiene` | Input problem or safety issue | GEN-PCI-001, GEN-FMT-003 |

**Tier rollup.** For each brand that applies, the engine starts at the brand's top tier and steps down past any tier blocked by an error-severity finding. `riskVisaValidation` errors mark Product 3 as "at risk" instead of blocked, because Visa decides after settlement.

```text
INV-20417  visa purchasing  MCC 5047
  Likely tier: Product 3 (at risk)
    error   VISA-DQ-001  /lineItems/1/description  "Item" is a placeholder description.
            Fix: use the real item name, for example "Isolation gown blue cs 50".
            Source: https://merchantcostconsulting.com/...  (processor-doc)
  If it fails validation: settles as Commercial card not present.
```

**Cost estimates (opt in).** With `--rates <file>`, the summary adds an estimated cost for the likely tier and for the best tier, and the difference. l3lint ships no rates: published schedules change every April and October, and stale numbers would mislead. The user maintains the file:

```yaml
name: My processor IC+ schedule
effective: 2026-04-18
currency: USD
programFees:
  - brand: visa
    when: enhancedDataSent
    percent: 0.05
categories:
  - brand: visa
    products: [corporate, purchasing]
    tier: product3
    percent: 1.75
    fixed: 0.10
  - brand: visa
    products: [corporate, purchasing]
    tier: cardNotPresent
    percent: 2.70
    fixed: 0.10
```

The estimate prints as "Estimated, using My processor IC+ schedule (effective 2026-04-18)". It never claims to be the actual fee, because interchange also depends on factors l3lint does not see, such as issuer region and authorization timing. When a rate table includes Large Ticket categories, `VISA-LT-001` computes the break-even amount from the table instead of using a fixed number.

## Security, privacy and PCI scope

l3lint handles invoice data, which can include names, addresses and occasionally a pasted card number. The design keeps the tool out of PCI scope and keeps every byte on the user's machine.

**Threats and controls**

| Threat | Control |
| --- | --- |
| A real card number in a payload | Scrub stage masks it before mapping; `GEN-PCI-001`; `--strict-pci` stops the run |
| Payload data leaking through logs | Logs carry rule IDs, paths and counts only; values need `--log-values` and a warning prints when it is set |
| Data leaving the machine | No network code in Core, Rules, Adapters, Reporting or Cli; a CI test runs the CLI with outbound network blocked |
| Hostile input (huge files, deep JSON, regex backtracking) | 25 MB file cap, depth 64, 1,000 lines per transaction, `RegexOptions.NonBacktracking` or 100 ms timeouts, streaming NDJSON |
| Malicious rule pack | Packs are data only, schema-validated and checksum-verified; `custom` checks resolve only to classes compiled into the tool |
| MCP server reached from a web page (DNS rebinding) | HTTP transport binds 127.0.0.1, validates the `Origin` header, and supports an optional bearer token |
| Tampered releases | Signed NuGet packages, CycloneDX SBOM, GitHub artifact attestations, Action steps pinned by commit SHA |
| YAML deserialization attacks | YamlDotNet configured without type tags; config bound to plain option classes |

**Card-number detection algorithm**

1. Visit every string and number token in the raw document, before mapping.
2. Strip spaces and hyphens. Skip values that are not 13 to 19 digits.
3. Run the Luhn check. Skip failures.
4. Check the issuer prefix and length against major brand ranges (Visa 4; Mastercard 51 to 55 and 2221 to 2720; Amex 34 and 37; Discover 6011, 644 to 649 and 65).
5. If the token sits in a free-text or reference field and passes steps 2 to 4, raise `GEN-PCI-001` as an error. If it sits in a code field (`productCode`, `commodityCode`, `sequence`), raise it as a warning, because any random 16-digit code passes Luhn about one time in ten.
6. Replace the value with asterisks plus the last four digits in the in-memory document. Every later stage and every output sees only the masked value.

`pci.allowFields` in config can exempt specific paths after a human confirms the values are not card numbers.

**Privacy commitments**

- No telemetry, crash reporting or update checks in any component.
- The MCP server processes only what the client sends in the tool call and keeps nothing after the call returns.
- The GitHub Action reads only the files matched by its `paths` input, writes results to the runner and the code scanning API, and nowhere else.

## Errors, logging, performance and extension points

Problems in the payload are findings; problems with the run are errors. Keeping the two apart is what lets CI trust the exit code.

**Error taxonomy**

| Kind | Example | Exit code | Where it shows |
| --- | --- | --- | --- |
| Finding at or above `--fail-on` | Missing commodity code | 1 | Results output |
| Usage error | Unknown option, unreadable file, ambiguous format | 2 | stderr, one line plus a hint |
| Rule pack or config error | Checksum mismatch, invalid `.l3lint.yaml` | 3 | stderr with the file and path |
| Internal error | A rule throws | 3 | stderr with a short ID; the full stack only with `--verbose` |

A rule that throws is isolated: the engine records an internal error for that rule and transaction, then runs the rest, so one bug never hides other findings.

**Logging.** `Microsoft.Extensions.Logging` writes to stderr; stdout carries only results so pipes stay clean. `--log-level` takes `error`, `warn`, `info`, `debug` or `trace`; `--log-format json` emits one JSON object per line. Event IDs are stable (1000 to 1999 input, 2000 to 2999 rules, 3000 to 3999 output) so log alerts survive upgrades.

**Performance plan**

- Parse with `Utf8JsonReader` and source-generated `System.Text.Json` contexts.
- Compute derived facts once per transaction (pipeline stage 6).
- Precompile every regex with `[GeneratedRegex]`; load lists into `FrozenSet<string>` with ordinal, case-insensitive comparers.
- Allocate findings only when a rule fails; the passing path allocates nothing per rule.
- Benchmarks in CI fail the build when single-payload p95 regresses more than 15% against the last release.

**Extension points**

| Extension | How | Available in |
| --- | --- | --- |
| Custom rules | Implement `IRule` or `ICustomCheck`, register with `services.AddL3Lint(o => o.AddRule<T>())` | Library |
| Custom adapters | Implement `IPayloadAdapter` and register it | Library |
| Custom output | Implement `IReportWriter` | Library |
| Placeholder terms, thresholds, severities | `.l3lint.yaml` | CLI, MCP, Action, library |
| Processor profiles | YAML profile files (`profiles/*.yaml`) with limits and rule toggles | CLI, MCP, Action, library |
| Rule packs | Local pack via `--rule-pack` | CLI, Action |

The CLI deliberately does not load plugin assemblies in v1: loading third-party code would break the "no code from data" guarantee. Teams that need custom code use the library.
