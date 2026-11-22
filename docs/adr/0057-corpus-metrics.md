# ADR-0057: How the corpus counts recall and false positives

- Status: Accepted
- Date: 2026-10-30

## Context

The spec sets two corpus gates: recall of at least 95% on seeded defects and at most 2% false positives on known-good payloads (Product requirements, success metrics; Test and delivery tab). It also says each fixture "yields exactly its expected findings", lists ten mutation operators applied to 40 valid bases, and gives an `*.expect.yaml` format with `findings`, `absent`, `exitCode` and `likelyTier`. It does not say how a mutation that no rule covers for a given brand is counted, or exactly what the false-positive denominator is.

## Decision

- **Generation.** `L3Lint.FixtureGen corpus` writes the corpus with a fixed seed and its own SplitMix64 generator, so the files never depend on the runtime's random number generator. `--check` fails when a committed file differs. The generator does not reference the engine; every expectation comes from the operator and the rule packs.
- **Expected findings.** An expected finding must appear with its rule ID, canonical path and, when stated, severity. Severity is stated only when the pack and ADR-0016 fix it (rules tied to a processor profile are warnings under the generic profile). Other findings may also appear: a mutation can break more than one documented requirement, and the `absent` list names the rules that must stay quiet.
- **Operators without a rule for a brand.** Visa and Discover keep no unit-of-measure list, and Amex and Discover have no tax ratio rule. For those brands the fixture expects no finding and lists the other brands' rules (and the arithmetic rules) as absent. These fixtures test precision, not recall.
- **Recall** is found expected findings divided by all expected findings across the 400 seeded fixtures.
- **False positives.** A known-good payload is a false positive when it gets any error or warning, the wrong exit code or the wrong likely tier. The rate is false-positive payloads divided by the 200 known-good payloads. Information findings (such as "no Discover rate") are not defects. Separately, at most 2% of seeded fixtures may trip a rule in their `absent` list or give the wrong exit code.
- **Rule examples** are linted with only their own rule selected (context `only`), as `l3lint rules test` does, so a rule that another rule supersedes on the same field can still be shown to fire. The batch rule GEN-DQ-007 repeats its fail example 20 times (context `copies`), the default minimum batch.
- **Known-good currencies.** Visa and Mastercard known-good payloads use US dollars, because those programs are US dollar programs; Amex and Discover payloads also use euro and yen.

## Alternatives considered

- Counting false positives per finding over all corpus findings: rejected, because the denominator would grow with the seeded set and hide noise on clean payloads.
- Dropping operators a brand has no rule for: rejected, because the spec fixes 40 bases times 10 operators, and the quiet cases are useful precision checks.

## Consequences

The corpus has 836 payloads: 156 rule examples, 70 adapter fixtures, 400 seeded defects, 200 known-good payloads and 10 PCI payloads. A new rule or a changed default can change the expected findings; the failing test names the fixture and lists the actual findings.

## Spec IDs affected

T-22, success metrics (recall, false positives), NFR-001 (determinism over the corpus), NFR-007 (PCI set)
