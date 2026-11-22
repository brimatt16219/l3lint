# l3lint

l3lint checks commercial card Level 2 and Level 3 payment payloads before you send them. It compares each payload with the public Visa, Mastercard, American Express and Discover requirements, explains every likely downgrade, and says how to fix it.

Enhanced data fails quietly: a transaction with bad Level 3 data still approves, then settles at a higher interchange rate. Since Visa's Commercial Enhanced Data Program, placeholder descriptions, line items that do not add up and missing fields fail Visa's review. l3lint finds those problems in seconds, while they are still cheap to fix.

- **CLI** (`l3lint`): lints files, folders, globs, NDJSON and standard input; exits non-zero on errors.
- **MCP server** (`l3lint-mcp`): lets an AI agent lint the payloads it writes and apply safe fixes.
- **GitHub Action**: lints payload fixtures on every pull request and uploads SARIF to code scanning.
- **.NET library**: runs the same checks inside your service, before it calls the gateway.

It reads l3lint's canonical JSON, Stripe, Adyen, Braintree, Cybersource, Authorize.net and Checkout.com request bodies, and CSV exports with a column map. It writes text, JSON, SARIF, JUnit XML and Markdown.

## Advisory, not a guarantee

Findings are advisory. A finding says a payload is likely to downgrade, or that it misses a documented requirement. A clean result says the payload meets the documented requirements that l3lint checks. l3lint is not a guarantee: card networks and issuers decide the rate, and Visa reviews enhanced data after settlement. Every rule cites the public pages it comes from and carries a confidence label, so you can judge each finding. Read [Limitations](https://brimatt16219.github.io/l3lint/articles/limitations.html) for what l3lint cannot see.

l3lint runs offline. It makes no network calls, sends no telemetry, and masks anything that looks like a card number before any rule reads the payload.

## Install

```text
dotnet tool install --global l3lint
```

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Self-contained single-file binaries for Windows, macOS and Linux (x64 and arm64) are attached to each GitHub release. See [Installation](https://brimatt16219.github.io/l3lint/articles/installation.html), which also shows how to build and install from source.

## Quick start

These commands run from the root of this repository, so the sample files exist.

Lint a Visa purchasing card payload with one missing commodity code:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20418.json
INV-20418  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  error       VISA-L3-004        /lineItems/1/commodityCode
           Line 2 has no commodity code; Visa needs one on every line for Product 3.
           Fix: Add the most specific 8-digit UNSPSC code for this item.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 1 error)

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

The exit code is 1 because of the error, so a CI job fails. Ask why the rule exists:

```text
$ l3lint explain VISA-L3-004
# VISA-L3-004: Commodity code is required for Product 3
```

(The page continues with the requirement, the reason, the check, the sources and a passing and a failing example.)

Lint a Stripe PaymentIntent request. Gateway requests rarely carry the card brand, card product or MCC, so pass them:

```text
$ l3lint lint fixtures/adapters/stripe/01-complete-level3.json --brand visa --card-product purchasing --mcc 5047 --quiet
1 transaction, 3 errors, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Without `--quiet`, each finding points at the field in the Stripe request, for example `/amount_details/line_items/1/discount_amount`.

Write SARIF for GitHub code scanning next to the text report:

```text
$ l3lint lint samples/action-example/fixtures/payments --output text --output sarif=l3lint.sarif --quiet
2 transactions, 1 error, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

In your own project folder, write a config file, a lock file, a sample payload and a sample workflow:

```text
$ l3lint init
wrote .l3lint.yaml
wrote .l3lint.lock
wrote samples/l3lint-sample.json
wrote .github/workflows/l3lint.yml
Next: run l3lint lint samples/l3lint-sample.json
```

Exit codes: 0 clean, 1 findings at or above `--fail-on` (default `error`), 2 usage or input error, 3 rule pack, config or internal error.

## Documentation

The documentation site is <https://brimatt16219.github.io/l3lint/>. Its source is in [docs/](docs/).

- [Getting started](https://brimatt16219.github.io/l3lint/articles/getting-started.html)
- [Using the CLI](https://brimatt16219.github.io/l3lint/articles/cli.html) and the [CLI reference](https://brimatt16219.github.io/l3lint/reference/cli.html)
- [Rule reference](https://brimatt16219.github.io/l3lint/rules/index.html): one page per rule, with sources and examples
- [Configuration](https://brimatt16219.github.io/l3lint/articles/configuration.html)
- [Output formats and exit codes](https://brimatt16219.github.io/l3lint/articles/output-formats.html)
- [Likely tiers](https://brimatt16219.github.io/l3lint/articles/likely-tiers.html)
- [Privacy and security](https://brimatt16219.github.io/l3lint/articles/privacy-and-security.html)

## MCP server

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

Six read-only tools: `lint_payload`, `explain_rule`, `list_rules`, `map_payload`, `suggest_fixes` and `get_schema`. See [MCP server](https://brimatt16219.github.io/l3lint/articles/mcp-server.html) and [src/L3Lint.Mcp](src/L3Lint.Mcp/README.md).

## GitHub Action

```yaml
- uses: brimatt16219/l3lint/action@v1.0.0
  with:
    paths: |
      fixtures/payments/**/*.json
    fail-on: error
```

See [GitHub Action](https://brimatt16219.github.io/l3lint/articles/github-action.html) and [action/README.md](action/README.md).

## .NET library

```text
dotnet add package L3Lint.Hosting
```

```csharp
builder.Services.AddL3Lint(options => options.Processor = "generic");
```

See [.NET library](https://brimatt16219.github.io/l3lint/articles/library.html).

## Contributing

Rules come only from public sources, and every rule cites them. See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md) for reporting a vulnerability, and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## License

l3lint is licensed under the [Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for bundled data and third-party notices. l3lint is not affiliated with or endorsed by any card brand, processor or gateway it names.
