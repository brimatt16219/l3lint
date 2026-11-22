# CLI commands and options exercised end to end

Every command and every option of `l3lint` is passed through the real command line parser (`CliApplication.RunAsync`) in at least one test in `tests/L3Lint.Cli.Tests`. The list of options comes from `l3lint <command> --help`. The test names come from scanning the test files for each token inside a `RunAsync` call (2026-11-02).

The in-process tests give the CLI its own output and error writers, but the console logger always writes to the process's standard error. So the log content checks live in `tests/L3Lint.Security.Tests` (`LogPrivacyTests`), which run the CLI with the process console captured.

Options shared by several commands (the input, context and rule selection options on `lint`, `fix` and `convert`) go through the same `ContextOptions` and `SettingsResolver` code on every command, so one end-to-end test per option covers the shared path. `--rule-pack`, which eight commands take, is tested on each of them: `lint`, `explain`, `version`, `rules list`, `rules show` and `rules test` load a local pack, and a tampered local pack stops `lint`, `fix` and `convert` with exit 3.

## Commands

| Command | Example tests |
| --- | --- |
| `lint` | 53 tests, for example `ConfigTests.Nearest_config_walking_up_is_used` |
| `explain` | `RuleCommandTests.Explain_shows_requirement_sources_confidence_dates_and_examples` |
| `fix` | `FixConvertInitTests.Fix_dry_run_prints_the_line_total_patch` |
| `convert` | `FixConvertInitTests.Convert_canonical_has_an_identity_path_map` |
| `rules list` | `RuleCommandTests.Rules_list_shows_every_pack_with_version_dates_and_source_count` |
| `rules show` | `RuleCommandTests.Rules_show_prints_the_reference_page` |
| `rules test` | `RuleCommandTests.Rules_test_runs_every_example_and_passes` |
| `schema` | `RuleCommandTests.Schema_prints_each_schema_byte_for_byte` |
| `init` | `FixConvertInitTests.Init_writes_four_files_that_lint_clean` |
| `version` | `RuleCommandTests.Version_prints_engine_and_pack_versions` |

## Options

| Option | Example tests |
| --- | --- |
| `-f`, `--format` | `LintCommandTests.Csv_with_a_column_map_groups_rows_into_transactions`, `MoreOptionTests.Short_aliases_match_their_long_options` |
| `--map` | `LintCommandTests.Csv_with_a_column_map_groups_rows_into_transactions` |
| `--currency` | `LintCommandTests.Currency_flag_and_config_key_fill_a_payload_without_currency` |
| `--brand` (lint) | `LintCommandTests.Brand_all_lints_against_every_brand` |
| `--brand` (rules list) | `RuleCommandTests.Rules_list_filters` |
| `--card-product` | `LintCommandTests.Card_product_flag_overrides_the_payload_and_says_so` |
| `--mcc` | `LintCommandTests.Csv_with_a_column_map_groups_rows_into_transactions` |
| `--merchant-country` | `MoreOptionTests.Merchant_country_flag_overrides_the_payload_with_a_note` |
| `--processor` | `MoreOptionTests.Processor_flag_selects_the_profile_that_owns_a_rule`, `ConfigTests.Unknown_processor_profile_is_a_configuration_error` |
| `--as-of` | `LintCommandTests.As_of_before_a_rule_takes_effect_skips_it` |
| `--config` | `ConfigTests.Explicit_config_is_used_and_flags_beat_it`, `ConfigTests.Missing_explicit_config_is_a_usage_error` |
| `--rule-pack` | `MoreOptionTests.A_local_rule_pack_works_with_lint_explain_version_and_rules` (lint, explain, version, rules list, rules show, rules test), `MoreOptionTests.A_tampered_local_pack_is_refused_with_exit_three` (lint, fix, convert) |
| `--only` | `LintCommandTests.Only_disable_and_severity_change_rule_selection`, `RuleCommandTests` (rules test) |
| `--disable` | `LintCommandTests.Only_disable_and_severity_change_rule_selection` |
| `--severity` | `LintCommandTests.Only_disable_and_severity_change_rule_selection` |
| `--rates` | `OutputTests.Cost_estimates_need_a_rate_table_and_are_labeled`, `OutputTests.Missing_rate_table_flag_is_a_usage_error` |
| `--strict-pci` | `FixConvertInitTests.Fix_with_strict_pci_stops`, `FixConvertInitTests.Convert_masks_card_numbers_and_honors_strict_pci` |
| `--strict-profiles` | `MoreOptionTests.Strict_profiles_runs_processor_rules_at_their_own_severity` |
| `--fail-on` | `LintCommandTests.Fail_on_warning_turns_the_warning_into_exit_one`, `LintCommandTests.Errors_exit_one_unless_fail_on_none` |
| `-o`, `--output` | 24 tests, for example `LintCommandTests.Standard_input_with_several_lines_is_ndjson`, `MoreOptionTests.Short_aliases_match_their_long_options` |
| `--max-findings` | `LintCommandTests.Max_findings_caps_output_but_the_summary_counts_everything` |
| `--annotations` | `OutputTests.Github_annotations_cover_findings_at_or_above_fail_on` |
| `--no-color` | `OutputTests.Color_only_on_a_terminal_without_no_color` |
| `-q`, `--quiet` | `LintCommandTests.Quiet_and_verbose_change_what_is_printed`, `MoreOptionTests.Short_aliases_match_their_long_options` |
| `-v`, `--verbose` | `MoreOptionTests.Verbose_prints_where_settings_came_from`, `MoreOptionTests.Short_aliases_match_their_long_options` |
| `--log-level` | `LintCommandTests.Card_numbers_are_masked_in_every_output`, `LogPrivacyTests.Trace_logs_carry_no_payload_values` |
| `--log-format` | `LintCommandTests.Logs_go_to_standard_error_only`, `LogPrivacyTests.Trace_logs_carry_no_payload_values` (text and json) |
| `--log-values` | `LintCommandTests.Log_values_prints_a_warning`, `LogPrivacyTests.Log_values_is_the_only_way_to_get_values_into_logs_and_they_are_masked` |
| `--dry-run` | `MoreOptionTests.Fix_dry_run_flag_prints_the_default_patch`, `FixConvertInitTests.Fix_rejects_bad_targets` |
| `--write` | `FixConvertInitTests.Fix_write_creates_a_patched_copy_that_lints_cleaner` |
| `--fix-amount` | `FixConvertInitTests.Fix_amount_is_only_patched_with_fix_amount` |
| `--to` | `FixConvertInitTests.Convert_canonical_has_an_identity_path_map` |
| `--canonical-only` | `FixConvertInitTests.Convert_canonical_only_round_trips_the_payload` |
| `-d`, `--directory` | `FixConvertInitTests.Init_writes_into_another_directory`, `MoreOptionTests.Short_aliases_match_their_long_options` |
| `--force` | `FixConvertInitTests.Init_never_overwrites_without_force` |
| `--level` | `RuleCommandTests.Rules_list_filters` |
| `--query` | `RuleCommandTests.Rules_list_filters` |
| `-h`, `--help` | `RuleCommandTests.Help_lists_the_eight_commands`, `MoreOptionTests.Short_aliases_match_their_long_options` |

The packed tool and the single-file binary were also run by hand (see packaging-dry-run.md), and the Action script tests (`ActionScriptTests`) run the CLI the way the GitHub Action does.
