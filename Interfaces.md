# 4. Interfaces: CLI, MCP server, GitHub Action, library

## CLI

The CLI has eight commands. `lint` does the real work; the rest explain, fix, convert and manage rules. It installs as a .NET tool (`dotnet tool install -g l3lint`) or runs as a single-file binary.

| Command | Purpose | Stories |
| --- | --- | --- |
| `l3lint lint <inputs...>` | Lint files, globs, NDJSON or stdin (`-`) | US-01 to US-04, US-13, US-14 |
| `l3lint explain <ruleId>` | Show a rule's requirement, sources, confidence, dates and examples | US-05 |
| `l3lint fix <input>` | Print a JSON Patch of mechanical fixes (`--dry-run`, default) or write a patched copy (`--write <path>`) | US-06 |
| `l3lint convert <input> --to canonical` | Map a gateway payload to canonical JSON, with the path map | US-02 |
| `l3lint rules list` / `show <id>` / `test` | Browse rules; run every rule's pass and fail examples | US-11 |
| `l3lint schema <name>` | Print the canonical, results, config or rule pack JSON Schema | US-08 |
| `l3lint init` | Write `.l3lint.yaml`, a sample payload and a sample workflow | US-07 |
| `l3lint version` | Print engine and rule pack versions | All |

**`lint` options**

| Option | Values | Default | Notes |
| --- | --- | --- | --- |
| `--format` | `auto`, `canonical`, `stripe`, `adyen`, `braintree`, `cybersource`, `authorizenet`, `checkoutcom`, `csv` | `auto` | Skips detection when set |
| `--map` | Path | None | Column map, required with `csv` |
| `--brand` | `auto`, `visa`, `mastercard`, `amex`, `discover`, `all` | `auto` | `auto` reads `card.brand`; `all` lints against every brand |
| `--card-product` | `consumer`, `business`, `corporate`, `purchasing`, `fleet`, `commercialPrepaid` | From payload | Overrides the payload with a note |
| `--mcc` | 4 digits | From payload |  |
| `--merchant-country` | ISO alpha-2 | `US` |  |
| `--processor` | Profile name | `generic` | Loads `profiles/<name>.yaml` |
| `--as-of` | `YYYY-MM-DD` | `transactionDate`, else today | Picks the rules in force on that date |
| `--config` | Path | Nearest `.l3lint.yaml` walking up from the current directory |  |
| `--rule-pack` | Path, repeatable | Embedded packs | For rule authors |
| `--only`, `--disable` | Rule IDs or globs such as `VISA-DQ-*` | None |  |
| `--severity` | `RULE=level`, repeatable | Pack defaults | For example `GEN-DQ-007=error` |
| `--fail-on` | `error`, `warning`, `information`, `none` | `error` | Sets exit code 1 |
| `--output` | `text`, `json`, `sarif`, `junit`, `markdown`; `kind=path` to write a file; repeatable | `text` to stdout | `--output json --output sarif=l3lint.sarif` |
| `--max-findings` | Integer | Unlimited | The summary still counts everything |
| `--rates` | Path | None | Enables cost estimates |
| `--strict-pci` | Flag | Off | Stop instead of masking |
| `--strict-profiles` | Flag | Off | Run single-processor rules as errors |
| `--no-color`, `--quiet`, `--verbose` | Flags |  | `NO_COLOR` also disables color |
| `--log-level`, `--log-format` | See Architecture | `warn`, `text` | Logs go to stderr |

One more option exists for CI: `--annotations github` also prints GitHub workflow-command annotations (`::error file=...,line=...::message`) to stdout, one per finding at or above `--fail-on`. The GitHub Action turns it on; it defaults to `none`.

**Precedence.** Command-line flags beat values in the payload, which beat the config file, which beats built-in defaults. Every override prints a one-line note so a reviewer can see why a rule ran.

**Exit codes.** 0 clean or below `--fail-on`; 1 findings at or above `--fail-on`; 2 usage or input error; 3 rule pack, config or internal error.

**Example session**

```text
$ l3lint lint invoices/INV-20417.stripe.json --brand visa --card-product purchasing --mcc 5047
INV-20417  stripe  visa purchasing  MCC 5047  as of 2026-10-19
  error    VISA-L3-004   /amount_details/line_items/1/payment_method_options/card/commodity_code
           Line 2 has no commodity code; Visa needs one on every line for Product 3.
           Fix: add the most specific 8-digit UNSPSC code for this item.
  warning  GEN-DQ-004    /payment_details/customer_reference
           "111111" looks like filler, not the buyer's reference.
  Likely tier: below Product 3 (blocked by 1 error)

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.2.0, VISA 1.2.0.
$ echo $?
1
```

## Configuration file

`.l3lint.yaml` holds team defaults so CI and laptops lint the same way. It is validated against `config.schema.json` at startup; an invalid file stops the run with exit code 3 and the failing path.

```yaml
# .l3lint.yaml
version: 1

context:                       # defaults when the payload and flags are silent
  merchantCountry: US
  mcc: "5047"
  processor: generic          # or a profile name, e.g. stripe, adyen, jpmorgan

inputs:
  format: auto
  csvMap: config/erp-map.yaml
  limits:
    maxFileBytes: 26214400     # 25 MB
    maxLineItems: 1000
    maxJsonDepth: 64

rules:
  disable: [GEN-DQ-008]
  severity:
    GEN-DQ-007: error
    VISA-L3-010: information
  placeholders:
    add: ["supplies", "parts"]
    remove: ["order"]
  batch:
    minTransactions: 20
    sameValueShare: 0.80

arithmetic:
  lineTolerance: 0.01
  totalTolerance: 0.01
  toleranceMode: strict        # or accumulated
  rounding: awayFromZero       # or toEven

pci:
  strict: false
  allowFields: []              # canonical JSON Pointers, after human review

output:
  failOn: error
  formats:
    - text
    - sarif=artifacts/l3lint.sarif
  maxFindings: 500

rates: config/ic-plus-2026-04.yaml   # optional; enables cost estimates
```

| Key | Type | Default | Meaning |
| --- | --- | --- | --- |
| `version` | integer | 1 | Config schema version |
| `context.*` | object | Empty | Defaults for brand, card product, MCC, merchant country, processor |
| `inputs.format` | string | `auto` | Same values as `--format` |
| `inputs.csvMap` | path | None | Column map for CSV input |
| `inputs.limits.*` | integers | 25 MB, 1,000, 64 | Hard input limits (FR-034) |
| `rules.disable` | list | Empty | Rule IDs or globs to skip |
| `rules.severity` | map | Empty | Per-rule severity overrides |
| `rules.placeholders.add`, `.remove` | lists | Empty | Edits to the placeholder term list |
| `rules.batch.*` | numbers | 20, 0.80 | Thresholds for `GEN-DQ-007` |
| `arithmetic.*` | mixed | See Card brand rules | Tolerance and rounding |
| `pci.strict`, `pci.allowFields` | bool, list | false, empty | Card-number handling |
| `output.failOn` | string | `error` | Same as `--fail-on` |
| `output.formats` | list | `[text]` | Same as repeated `--output` |
| `output.maxFindings` | integer | Unlimited | Same as `--max-findings` |
| `rates` | path | None | Rate table for cost estimates |

Relative paths resolve from the config file's folder. Unknown keys are errors, not warnings, so typos never silently change behavior.

## Output formats

Five formats render one `LintResult`. JSON is the contract for machines; SARIF is the contract for GitHub; text and Markdown are for people; JUnit is for CI dashboards.

**JSON** (validated against `results.schema.json`)

```json
{
  "tool": { "name": "l3lint", "version": "1.0.0" },
  "rulePacks": { "l3lint.rules.general": "1.2.0", "l3lint.rules.visa": "1.2.0" },
  "asOf": "2026-10-19",
  "inputs": [
    { "path": "invoices/INV-20417.stripe.json", "format": "stripe", "sha256": "9f2c...", "transactions": 1 }
  ],
  "transactions": [
    {
      "index": 0,
      "input": 0,
      "id": "INV-20417",
      "brand": "visa",
      "cardProduct": "purchasing",
      "likelyTier": { "best": "product3", "likely": "belowProduct3", "status": "blocked" },
      "findings": [
        {
          "ruleId": "VISA-L3-004",
          "severity": "error",
          "impact": "blocksLevel3",
          "path": {
            "canonical": "/lineItems/1/commodityCode",
            "source": "/amount_details/line_items/1/payment_method_options/card/commodity_code"
          },
          "location": { "line": 31, "column": 9 },
          "message": "Line 2 has no commodity code; Visa needs one on every line for Product 3.",
          "expected": "8-digit commodity code",
          "actual": null,
          "fix": "Add the most specific 8-digit UNSPSC code for this item.",
          "patch": null,
          "confidence": "processor-doc",
          "sources": ["https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program"]
        }
      ]
    }
  ],
  "summary": { "transactions": 1, "errors": 1, "warnings": 1, "information": 0, "byRule": { "VISA-L3-004": 1, "GEN-DQ-004": 1 } },
  "exitCode": 1
}
```

**SARIF 2.1.0 mapping.** GitHub code scanning accepts a subset of SARIF 2.1.0, at most 5,000 results and 10 MB gzipped per upload ([GitHub Docs](https://docs.github.com/en/enterprise-server@3.1/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning)).

| SARIF property | Value |
| --- | --- |
| `runs[0].tool.driver.name`, `.version`, `.informationUri` | `l3lint`, engine version, project URL |
| `runs[0].tool.driver.rules[]` | One entry per rule that fired: `id`, `name` (title), `shortDescription`, `fullDescription`, `helpUri` (rule reference page), `defaultConfiguration.level`, `properties.tags` (brand, level, confidence) |
| `results[].ruleId`, `.level` | Rule ID; `error`, `warning` or `note` |
| `results[].message.text` | Message plus "Fix:" text |
| `results[].locations[0].physicalLocation` | Input file URI relative to the repository root; `region.startLine` and `startColumn` from the JSON token position |
| `results[].logicalLocations[0].fullyQualifiedName` | Source JSON Pointer |
| `results[].partialFingerprints["l3lint/v1"]` | SHA-256 of rule ID, source pointer and transaction ID, so alerts survive line moves |
| `results[].properties` | `transactionIndex`, `expected`, `actual`, `confidence`, `sources`, `impact` |
| `runs[0].automationDetails.id` | `l3lint/` plus the config hash, used as the upload category |

When results exceed 5,000, the writer keeps errors first, then warnings, and adds one note result stating how many were dropped.

**JUnit XML.** One `testsuite` per input file, one `testcase` per transaction named by transaction ID. Each error finding becomes a `failure` element; warnings and information go to `system-out`.

**Markdown.** A summary table (transactions, errors, warnings by brand) followed by one collapsible section per failing transaction. The GitHub Action writes this to the job summary.

**Text.** The human format shown in the CLI section. Severity words always print, so color is never the only signal (NFR-010).

## MCP server

The MCP server lets an AI agent lint a payload it just generated, read why a rule exists, and apply safe fixes, all through six read-only tools. It is built on the official ModelContextProtocol C# SDK and ships as a NuGet package with the `McpServer` package type, so clients can launch it with `dnx` ([Microsoft Learn](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)).

**Tools**

| Tool | Input | Output (structured content) | Annotations |
| --- | --- | --- | --- |
| `lint_payload` | `payload` (object or JSON string, at most 2 MB), `format?`, `context?` {`brand`, `cardProduct`, `mcc`, `merchantCountry`, `processor`, `asOf`}, `failOn?`, `maxFindings?` | The JSON result for one transaction: findings, summary, likely tier | readOnly, idempotent, closed world |
| `explain_rule` | `ruleId` | Title, requirement, rationale, severity, confidence, effective dates, sources, pass and fail examples | readOnly, idempotent |
| `list_rules` | `brand?`, `level?`, `query?` | Rule IDs, titles, severities | readOnly, idempotent |
| `map_payload` | `payload`, `format?` | Canonical JSON, path map, derived fields, adapter notes | readOnly, idempotent |
| `suggest_fixes` | `payload`, `format?`, `context?` | RFC 6902 patch in the source format, plus a list of findings that need a human decision | readOnly, idempotent |
| `get_schema` | `name`: `canonical`, `results`, `config`, `rulepack` | The JSON Schema | readOnly, idempotent |

Every tool also returns a short text summary next to the structured content, for clients that only read text.

**Errors the model can act on.** A payload that is not valid JSON, an unknown format or a rule ID that does not exist returns a tool result with `isError: true` and a message that says what to change. The MCP specification asks servers to report input validation problems this way so the model can correct itself ([MCP changelog](https://modelcontextprotocol.io/specification/2025-11-25/changelog)). Protocol errors are reserved for malformed requests.

**Resources and prompts**

| Kind | Name | Content |
| --- | --- | --- |
| Resource template | `l3lint://rules/{ruleId}` | The rule reference page as Markdown |
| Resource template | `l3lint://schemas/{name}` | JSON Schemas |
| Resource template | `l3lint://examples/{brand}/{name}` | Sample passing payloads per brand |
| Prompt | `review_payload` | Lint the payload, explain each error in plain words, propose the patch, and list what needs a human |
| Prompt | `explain_downgrade` | Given a payload and the interchange category it settled in, list the findings that explain it |

**Tool definition in C#**

```csharp
[McpServerToolType]
public sealed class LintTools(ILinter linter, IAdapterRegistry adapters)
{
    [McpServerTool(Name = "lint_payload", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lint a commercial card payload against Visa, Mastercard, Amex and Discover enhanced-data rules.")]
    public LintToolResult LintPayload(
        [Description("The payment request body, as a JSON object or string.")] JsonElement payload,
        [Description("Input format; omit to auto-detect.")] string? format = null,
        [Description("Brand, card product, MCC, processor and as-of date.")] LintContextDto? context = null,
        [Description("Lowest severity that counts as failing.")] string failOn = "error")
    {
        var document = RawDocument.FromJson(payload, maxBytes: 2 * 1024 * 1024);
        var adapter = adapters.Resolve(format, document);      // throws a validation error the SDK reports as isError
        var result = linter.Lint(adapter.Map(document, AdapterOptions.Default), context.ToOptions(failOn));
        return LintToolResult.From(result);
    }
}
```

Confirm the attribute property names against the SDK version you install; the SDK is new and names may differ slightly.

**Transports**

- **stdio** (default): `l3lint-mcp`. Logs go to stderr only, because stdout carries JSON-RPC.
- **Streamable HTTP**: `l3lint-mcp --http --port 7341`. Binds 127.0.0.1, checks the `Origin` header, and accepts an optional `--token` for a bearer token. A newer protocol revision removes protocol-level sessions from Streamable HTTP ([MCP changelog](https://modelcontextprotocol.io/specification/latest/changelog)); the server keeps no session state, so it works either way.

**Client configuration** (after the package is on NuGet.org)

```json
{
  "servers": {
    "l3lint": {
      "type": "stdio",
      "command": "dnx",
      "args": ["L3Lint.Mcp", "--yes"]
    }
  }
}
```

**Publishing.** The package includes `.mcp/server.json`, its README carries an `mcp-name` comment, and a release step runs `mcp-publisher publish` to list the server in the official MCP Registry after NuGet finishes validating the package ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry)).

## GitHub Action

The Action is a composite action that installs .NET 10 and a pinned l3lint version, lints the files a team names (fixtures, recorded requests, sample payloads), writes SARIF and a job summary, and fails the job by `fail-on`. It never reads files outside `paths`.

**Inputs and outputs**

| Name | Kind | Default | Meaning |
| --- | --- | --- | --- |
| `paths` | input, required |  | Newline-separated globs, for example `fixtures/payments/**/*.json` |
| `format` | input | `auto` | Same as `--format` |
| `brand`, `card-product`, `mcc`, `processor`, `as-of` | input | Empty | Same as the CLI flags |
| `config` | input | `.l3lint.yaml` | Config file path |
| `fail-on` | input | `error` | `error`, `warning`, `information`, `none` |
| `sarif-file` | input | `l3lint.sarif` | Where SARIF is written |
| `upload-sarif` | input | `true` | Upload to code scanning with `github/codeql-action/upload-sarif` |
| `annotations` | input | `true` | Emit workflow command annotations on changed lines, for repositories without code scanning |
| `version` | input | The Action's own version | l3lint tool version to install |
| `dotnet-version` | input | `10.0.x` | SDK for `actions/setup-dotnet` |
| `errors`, `warnings` | output |  | Counts |
| `exit-code` | output |  | The CLI exit code |
| `sarif-file` | output |  | Path of the written SARIF |

**action.yml** (abridged; pin every third-party action by full commit SHA in the real file)

```yaml
name: l3lint
description: Lint commercial card Level 2 and Level 3 payloads before they reach production.
branding: { icon: check-circle, color: blue }
inputs:
  paths: { description: Globs of payload files, required: true }
  fail-on: { description: Lowest failing severity, default: error }
  sarif-file: { description: SARIF output path, default: l3lint.sarif }
  upload-sarif: { description: Upload SARIF to code scanning, default: "true" }
  annotations: { description: Emit inline annotations, default: "true" }
  version: { description: l3lint version; empty means the version in this action's VERSION file, default: "" }
  dotnet-version: { description: .NET SDK, default: 10.0.x }
outputs:
  errors: { description: Error count, value: "${{ steps.lint.outputs.errors }}" }
  warnings: { description: Warning count, value: "${{ steps.lint.outputs.warnings }}" }
  exit-code: { description: CLI exit code, value: "${{ steps.lint.outputs.exit-code }}" }
runs:
  using: composite
  steps:
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: "${{ inputs.dotnet-version }}" }
    - shell: bash
      env: { L3LINT_VERSION: "${{ inputs.version }}" }
      run: dotnet tool install --global l3lint --version "${L3LINT_VERSION:-$(cat "$GITHUB_ACTION_PATH/VERSION")}"
    - id: lint
      shell: bash
      run: "$GITHUB_ACTION_PATH/scripts/run.sh"
      env:
        L3LINT_PATHS: ${{ inputs.paths }}
        L3LINT_FAIL_ON: ${{ inputs.fail-on }}
        L3LINT_SARIF: ${{ inputs.sarif-file }}
        L3LINT_ANNOTATIONS: ${{ inputs.annotations }}
    - if: ${{ always() && inputs.upload-sarif == 'true' }}
      uses: github/codeql-action/upload-sarif@v4
      with: { sarif_file: "${{ inputs.sarif-file }}", category: l3lint }
    - if: ${{ always() }}
      shell: bash
      run: exit "${{ steps.lint.outputs.exit-code }}"
```

`scripts/run.sh` runs `l3lint lint` with `--output text --output sarif=$L3LINT_SARIF --output markdown=$GITHUB_STEP_SUMMARY`, adds `--annotations github` when enabled, writes the counts to `$GITHUB_OUTPUT`, and always exits 0 so the upload step runs; the final step re-applies the real exit code. GitHub documents `upload-sarif@v4` for third-party SARIF ([GitHub Docs](https://docs.github.com/en/enterprise-server@3.16/code-security/how-tos/find-and-fix-code-vulnerabilities/integrate-with-existing-tools/upload-sarif-file)).

**Example workflow**

```yaml
name: payments-lint
on:
  pull_request:
    paths: ["fixtures/payments/**", "src/Payments/**"]
permissions:
  contents: read
  security-events: write   # only needed for SARIF upload
jobs:
  l3lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: <owner>/l3lint-action@v1
        with:
          paths: |
            fixtures/payments/**/*.json
          brand: all
          fail-on: error
```

Code scanning upload needs code scanning to be available on the repository; on private repositories that is a paid GitHub feature. With `upload-sarif: false`, annotations and the job summary still work everywhere.

## Library API

The library lets a .NET service lint a payload in process, right before it calls the gateway. It is the same engine the CLI uses, registered through dependency injection.

**Packages**

| Package | Contents |
| --- | --- |
| `L3Lint` | Meta-package: Core, Hosting, Adapters, Reporting and all embedded rule packs |
| `L3Lint.Core` | Model, engine, detectors, abstractions; no rule packs |
| `L3Lint.Rules.General`, `.Visa`, `.Mastercard`, `.Amex`, `.Discover` | One rule pack each, versioned independently |
| `L3Lint.Adapters` | Gateway and CSV adapters |
| `L3Lint.Reporting` | Output writers |

**Public surface**

```csharp
public interface ILinter
{
    LintResult Lint(Transaction transaction, LintOptions? options = null);
    LintResult Lint(RawDocument document, LintOptions? options = null);   // adapter auto-detect
    IAsyncEnumerable<TransactionResult> LintManyAsync(
        IAsyncEnumerable<Transaction> transactions,
        LintOptions? options = null,
        CancellationToken cancellationToken = default);
    RuleMetadata Explain(string ruleId);
    IReadOnlyList<RuleMetadata> Rules(RuleQuery? query = null);
}

public sealed record LintOptions
{
    public CardBrand? Brand { get; init; }
    public CardProduct? CardProduct { get; init; }
    public string? Mcc { get; init; }
    public string? Processor { get; init; }
    public DateOnly? AsOf { get; init; }
    public Severity FailOn { get; init; } = Severity.Error;
    public ArithmeticOptions Arithmetic { get; init; } = new();
    public IReadOnlySet<string> DisabledRules { get; init; } = FrozenSet<string>.Empty;
}
```

**Registration and use**

```csharp
builder.Services.AddL3Lint(options =>
{
    options.Processor = "generic";
    options.FailOn = Severity.Error;
});

app.MapPost("/invoices/{id}/charge", async (string id, InvoiceService invoices, ILinter linter, PaymentGateway gateway) =>
{
    Transaction tx = invoices.ToCanonical(id);          // your mapping, or an adapter
    LintResult result = linter.Lint(tx);
    if (result.Summary.Errors > 0)
        return Results.UnprocessableEntity(result.Findings.Where(f => f.Severity == Severity.Error));
    return Results.Ok(await gateway.ChargeAsync(tx));
});
```

**Guarantees.** `ILinter` is a thread-safe singleton. After startup it does no I/O: rule packs, lists and the currency table load once. A warm 10-line payload lints in under 1 ms (US-09). Exceptions never escape `Lint` for bad payload data; they become findings. Only programming errors, such as a null argument, throw.
