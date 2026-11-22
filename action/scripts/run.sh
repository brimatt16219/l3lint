#!/usr/bin/env bash
# Runs l3lint for the GitHub Action (Interfaces tab, "GitHub Action").
#
# Lints only the globs in L3LINT_PATHS, writes text to the log, SARIF to L3LINT_SARIF and Markdown to the job
# summary, adds workflow annotations when enabled, and writes errors, warnings, exit-code, sarif-file and
# sarif-written to GITHUB_OUTPUT. It always exits 0 so the SARIF upload step runs; action.yml re-applies the
# real exit code in its last step.
#
# Inputs come from environment variables, never from string interpolation into this script, so a crafted
# input cannot run shell code.
set -uo pipefail

bin="${L3LINT_BIN:-l3lint}"
output_file="${GITHUB_OUTPUT:-/dev/null}"
sarif="${L3LINT_SARIF:-l3lint.sarif}"
json_report="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/l3lint-action-report.json"

finish() {
  local code="$1" errors="$2" warnings="$3" written="$4"
  {
    echo "errors=$errors"
    echo "warnings=$warnings"
    echo "exit-code=$code"
    echo "sarif-file=$sarif"
    echo "sarif-written=$written"
  } >> "$output_file"
  exit 0
}

trim() {
  local value="$1"
  value="${value#"${value%%[![:space:]]*}"}"
  value="${value%"${value##*[![:space:]]}"}"
  printf '%s' "$value"
}

# The action never reads files outside the paths it is given: absolute paths and parent segments are refused.
args=(lint)
count=0
while IFS= read -r raw || [ -n "$raw" ]; do
  pattern="$(trim "$raw")"
  if [ -z "$pattern" ]; then
    continue
  fi

  case "$pattern" in
    /* | [A-Za-z]:* | .. | ../* | */.. | */../*)
      echo "::error title=l3lint::The path '$pattern' leaves the workspace. Use a relative path or glob inside the repository."
      finish 2 0 0 false
      ;;
  esac

  args+=("$pattern")
  count=$((count + 1))
done <<< "${L3LINT_PATHS:-}"

if [ "$count" -eq 0 ]; then
  echo "::error title=l3lint::The paths input is empty. Name the payload files to lint, for example fixtures/payments/**/*.json."
  finish 2 0 0 false
fi

add_option() {
  if [ -n "$2" ]; then
    args+=("$1" "$2")
  fi
}

add_option --format "${L3LINT_FORMAT:-}"
add_option --brand "${L3LINT_BRAND:-}"
add_option --card-product "${L3LINT_CARD_PRODUCT:-}"
add_option --mcc "${L3LINT_MCC:-}"
add_option --processor "${L3LINT_PROCESSOR:-}"
add_option --as-of "${L3LINT_AS_OF:-}"
add_option --fail-on "${L3LINT_FAIL_ON:-error}"

# The default config is optional; a config named explicitly must exist, and l3lint reports it when it does not.
config="${L3LINT_CONFIG:-}"
if [ -n "$config" ] && { [ -f "$config" ] || [ "$config" != ".l3lint.yaml" ]; }; then
  args+=(--config "$config")
fi

args+=(--output text --output "sarif=$sarif" --output "json=$json_report")
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  args+=(--output "markdown=$GITHUB_STEP_SUMMARY")
fi

if [ "${L3LINT_ANNOTATIONS:-true}" = "true" ]; then
  args+=(--annotations github)
fi

rm -f "$sarif" "$json_report"
"$bin" "${args[@]}"
code=$?

# Read the counts from the summary object of the JSON report, which l3lint writes indented in a fixed order.
errors=0
warnings=0
if [ -f "$json_report" ]; then
  errors="$(awk '/^  "summary": \{/ { inside = 1 } inside && /"errors":/ { gsub(/[^0-9]/, ""); print; exit }' "$json_report")"
  warnings="$(awk '/^  "summary": \{/ { inside = 1 } inside && /"warnings":/ { gsub(/[^0-9]/, ""); print; exit }' "$json_report")"
fi

written=false
if [ -f "$sarif" ]; then
  written=true
fi

finish "$code" "${errors:-0}" "${warnings:-0}" "$written"
