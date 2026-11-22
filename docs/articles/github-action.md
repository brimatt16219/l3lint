# GitHub Action

The l3lint Action lints payment payloads in pull requests: fixtures, recorded requests and sample payloads. It uploads SARIF to code scanning, writes a job summary, adds inline annotations, and fails the job by `fail-on`.

It is a composite action in the `action/` folder of the l3lint repository. It installs .NET and a pinned l3lint version, then lints only the files that `paths` names.

## A workflow

```yaml
name: payments-lint
on:
  pull_request:
    paths: ["fixtures/payments/**"]
permissions:
  contents: read
  security-events: write   # only needed for the SARIF upload
jobs:
  l3lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: brimatt16219/l3lint/action@v1.0.0
        with:
          paths: |
            fixtures/payments/**/*.json
          brand: all
          fail-on: error
```

This is `samples/action-example/.github/workflows/payments-lint.yml`. `l3lint init` writes a similar workflow to `.github/workflows/l3lint.yml`.

## Inputs

| Input | Default | Meaning |
| --- | --- | --- |
| `paths` | required | Newline-separated globs inside the workspace, for example `fixtures/payments/**/*.json` |
| `format` | `auto` | Same as `--format` |
| `brand` | empty | Same as `--brand` |
| `card-product` | empty | Same as `--card-product` |
| `mcc` | empty | Same as `--mcc` |
| `processor` | empty | Same as `--processor` |
| `as-of` | empty | Same as `--as-of` |
| `config` | `.l3lint.yaml` | Config file. The default is used only when the file exists; a path you name must exist. |
| `fail-on` | `error` | `error`, `warning`, `information` or `none` |
| `sarif-file` | `l3lint.sarif` | Where SARIF is written |
| `upload-sarif` | `true` | Upload SARIF with `github/codeql-action/upload-sarif` |
| `annotations` | `true` | Emit workflow annotations, for repositories without code scanning |
| `version` | the Action's `VERSION` file | l3lint tool version to install |
| `dotnet-version` | `10.0.x` | SDK for `actions/setup-dotnet` |
| `tool-source` | empty | An extra NuGet source (a folder or a feed URL) to install the tool from, for testing a locally built package (ADR-0027) |

## Outputs

| Output | Meaning |
| --- | --- |
| `errors` | Error count |
| `warnings` | Warning count |
| `exit-code` | The CLI exit code: 0 clean, 1 findings at or above `fail-on`, 2 usage or input error, 3 rule pack, config or internal error |
| `sarif-file` | Path of the SARIF file |

## Permissions

| Permission | Why |
| --- | --- |
| `contents: read` | Check out the repository |
| `security-events: write` | Upload SARIF to code scanning. Not needed with `upload-sarif: false`. |

Code scanning must be available on the repository for the upload. On private repositories that is a paid GitHub feature. With `upload-sarif: false`, the annotations and the job summary still work everywhere.

## How it runs

1. `actions/setup-dotnet` installs the SDK.
2. `dotnet tool install l3lint` installs the pinned version into a folder under the runner's temp directory.
3. `scripts/run.sh` runs `l3lint lint` over `paths` with `--output text --output sarif=<sarif-file> --output markdown=$GITHUB_STEP_SUMMARY`, adds `--annotations github` when `annotations` is `true`, and writes the counts to `$GITHUB_OUTPUT`. It always exits 0, so the upload step runs even when the lint fails.
4. `github/codeql-action/upload-sarif` uploads the SARIF file with the category `l3lint`, when `upload-sarif` is `true` and a SARIF file was written.
5. The last step exits with the real l3lint exit code, which fails the job when findings reach `fail-on`.

Inputs reach the script only through environment variables, never through text pasted into the script, so a crafted input cannot run shell code. The script refuses absolute paths and paths that leave the workspace.

A local run of the script against the sample repository, with the built CLI (`L3LINT_BIN` points the script at it):

```text
$ cd samples/action-example
$ L3LINT_BIN=l3lint GITHUB_OUTPUT=out.txt L3LINT_PATHS='fixtures/payments/**/*.json' L3LINT_FAIL_ON=error L3LINT_SARIF=l3lint.sarif L3LINT_ANNOTATIONS=true bash ../../action/scripts/run.sh
```

prints the text report and one annotation:

```text
2 transactions, 1 error, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
::error file=fixtures/payments/INV-20418.json,line=59,col=5,title=VISA-L3-004::Line 2 has no commodity code; Visa needs one on every line for Product 3. Fix: Add the most specific 8-digit UNSPSC code for this item. (/lineItems/1/commodityCode)
```

and writes these outputs:

```text
errors=1
warnings=2
exit-code=1
sarif-file=l3lint.sarif
sarif-written=true
```

A path outside the workspace is refused before l3lint runs:

```text
::error title=l3lint::The path '../outside.json' leaves the workspace. Use a relative path or glob inside the repository.
```

with `exit-code=2`.

## What you see in a pull request

- **Code scanning alerts** from the SARIF upload, one per finding, each linking to the rule's page on this site. Fingerprints use the rule ID, the JSON path and the transaction ID, so an alert survives edits that move lines.
- **Inline annotations** on the files for findings at or above `fail-on`.
- **A job summary** in Markdown: counts by brand and one section per transaction with findings. See [Output formats](output-formats.md#markdown).

Delete `INV-20418.json` from the sample to see the job turn green.

## Pinning

- Pin the Action to a release tag such as `v1.0.0`, or better, to the full commit SHA of that release.
- The Action pins l3lint itself to the version in its `VERSION` file, so a given Action release always runs the same tool. Override it with `version`.
- Inside `action.yml`, third-party actions are pinned by full commit SHA: `actions/setup-dotnet` v6.0.0 and `github/codeql-action/upload-sarif` v4.39.1.
- Add a `.l3lint.lock` (from `l3lint init`) so a rule pack change shows up as a lock mismatch, not as new findings.

## Privacy

The Action reads only the files that `paths` matches. It writes results to the runner, the job summary and, when enabled, the code scanning API. l3lint itself makes no network calls; only the setup steps and the SARIF upload use the network.
