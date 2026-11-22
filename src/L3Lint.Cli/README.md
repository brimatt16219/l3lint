# l3lint

The l3lint command-line tool. It lints commercial card Level 2 and Level 3 payloads against Visa, Mastercard, American Express and Discover requirements before submission, explains each likely downgrade and says how to fix it. It reads canonical JSON, six gateway formats, NDJSON and CSV, and writes text, JSON, SARIF, JUnit and Markdown.

```text
dotnet tool install --global l3lint
l3lint lint payload.json
```

It runs offline: no network calls, no telemetry, and card numbers are masked before any rule reads the payload. Findings are advisory: they say a payload is likely to downgrade or meets the documented requirement.

Documentation: https://brimatt16219.github.io/l3lint/
