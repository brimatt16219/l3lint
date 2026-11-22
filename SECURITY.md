# Security policy

## Reporting a vulnerability

Please report security problems privately through GitHub security advisories: open the repository's **Security** tab and choose **Report a vulnerability**. Do not open a public issue, pull request or discussion for a vulnerability.

Include what you found, the l3lint version (`l3lint version`), the component (CLI, MCP server, GitHub Action or library), and the smallest input that shows the problem. Use synthetic data and public test card numbers only. Never send a real card number or real customer data, even to show a masking problem.

The maintainers will confirm the report, work on a fix in a private advisory, and credit you in the advisory unless you ask not to be named.

## Supported versions

| Version | Supported |
| --- | --- |
| 1.0.x | Yes |
| Earlier versions | No |

Security fixes ship as 1.0.x patch releases of the tool, the library packages, the MCP server and the GitHub Action. Rule packs version on their own and get patch releases when a fix touches them.

## Scope

In scope:

- **Card-number handling.** A card number that reaches any output, log, patch or written file unmasked, when it is a whole value that the documented algorithm should detect. See [Privacy and security](docs/articles/privacy-and-security.md). A card number inside a longer free text is a documented limit of the algorithm, not a vulnerability, though reports that improve it are welcome as normal issues.
- **Data leaving the machine.** Any network call from the CLI, the library, the MCP server or the rule engine, or payload values in logs without `--log-values`.
- **The MCP HTTP transport.** A way to reach it from another host, to bypass the Host, Origin or bearer token checks, or to make it serve a web page's requests.
- **Hostile input.** Inputs that crash l3lint, hang it, or use unbounded memory despite the input limits, including regular expression backtracking.
- **Rule packs and configuration.** A pack, profile, column map, rate table or config file that makes l3lint run code, read files it was not given, or skip the manifest check.
- **The GitHub Action.** Shell injection through inputs, or reading files outside the workspace.
- **Supply chain.** Tampering with released packages, binaries or the Action.

Out of scope:

- Wrong or missing rules, and differences from how a card brand or processor treats a transaction. Findings are advisory. Report these as normal issues, with public sources.
- Problems that need an attacker who can already replace the installed l3lint binaries or packages on your machine.
- Problems in third-party dependencies with no effect on l3lint; report those upstream.
