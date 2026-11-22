# L3Lint.Rules.Visa

The l3lint Visa rule pack, `l3lint.rules.visa`: 26 rules as versioned YAML data, the lists they use, a passing and a failing example for each rule, and the compiled checks the pack names. Every rule cites the public pages it comes from and carries a confidence label and effective dates.

```text
dotnet add package L3Lint.Rules.Visa
```

`L3Lint.Hosting` already includes all five rule packs. Add this package on its own when you build a linter from `L3Lint.Core` with `LinterBuilder.AddRulePack(new VisaRulePack())`.

Rule reference: https://brimatt16219.github.io/l3lint/rules/index.html
