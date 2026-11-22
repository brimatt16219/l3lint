# ADR-0001: Rules come only from public sources

- Status: Accepted
- Date: 2026-10-25

## Context

Card brand specifications such as Visa's Enhanced Data Services File Specifications go to acquirers, not the public. Decision D1 in the Overview settles this.

## Decision

Every rule, list and processor profile value cites at least one public URL. The rule pack schema requires a non-empty `sources` list. Confidence labels say how strong each source is. Non-public material is never cited or paraphrased.

## Alternatives considered

Using acquirer specifications would give stricter checks but would make the project unshareable and could breach confidentiality terms.

## Consequences

Some checks stay `inferred`. The `explain` command and the rule reference show every source so users can judge each finding.

## Spec IDs affected

D1, FR-012, FR-036, G2
