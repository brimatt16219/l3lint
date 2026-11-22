# ADR-0052: Markdown job summary layout, escaping and size limit

- Status: Accepted
- Date: 2026-10-26

## Context

The spec asks for "a summary table (transactions, errors, warnings by brand) followed by one collapsible section per failing transaction", written to the GitHub job summary. GitHub limits a step summary to 1 MiB. Payload text (descriptions, IDs, messages that quote values) can contain Markdown and HTML syntax.

## Decision

- **Layout.** An H2 heading (so the page nests under other steps' summaries), report notes as quotes, the text writer's summary line, then the brand table: brand, transactions, errors, warnings, information, counted as JSON `byBrand` does, plus a "Batch findings" row and a bold total row from the report summary. A line counts transactions with no findings. Then an input errors table, one H3 per input with findings, and one `<details>` section per transaction with any finding, holding a findings table (severity, rule ID linked to its reference page with the title as tooltip, path as code, message, fix; errors first), the likely tier for each brand and the cost estimate table when present. Batch findings get their own table, and a closing line counts findings left out by `--max-findings`.
- **"Failing" transaction.** Any transaction with a finding gets a section, because warnings carry the data quality signals (CEDP) users act on.
- **Escaping.** In Markdown text, the backslash, backtick, asterisk, underscore, square brackets, angle brackets, ampersand, pipe, tilde, hash, exclamation mark and dollar sign get a backslash (GFM allows escaping any ASCII punctuation, and a dollar sign starts math on GitHub). Line breaks become `<br>` and control characters become U+FFFD. Inside `<summary>`, where Markdown is not parsed, text is HTML-encoded. Paths are code spans whose fence is longer than any backtick run in the path, with pipes escaped for the table parser.
- **Size.** The page holds at most 1,000,000 UTF-8 bytes. Rendering keeps 4,096 bytes back, stops at the first heading, section or row that would not fit, closes any open `<details>`, and ends with a quoted note giving the number of findings not shown and pointing to the JSON output. Table cells are cut to 2,000 characters with an ellipsis, so one huge value cannot fill the page.

## Alternatives considered

- Sections only for transactions with errors were rejected for the reason above.
- Escaping only pipes was rejected: payload text could otherwise inject links, images, HTML or math into a CI page.
- Cutting at exactly 1 MiB was rejected; the margin covers the closing tags, the note and any text the Action adds.

## Consequences

The job summary is readable for small runs and degrades predictably for large ones. The Markdown output is deterministic, though NFR-001 only requires byte identity for JSON and SARIF.

## Spec IDs affected

FR-024, FR-030, NFR-001, NFR-010, US-07, T-29, T-36
