# L3Lint.Mcp

<!-- mcp-name: io.github.brimatt16219/l3lint -->

The l3lint MCP server lets an AI agent lint a commercial card payment payload it just wrote, read why a rule exists, and get safe mechanical fixes. It checks Level 2 and Level 3 data against Visa, Mastercard, American Express and Discover requirements. It runs offline and keeps nothing after a call.

Findings are advisory. They say a payload is likely to downgrade, or meets the documented requirement. They never promise an interchange rate.

## Use it from a client

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

## Tools

| Tool | What it does |
| --- | --- |
| `lint_payload` | Lints one payload (canonical JSON, Stripe, Adyen, Braintree, Cybersource, Authorize.net or Checkout.com) and returns findings, a summary and the likely tier |
| `explain_rule` | Requirement, rationale, severity, confidence, dates, sources and examples of one rule |
| `list_rules` | Rule IDs, titles and severities, filtered by brand, level or text |
| `map_payload` | The canonical JSON of a gateway payload, with the path map and adapter notes |
| `suggest_fixes` | An RFC 6902 patch of mechanical fixes, plus the findings a person must decide |
| `get_schema` | The canonical, results, config or rule pack JSON Schema |

Resources: `l3lint://rules/{ruleId}`, `l3lint://schemas/{name}` and `l3lint://examples/{brand}/level3`. Prompts: `review_payload` and `explain_downgrade`.

## Transports

- stdio (default): `l3lint-mcp`. Logs go to standard error.
- Streamable HTTP: `l3lint-mcp --http --port 7341` serves `http://127.0.0.1:7341/mcp`. It binds the loopback address only, rejects other Host headers and browser origins, and accepts an optional bearer token with `--token` or the `L3LINT_MCP_TOKEN` environment variable. `--allow-origin <origin>` allows one more browser origin.

## Privacy

Card numbers are masked before any rule sees the payload. The server makes no network calls, sends no telemetry and keeps no state between calls.

Documentation: https://brimatt16219.github.io/l3lint/
