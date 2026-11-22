# ADR-0017: Processor profiles are embedded YAML

- Status: Accepted
- Date: 2026-10-25

## Context

Clarification 7 lists the profiles and their content.

## Decision

Profiles ship embedded in `L3Lint.Core`: generic (default), stripe, adyen, braintree, cybersource, authorizenet, checkoutcom, jpmorgan, mastercard-gateway-amex and amex-advice-text. Each profile has limits (line-item caps, field lengths, unit list, Amex lines forwarded) and rule toggles (disable, severity). Every value carries a source URL; the profile schema requires it. `--processor <path>` loads a custom profile file. Values that public pages do not state are left out instead of guessed.

## Alternatives considered

Profiles in C# were rejected because they are data that changes with gateway docs.

## Consequences

Rules read limits by `profileKey`; a missing key means the rule's default, or no check when there is no default.

## Spec IDs affected

FR-011, US-02
