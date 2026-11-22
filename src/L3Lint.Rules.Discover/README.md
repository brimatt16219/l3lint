# L3Lint.Rules.Discover

The l3lint Discover rule pack, `l3lint.rules.discover`: 3 rules as versioned YAML data, with a passing and a failing example for each rule. Every rule cites the public pages it comes from and carries a confidence label and effective dates.

```text
dotnet add package L3Lint.Rules.Discover
```

`L3Lint.Hosting` already includes all five rule packs. Add this package on its own when you build a linter from `L3Lint.Core` with `LinterBuilder.AddRulePack(new DiscoverRulePack())`.

Rule reference: https://brimatt16219.github.io/l3lint/rules/index.html
