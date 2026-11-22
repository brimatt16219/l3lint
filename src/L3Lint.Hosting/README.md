# L3Lint.Hosting

l3lint for .NET services. One package brings the engine, the five rule packs (general, Visa, Mastercard, American Express, Discover), the gateway and CSV adapters and the output writers, plus `AddL3Lint()` registration, `.l3lint.yaml` loading and the batch runner.

```text
dotnet add package L3Lint.Hosting
```

```csharp
builder.Services.AddL3Lint(options => options.Processor = "generic");
```

`ILinter` is a thread-safe singleton. After startup it does no I/O. Findings are advisory: they say a payload is likely to downgrade or meets the documented requirement.

Documentation: https://brimatt16219.github.io/l3lint/articles/library.html
