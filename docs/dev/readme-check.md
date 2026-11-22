# README quick-start check (WP15)

Date: 2026-10-30. Every command in the README's "Install" and "Quick start" sections was run against the CLI built from this repository (`dotnet build L3Lint.slnx`, Debug). In the transcripts, `l3lint` is a wrapper script that runs `dotnet src/L3Lint.Cli/bin/Debug/net10.0/l3lint.dll "$@"`. Commands ran in Git Bash on Windows 10 from the repository root, except `l3lint init`, which ran in an empty folder. Exit codes come from `$?`.

## Install

`dotnet tool install --global l3lint` needs the package on NuGet.org, which is not published yet, and a global install would change the machine's global tools. The same install path was checked with a locally packed package and a tool path:

```text
$ dotnet pack src/L3Lint.Cli -c Release -o pkg
(l3lint.1.0.0.nupkg and l3lint.1.0.0.snupkg written)
$ dotnet tool install l3lint --tool-path tools-install --add-source pkg --version 1.0.0
You can invoke the tool using the following command: l3lint
Tool 'l3lint' (version '1.0.0') was successfully installed.
exit 0
$ tools-install/l3lint version
l3lint 1.0.0
  l3lint.rules.amex 1.0.0
  l3lint.rules.discover 1.0.0
  l3lint.rules.general 1.0.0
  l3lint.rules.mastercard 1.0.0
  l3lint.rules.visa 1.0.0
exit 0
```

The local tool manifest flow in `docs/articles/installation.md` was also checked: `dotnet new tool-manifest`, `dotnet tool install l3lint --add-source pkg`, `dotnet tool run l3lint version` (all exit 0, same version output). `dotnet publish src/L3Lint.Cli -c Release -r win-x64` and `-r linux-x64` both succeeded; the win-x64 binary printed the same version output.

Packing needs `LICENSE`, `NOTICE` and a `README.md` in the project folder, because `src/Directory.Build.targets` packs them. WP15 adds all three.

## Quick start

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20418.json
INV-20418  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  error       VISA-L3-004        /lineItems/1/commodityCode
           Line 2 has no commodity code; Visa needs one on every line for Product 3.
           Fix: Add the most specific 8-digit UNSPSC code for this item.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  warning     VISA-L3-010        /lineItems/0/detailIndicator
           2 line(s) have no line-item detail indicator, starting with line 1.
           Fix: Confirm your processor sets it.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  Likely tier: below Product 3 (blocked by 1 error)

1 transaction, 1 error, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
exit 1
```

```text
$ l3lint explain VISA-L3-004 | head -1
# VISA-L3-004: Commodity code is required for Product 3
exit 0 (l3lint explain VISA-L3-004 on its own also exits 0)
```

```text
$ l3lint lint fixtures/adapters/stripe/01-complete-level3.json --brand visa --card-product purchasing --mcc 5047 --quiet
1 transaction, 3 errors, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
exit 1
```

Without `--quiet` the first finding is `VISA-L3-011` at `/amount_details/discount_amount`, and `VISA-L3-009` is at `/amount_details/line_items/1/discount_amount`, as the README says.

```text
$ l3lint lint samples/action-example/fixtures/payments --output text --output sarif=l3lint.sarif --quiet
2 transactions, 1 error, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
exit 1
(l3lint.sarif was written, then deleted)
```

```text
$ l3lint init        (in an empty folder)
wrote .l3lint.yaml
wrote .l3lint.lock
wrote samples/l3lint-sample.json
wrote .github/workflows/l3lint.yml
Next: run l3lint lint samples/l3lint-sample.json
exit 0
```

## Result

Every quick-start command printed the output shown in the README and returned the exit code the README states. The only command not run as written is `dotnet tool install --global l3lint`, for the reason above.

## Recheck on the release binary (2026-11-06)

After the final code changes, the five quick-start commands were run again with the win-x64 binary extracted from `artifacts/binaries/l3lint-1.0.0-win-x64.zip`, from the repository root (`init` in an empty temporary folder). A script read each `$ l3lint` command and its output from the README and compared them; `explain` compared as a prefix, because the README shows only the page's first line.

| Command | Exit code | Output |
| --- | --- | --- |
| `l3lint lint samples/action-example/fixtures/payments/INV-20418.json` | 1 | identical |
| `l3lint explain VISA-L3-004` | 0 | starts with the README's line |
| `l3lint lint fixtures/adapters/stripe/01-complete-level3.json --brand visa --card-product purchasing --mcc 5047 --quiet` | 1 | identical |
| `l3lint lint samples/action-example/fixtures/payments --output text --output sarif=l3lint.sarif --quiet` | 1 | identical |
| `l3lint init` | 0 | identical |

The packed tool also installed from `artifacts/packages` with `dotnet tool install l3lint --tool-path <folder> --add-source artifacts/packages --version 1.0.0` and linted the spec example (exit 0).
