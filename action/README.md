# l3lint GitHub Action

Lints commercial card Level 2 and Level 3 payloads in pull requests: fixtures, recorded requests and sample payloads. It uploads SARIF to code scanning, writes a job summary, adds inline annotations, and fails the job by `fail-on`.

The action installs .NET and a pinned l3lint version, then lints only the files that `paths` names. It refuses absolute paths and paths that leave the workspace. l3lint runs offline: it sends nothing anywhere.

## Usage

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

Pin the action to a release tag or, better, to its full commit SHA.

## Inputs

| Name | Default | Meaning |
| --- | --- | --- |
| `paths` | required | Newline-separated globs inside the workspace, for example `fixtures/payments/**/*.json` |
| `format` | `auto` | Same as `--format` |
| `brand`, `card-product`, `mcc`, `processor`, `as-of` | empty | Same as the CLI flags |
| `config` | `.l3lint.yaml` | Config file path; used when the file exists |
| `fail-on` | `error` | `error`, `warning`, `information` or `none` |
| `sarif-file` | `l3lint.sarif` | Where SARIF is written |
| `upload-sarif` | `true` | Upload SARIF with `github/codeql-action/upload-sarif` |
| `annotations` | `true` | Emit workflow command annotations, for repositories without code scanning |
| `version` | this action's `VERSION` file | l3lint tool version to install |
| `dotnet-version` | `10.0.x` | SDK for `actions/setup-dotnet` |
| `tool-source` | empty | Extra NuGet source to install the tool from, for testing a locally built package |

## Outputs

| Name | Meaning |
| --- | --- |
| `errors`, `warnings` | Finding counts |
| `exit-code` | The CLI exit code: 0 clean, 1 findings at or above `fail-on`, 2 usage or input error, 3 rule pack, config or internal error |
| `sarif-file` | Path of the SARIF file |

## How it runs

`scripts/run.sh` runs `l3lint lint` with `--output text --output sarif=<sarif-file> --output markdown=$GITHUB_STEP_SUMMARY`, adds `--annotations github` when `annotations` is `true`, writes the counts to `$GITHUB_OUTPUT`, and always exits 0 so the upload step runs. The last step applies the real exit code. Inputs reach the script only through environment variables.

Code scanning upload needs code scanning on the repository; on private repositories that is a paid GitHub feature. With `upload-sarif: false`, annotations and the job summary still work everywhere.

Third-party actions inside `action.yml` are pinned by full commit SHA: `actions/setup-dotnet` v6.0.0 and `github/codeql-action/upload-sarif` v4.39.1.
