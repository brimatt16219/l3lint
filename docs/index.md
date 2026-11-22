# l3lint

l3lint checks commercial card Level 2 and Level 3 payment payloads before you send them. It compares each payload with the public Visa, Mastercard, American Express and Discover requirements, explains every likely downgrade, and says how to fix it.

It runs on your machine. It makes no network calls, sends no telemetry and masks anything that looks like a card number before any rule reads the payload.

## What you get

- A **CLI** (`l3lint`) that lints files, folders, globs, NDJSON and standard input, and exits non-zero when it finds errors.
- An **MCP server** (`l3lint-mcp`) so an AI agent can lint the payloads it writes and apply safe fixes.
- A **GitHub Action** that lints payload fixtures on every pull request and uploads SARIF to code scanning.
- A **.NET library** that runs the same checks inside your service, right before it calls the gateway.

All four use one engine and the same rule packs, so the same payload gets the same findings everywhere.

## Findings are advisory

A finding says a payload is likely to downgrade, or that it misses a documented requirement. A clean result says the payload meets the documented requirements that l3lint checks. Card networks and issuers make the final decision about the rate, and Visa reviews enhanced data after settlement. l3lint reports risk, not outcomes. See [Limitations](articles/limitations.md).

## Where to start

| If you want to | Read |
| --- | --- |
| Lint your first payload in five minutes | [Getting started](articles/getting-started.md) |
| Install the tool or a single-file binary | [Installation](articles/installation.md) |
| Look up one rule, such as VISA-L3-004 | [Rule reference](rules/index.md) |
| See every command and option | [CLI reference](reference/cli.md) |
| Read JSON results in code | [JSON results reference](reference/results.md) |
| Gate pull requests | [GitHub Action](articles/github-action.md) |
| Let an AI agent check its own payloads | [MCP server](articles/mcp-server.md) |
| Lint inside a .NET service | [.NET library](articles/library.md) |

l3lint is open source under the Apache License 2.0.
