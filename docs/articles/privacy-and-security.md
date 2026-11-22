# Privacy and security

Payment payloads can hold customer names, addresses, invoice data and, now and then, a card number pasted by mistake. l3lint is built to keep all of it on your machine and to stay out of PCI DSS scope.

## Offline by design

- The CLI, the library and the MCP server make no network calls. There is no telemetry, no crash reporting and no update check.
- Rule packs, lists, profiles, schemas and the currency table are embedded, so nothing is downloaded at run time.
- The MCP server sees only what a client sends in a tool call, and keeps nothing after the call returns. Its HTTP transport listens on the loopback address only.
- The GitHub Action reads only the files its `paths` input matches. It writes results to the runner, the job summary and, when enabled, the code scanning API.

## Card numbers are masked first

Card-number detection runs before an adapter reads the payload, so no rule, log line or output ever sees a full card number. The algorithm (ADR-0023):

1. Visit every string and number token in the raw document. For CSV, visit every cell.
2. Remove spaces and hyphens. Skip values that are not 13 to 19 digits long.
3. Run the Luhn check. Skip values that fail it.
4. Check the issuer prefix and length: Visa (starts with 4; 13, 16 or 19 digits), Mastercard (51 to 55 or 2221 to 2720; 16 digits), American Express (34 or 37; 15 digits) or Discover (6011, 644 to 649 or 65; 16 to 19 digits).
5. Report GEN-PCI-001. In a code field (`productCode`, `commodityCode`, `sequence`) it is a warning, because any random 16-digit code passes the Luhn check about one time in ten. Everywhere else it is an error.
6. Replace the value with asterisks and the last four digits. Every later stage and every output sees only the masked value.

```text
$ l3lint lint fixtures/adapters/braintree/07-card-number-in-order-id.json --currency USD --only GEN-PCI-001
Transaction 1  braintree  unknown  as of 2026-10-30
  error       GEN-PCI-001        /orderId
           A card number was found at /orderId and masked as ************4444.
           Fix: Remove the card number; send only the last four digits in card.last4.
           Source: https://www.pcisecuritystandards.org/standards/pci-dss/  (policy)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0.
```

The input held the public Mastercard test number 5555 5555 5555 4444 in its `orderId`. Masking runs even when GEN-PCI-001 is disabled; disabling the rule only hides the finding. `l3lint convert` shows the masked value and counts the masked numbers in `cardNumbersMasked`.

### The limit of this check

The check looks at whole values. A card number inside a longer text, such as a description that reads "Paid with card 4111 1111 1111 1111, thanks", is not one 13 to 19 digit value after spaces and hyphens are removed, so it is not detected or masked. The spec's algorithm works on whole tokens, and ADR-0023 follows it exactly. Do not rely on l3lint to find card numbers inside free text.

```text
$ sed 's/"description": "Isolation gown blue cs 50"/"description": "Paid with card 4111 1111 1111 1111, thanks"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only GEN-PCI-001 --quiet
1 transaction, 0 errors, 0 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ sed 's/"description": "Isolation gown blue cs 50"/"description": "4111 1111 1111 1111"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only GEN-PCI-001 --quiet
1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

### --strict-pci

With `--strict-pci` (or `pci.strict: true`), l3lint stops instead of continuing with the masked value:

```text
$ l3lint lint fixtures/adapters/braintree/07-card-number-in-order-id.json --currency USD --strict-pci
Transaction 1  braintree  unknown  as of 2026-10-30
  Note: Stopped because --strict-pci is set and a card number was found. Remove the card number and lint again.
  error       GEN-PCI-001        /orderId
           A card number was found at /orderId and masked as ************4444.
           Fix: Remove the card number; send only the last four digits in card.last4.
           Source: https://www.pcisecuritystandards.org/standards/pci-dss/  (policy)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0.
```

The exit code is 1. `fix` and `convert` also stop with exit code 1 under `--strict-pci` and write nothing.

### pci.allowFields

Some fields hold long digit strings that are not card numbers, such as a numeric reference that happens to pass the Luhn check. After a person has confirmed that, list the field in `.l3lint.yaml`:

```yaml
pci:
  allowFields:
    - /order/invoiceNumber
    - /lineItems/*/productCode
```

The values are canonical JSON Pointers; `*` matches any line index. For CSV input, a column is exempt when the canonical field it maps to matches. Values in other fields are still checked.

## Logs never carry payload values

Logs go to standard error; standard output carries only results. By default, log lines carry rule IDs, paths and counts, never field values. `--log-values` adds the (already masked) values and prints a warning first:

```text
$ l3lint lint fixtures/adapters/braintree/07-card-number-in-order-id.json --currency USD --only GEN-PCI-001 --log-values --log-level info --quiet
warning: --log-values writes masked payload values to the log. Keep these logs private.
info: L3Lint.Findings[2003] GEN-PCI-001 at /order/invoiceNumber: ************4444
1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0.
```

`--log-format json` writes one JSON object per line for log collectors.

## Hostile input

| Threat | Control |
| --- | --- |
| Huge files | 25 MB per file by default (`inputs.limits.maxFileBytes`); for NDJSON, per record |
| Deep JSON | Depth 64 by default (`inputs.limits.maxJsonDepth`) |
| Too many lines | 1,000 line items per transaction by default (`inputs.limits.maxLineItems`) |
| Regular expression backtracking | Rule pack patterns run with a 100 ms timeout |
| A malicious rule pack | Packs are data only; they are validated against a schema and their SHA-256 manifest before use, and `custom` checks resolve only to code compiled into the tool |
| YAML tricks | Config, packs, profiles and maps are read as plain data, with no type tags |
| An unexpected pack version | `.l3lint.lock` stops the run on any version mismatch |

An input over a limit is an input error: that input is skipped, the others are linted, and the run exits 2.

## The MCP HTTP transport

The HTTP transport binds 127.0.0.1 only, answers only requests addressed to `127.0.0.1`, `localhost` or `[::1]`, rejects browser origins other than localhost unless you allow them, and supports a bearer token (`--token` or `L3LINT_MCP_TOKEN`). These checks defend against DNS rebinding from a web page. See [MCP server](mcp-server.md#security-of-the-http-transport).

## Reporting a vulnerability

See `SECURITY.md` in the repository. Please report privately through GitHub security advisories, not in a public issue.
