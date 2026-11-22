# ADR-0056: Default currency for payloads that carry none

- Status: Accepted
- Date: 2026-10-29

## Context

Braintree transaction requests never carry a currency (the merchant account sets it), and Authorize.net requests may omit `currencyCode`. The adapters already accept a default currency (`AdapterOptions.DefaultCurrency`) and add a Defaulted note when they use it, but the spec defines no CLI flag or config key to set it. Without one, every Braintree payload linted from the CLI, the Action or the MCP server reports a missing currency.

## Decision

Add `--currency <ISO 4217 code>` to `lint`, `fix` and `convert`, the config key `inputs.defaultCurrency`, and a `currency` member in the MCP `context` argument. The value applies only when the payload carries no currency; a currency in the payload always wins. An unknown code is a usage error (exit 2). The flag beats the config key, as other flags do.

## Alternatives considered

- Leaving it out: every Braintree payload would fail a format rule that the merchant cannot fix in the payload.
- Guessing from the merchant country: a country does not fix a currency (many merchants settle in more than one).

## Consequences

The CLI option table and the config table in the docs list one more entry than the spec. The config schema gains `inputs.defaultCurrency`.

## Spec IDs affected

FR-003 to FR-008 (adapters), US-02, Interfaces tab lint options and configuration file tables
