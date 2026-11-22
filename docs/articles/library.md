# .NET library

The library runs the same engine as the CLI inside your .NET service, so you can lint a payload right before you call the gateway. It needs .NET 10.

## Packages

| Package | Contents |
| --- | --- |
| `L3Lint.Hosting` | Everything most services need: the engine, the five rule packs, the gateway and CSV adapters, the output writers, `AddL3Lint()` and the batch runner |
| `L3Lint.Core` | The model, the engine and the abstractions, with no rule packs |
| `L3Lint.Rules.General`, `.Visa`, `.Mastercard`, `.Amex`, `.Discover` | One rule pack each, versioned on its own |
| `L3Lint.Adapters` | Gateway and CSV adapters |
| `L3Lint.Reporting` | Text, JSON, SARIF, JUnit and Markdown writers |

```text
dotnet add package L3Lint.Hosting
```

The spec names the meta-package `L3Lint`. NuGet package IDs ignore case, so that name would collide with the `l3lint` tool; the meta-package is `L3Lint.Hosting` instead (ADR-0031).

## Register and lint

```csharp
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Hosting;

builder.Services.AddL3Lint(options =>
{
    options.Processor = "generic";
    options.FailOn = Severity.Error;
});
```

`AddL3Lint` registers `ILinter` as a singleton, and `LintRunner` for batch runs. Then lint a canonical transaction, for example one your code builds from an invoice:

```csharp
var transaction = new Transaction
{
    Id = "INV-20417",
    Amount = 280.36m,
    Currency = "USD",
    TransactionDate = new DateOnly(2026, 10, 19),
    Card = new CardInfo { Brand = CardBrand.Visa, Product = CardProduct.Purchasing },
    Merchant = new MerchantInfo { Mcc = "5047", Name = "Gulfside Medical Supply" },
    Order = new OrderInfo { PurchaseOrderNumber = "77812" },
    LineItems =
    [
        new LineItem { Sequence = 1, Description = "Nitrile exam gloves L 100", Quantity = 10, UnitCost = 12.50m, Total = 125.00m },
    ],
};

LintResult result = linter.Lint(transaction);
if (result.Summary.Failed)
{
    foreach (var finding in result.Findings.Where(f => f.Severity == Severity.Error))
    {
        Console.WriteLine($"{finding.Severity} {finding.RuleId} {finding.Path.Canonical}: {finding.Message}");
    }
}
```

That transaction is far from complete, so it prints several errors, among them:

```text
Error GEN-ARITH-002 /amount: The amount is 280.36; lines and summary amounts add up to 125.00 (difference 155.36).
Error VISA-L3-004 /lineItems/0/commodityCode: Line 1 has no commodity code; Visa needs one on every line for Product 3.
Error VISA-L3-012 /tax/amount: Tax is absent and the sale is not marked exempt.
```

`Summary.Failed` is true when a finding reaches `FailOn`. Every finding carries the rule ID, severity, impact, canonical and source paths, message, fix, sources and confidence, as in the [JSON results](../reference/results.md).

## Lint a gateway request

`RawDocument` holds raw input. Without `LintOptions.Format`, the linter detects the format:

```csharp
using L3Lint.Core.Input;

var document = RawDocument.ParseJson(File.ReadAllText("stripe-request.json"), "stripe-request.json");
var options = LintOptions.Default with { Brand = CardBrand.Visa, CardProduct = CardProduct.Purchasing, Mcc = "5047" };
LintResult fromGateway = linter.Lint(document, options);
Console.WriteLine($"{fromGateway.Format}: {fromGateway.Summary.Errors} error(s), first at {fromGateway.Findings.First(f => f.Severity == Severity.Error).Path.Source}");
```

With `fixtures/adapters/stripe/01-complete-level3.json` this prints:

```text
stripe: 3 error(s), first at /amount_details/discount_amount
```

`linter.Map(document)` returns the canonical transactions and path maps without linting, like `l3lint convert`. A document that cannot be read at all, such as JSON that is not well-formed, throws `InputException`; a format that cannot be detected throws `FormatDetectionException`.

## Options

`LintOptions` is an immutable record. Options passed to `Lint` replace the defaults from `AddL3Lint` for that call. The main properties:

| Property | Meaning |
| --- | --- |
| `Brand`, `AllBrands` | The brand to lint against, or every brand |
| `CardProduct`, `Mcc`, `MerchantCountry` | Context overrides; they beat the payload |
| `Processor`, `ProcessorProfile` | A [processor profile](processor-profiles.md) |
| `AsOf` | The date whose rules apply; the default is the transaction date, else today |
| `FailOn` | The lowest failing severity; `null` never fails |
| `OnlyRules`, `DisabledRules`, `SeverityOverrides` | Rule selection by ID or glob |
| `Arithmetic` | Tolerance and rounding |
| `StrictPci`, `PciAllowFields` | Card-number handling |
| `Rates` | A rate table for [cost estimates](cost-estimates.md) |
| `Format`, `Adapter`, `Limits` | Input format, adapter options (such as `DefaultCurrency`) and input limits |

## Without dependency injection

`L3LintFactory.CreateBuilder()` returns a `LinterBuilder` with every shipped pack and adapter:

```csharp
ILinter built = L3LintFactory.CreateBuilder()
    .UseDefaults(LintOptions.Default with { FailOn = Severity.Warning })
    .Build();
Console.WriteLine($"rules={built.Rules().Count} packs={string.Join(",", built.RulePacks.Select(p => p.Id))}");
Console.WriteLine(built.Explain("VISA-L3-004").Title);
```

```text
rules=78 packs=l3lint.rules.amex,l3lint.rules.discover,l3lint.rules.general,l3lint.rules.mastercard,l3lint.rules.visa
Commodity code is required for Product 3
```

To pick packs yourself, start from `new LinterBuilder()` and call `AddRulePack(new VisaRulePack())`, `AddAdapter(...)` and so on. Other builder methods: `AddRule`, `AddCustomCheck`, `AddBatchCheck`, `UseTimeProvider`, `UseLoggerFactory` and `RequirePackVersions` (what `.l3lint.lock` uses).

## Batch runs

`LintRunner` lints files, folders, globs and NDJSON in parallel, like `l3lint lint`, and returns a `LintReport` that the writers render:

```csharp
using L3Lint.Hosting.Runs;
using L3Lint.Reporting;

var runner = provider.GetRequiredService<LintRunner>();
LintReport report = await runner.RunAsync(new LintRunRequest
{
    Inputs = ["samples/action-example/fixtures/payments/*.json"],
    WorkingDirectory = repositoryRoot,
});
Console.WriteLine($"exit code {report.ExitCode}, {report.Transactions.Count} transaction(s)");
ReportWriters.TryGet("text", out var writer);
writer.Write(report, Console.Out, ReportWriterOptions.Default with { Quiet = true });
```

```text
exit code 1, 2 transaction(s)
2 transactions, 1 error, 2 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

`LintManyAsync` takes an `IAsyncEnumerable<Transaction>` and yields one result per transaction, then one result of kind `Batch` when batch rules such as GEN-DQ-007 fired. `CreateSession` gives the same batch behavior to callers that feed documents themselves.

## Custom rules

Implement `IRule` and register it with `options.AddRule<T>()`:

```csharp
using L3Lint.Core.Rules;

public sealed class PoPrefixRule : IRule
{
    public RuleMetadata Metadata { get; } = new()
    {
        Id = "GEN-ACME-001",
        Title = "PO numbers start with PO-",
        PackId = "acme",
        PackVersion = "1.0.0",
        Level = RuleLevel.L2,
        Severity = Severity.Warning,
        Confidence = Confidence.Policy,
        Impact = Impact.Hygiene,
        Message = "PO {actual} does not start with PO-.",
        Fix = "Send the PO number as printed on the buyer's order.",
        Check = new RuleCheck("custom", ["/order/purchaseOrderNumber"], []),
        Sources = [new Uri("https://example.com/acme-po-policy")],
    };

    public bool AppliesTo(in RuleContext context) => context.Transaction.Order.PurchaseOrderNumber is not null;

    public void Evaluate(in RuleContext context, IFindingSink sink)
    {
        var po = context.Transaction.Order.PurchaseOrderNumber!;
        if (!po.StartsWith("PO-", StringComparison.Ordinal))
        {
            sink.Report(new FindingDraft(Metadata, "/order/purchaseOrderNumber") { Actual = po });
        }
    }
}
```

With the transaction above, the rule reports:

```text
Warning GEN-ACME-001 /order/purchaseOrderNumber: PO 77812 does not start with PO-.
```

Rules are singletons called from many threads at once, so keep them stateless. A rule that throws does not stop the run: the engine reports `L3LINT-ENGINE-001` for that rule and transaction and runs the others.

Give custom rules an ID that starts with `GEN`, `VISA`, `MC`, `AMEX` or `DISC`, such as `GEN-ACME-001`. The engine accepts other IDs, but `results.schema.json` only accepts those prefixes, so JSON output with another prefix does not validate against the schema.

## Custom checks for rule packs

A [rule pack](rule-pack-authoring.md) is data only. When a declarative check kind is not enough, write the logic as an `ICustomCheck`, register it, and name it from the pack with `check: { kind: custom, check: PoPrefix }`:

```csharp
public sealed class PoPrefixCheck : ICustomCheck
{
    public string Name => "PoPrefix";

    public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
    {
        var po = context.Transaction.Order.PurchaseOrderNumber;
        if (po is not null && !po.StartsWith("PO-", StringComparison.Ordinal))
        {
            sink.Report(new FindingDraft(rule, "/order/purchaseOrderNumber") { Actual = po });
        }
    }
}

builder.Services.AddL3Lint(options =>
{
    options.AddCustomCheck(new PoPrefixCheck());
    options.AddRulePack(new DirectoryRulePackSource("rules/acme-pack"));
});
```

`RuleExampleTester.Run(linter, rules)` runs the passing and failing examples of the rules you give it, like `l3lint rules test`. The CLI cannot load a pack that names a check it does not have, because it never loads plugin code.

## Custom adapters and writers

- Implement `IPayloadAdapter` (name, input kind, a detection score from 0 to 1, and `Map`) and add it with `options.AddAdapter(...)`. See [Adapters](adapters.md) for the rules every adapter follows.
- Implement `IReportWriter` (a format name and `Write(report, output, options)`) for another output format.

## Thread safety and speed

- `ILinter` is thread-safe; register it once and share it.
- Rule packs, lists, profiles and the currency table load when the linter is built. After that, linting does no I/O.
- Bad payload data never throws; it becomes findings. Only programming errors, such as a `null` argument, and input errors on raw documents throw.
- The spec's target is under 1 ms for a warm 10-line payload (US-09). On the machine used to write this page, a Release build averaged 0.34 ms over 10,000 calls after a warm-up. Measure on your own hardware before you rely on a number.

## Configuration files

`L3LintConfiguration.Load(".l3lint.yaml")` reads a config file, and `SettingsResolver.Resolve` combines it with overrides into the same settings the CLI uses. `L3LintFactory.Create(settings)` builds a linter from them, including local packs and the lock file check.
