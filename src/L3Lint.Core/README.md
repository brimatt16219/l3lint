# L3Lint.Core

The l3lint engine: the canonical commercial card payload model, money and currency handling, card-number masking, the rule engine, the declarative check kinds, the data quality detectors and the arithmetic checks.

This package has no rule packs and no gateway adapters. Most applications want `L3Lint.Hosting` instead, which brings the engine, the five rule packs, the adapters and the output writers.

```text
dotnet add package L3Lint.Core
```

Findings are advisory: they say a payload is likely to downgrade or meets the documented requirement. The engine is offline and does no I/O after it starts.

Documentation: https://brimatt16219.github.io/l3lint/articles/library.html
