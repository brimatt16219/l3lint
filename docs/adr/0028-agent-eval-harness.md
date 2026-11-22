# ADR-0028: Agent evaluation harness

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 18 and the agent repair metric.

## Decision

`tools/L3Lint.AgentEval` drives the MCP server with a Claude model through the official Anthropic C# SDK. It reads `ANTHROPIC_API_KEY` from the environment and exits with a clear message when it is absent. It is not part of any shipped package and is never run by the shipped tool.

## Alternatives considered

Scripted agents were rejected because the metric is about a real model.

## Consequences

The repair rate is measured only when the owner supplies a key.

## Spec IDs affected

T-47, success metrics
