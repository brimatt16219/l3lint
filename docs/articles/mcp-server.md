# MCP server

The l3lint MCP server lets an AI agent lint a payment payload it just wrote, read why a rule exists, and get safe mechanical fixes. It uses the [Model Context Protocol](https://modelcontextprotocol.io/) through the official ModelContextProtocol C# SDK, and runs the same engine and rule packs as the CLI, so an agent gets the same findings you do.

All six tools are read-only. The server makes no network calls, sends no telemetry, never reads files and keeps nothing after a call returns.

## Add it to a client

The server ships as the NuGet package `L3Lint.Mcp` with the `McpServer` package type, so a client can start it with `dnx` (part of the .NET 10 SDK):

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

The file name and the top-level key depend on the client; check its documentation. The command and arguments stay the same. This configuration needs the package on NuGet.org. Before the first release, build the repository (`dotnet build L3Lint.slnx`) and give the client the command `dotnet` with one argument: the full path of `src/L3Lint.Mcp/bin/Debug/net10.0/l3lint-mcp.dll`.

The package also carries `.mcp/server.json` for the official MCP Registry, under the name `io.github.brimatt16219/l3lint`.

## Tools

| Tool | Input | Returns |
| --- | --- | --- |
| `lint_payload` | `payload` (a JSON object, or a string holding JSON, at most 2 MB), `format?`, `context?`, `failOn?`, `maxFindings?` | The results for the payload in the [JSON results](../reference/results.md) shape: findings, summary and likely tier |
| `explain_rule` | `ruleId` | Requirement, rationale, severity, confidence, effective dates, sources, and a passing and a failing example |
| `list_rules` | `brand?`, `level?`, `query?` | Rule IDs, titles and severities |
| `map_payload` | `payload`, `format?` | The canonical JSON, the path map, derived fields and adapter notes (the same document as `l3lint convert`) |
| `suggest_fixes` | `payload`, `format?`, `context?` | An RFC 6902 patch in the payload's own format, plus the findings a person must decide |
| `get_schema` | `name`: `canonical`, `results`, `config`, `rulepack`, `csvmap`, `rates`, `profile` or `lock` | The JSON Schema |

`context` is an object with `brand`, `cardProduct`, `mcc`, `merchantCountry`, `currency`, `processor` and `asOf`, the same values as the CLI flags. Values in `context` beat values in the payload. `format` takes `canonical`, `stripe`, `adyen`, `braintree`, `cybersource`, `authorizenet` or `checkoutcom`; omit it to detect the format.

Every tool returns structured content and a short text summary for clients that only read text. The tool annotations mark every tool as read-only, idempotent, not destructive and closed-world.

Here is `lint_payload` on `INV-20418.json`; the text summary reads:

```text
1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0. Likely visa tier: below Product 3 (blocked by 1 error).
VISA-L3-004 at /lineItems/1/commodityCode: Line 2 has no commodity code; Visa needs one on every line for Product 3.
```

## Errors the model can act on

Bad input does not raise a protocol error. It returns a tool result with `isError: true` and a message that says what to change, so the model can correct itself:

```json
{"result":{"content":[{"type":"text","text":"No rule has the ID VISA-L3-999. Call list_rules to see every rule ID."}],"isError":true},"id":3,"jsonrpc":"2.0"}
```

The same happens for a payload that is not JSON, a payload over 2 MB, an unknown format, a format that cannot be detected and an invalid context value.

## Resources and prompts

| Kind | Name | Content |
| --- | --- | --- |
| Resource template | `l3lint://rules/{ruleId}` | The rule's reference page as Markdown, the same page as on this site |
| Resource template | `l3lint://schemas/{name}` | A JSON Schema |
| Resource template | `l3lint://examples/{brand}/{name}` | A canonical payload that meets the documented Level 3 requirements of a brand. Brands: `visa`, `mastercard`, `amex`, `discover`. Name: `level3`. |
| Prompt | `review_payload` | Lint a payload, explain each error in plain words, propose the patch and list what needs a human decision |
| Prompt | `explain_downgrade` | Given a payload and the interchange category it settled in, list the findings that explain the downgrade |

## Transports

**stdio** is the default: the client starts `l3lint-mcp` and talks over standard input and output. Logs go to standard error only, because standard output carries the protocol.

**Streamable HTTP** serves the protocol at `http://127.0.0.1:<port>/mcp`:

```text
$ l3lint-mcp --help
Usage: l3lint-mcp [--http] [--port <n>] [--token <token>] [--allow-origin <origin>]... [--version] [--help]. Default transport: stdio. --http serves Streamable HTTP at http://127.0.0.1:<port>/mcp (default port 7341). The token can also come from the L3LINT_MCP_TOKEN environment variable.
```

The HTTP server keeps no session state between requests.

## Security of the HTTP transport

A web page in a browser can try to reach a server on `127.0.0.1` (DNS rebinding). The HTTP transport defends against that:

- It binds the loopback address only, never another interface.
- It answers only requests whose `Host` header is `127.0.0.1`, `localhost` or `[::1]`. Others get 403.
- It rejects a browser `Origin` that is not localhost, unless you allow it with `--allow-origin <origin>`. Rejected origins get 403.
- With `--token <token>`, or the `L3LINT_MCP_TOKEN` environment variable, every request must send `Authorization: Bearer <token>`. Others get 401. The environment variable keeps the token out of process listings.

These responses come from a run with a token set:

```text
$ L3LINT_MCP_TOKEN=s3cret-test-token l3lint-mcp --http --port 7341
$ curl -s -X POST http://127.0.0.1:7341/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"curl","version":"1"}}}'
A bearer token is required. Send it as Authorization: Bearer <token>.
$ curl -s -X POST http://127.0.0.1:7341/mcp -H "Authorization: Bearer s3cret-test-token" -H "Origin: https://example.com" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"curl","version":"1"}}}'
This origin may not call the l3lint MCP server. Start it with --allow-origin to allow it.
```

and from a run without a token:

```text
$ curl -s -X POST http://127.0.0.1:7341/mcp -H "Host: attacker.example" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
This server only answers requests addressed to 127.0.0.1 or localhost.
```

Use a token whenever other users share the machine.

## Privacy

The server sees only what the client sends in a tool call. Card numbers in the payload are masked before any rule reads it, and every output shows only the last four digits. Nothing is written to disk and nothing persists between calls. See [Privacy and security](privacy-and-security.md).
