# Action example

A tiny repository layout that shows the l3lint GitHub Action at work.

- `fixtures/payments/INV-20417.json` is the spec's worked example. It lints with one Visa warning (VISA-L3-010), so the job passes with `fail-on: error`.
- `fixtures/payments/INV-20418.json` is the same payload with the second line's commodity code removed. It fails VISA-L3-004, so the job fails and the finding shows as a code scanning alert and an inline annotation.
- `.github/workflows/payments-lint.yml` runs the action on pull requests that touch the fixtures.

Delete `INV-20418.json` to see the job turn green.
