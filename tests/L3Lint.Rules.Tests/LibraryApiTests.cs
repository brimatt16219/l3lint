using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Hosting;
using L3Lint.Hosting.Runs;
using Microsoft.Extensions.DependencyInjection;

namespace L3Lint.Rules.Tests;

/// <summary>
/// The library API (Interfaces tab "Library API", US-09, FR-018, FR-031): DI registration, thread safety,
/// warm latency, extension points and fault isolation.
/// </summary>
public sealed class LibraryApiTests
{
    private static readonly string SpecExample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    private static Transaction SpecTransaction => Linters.Packs.Map(RawDocument.ParseJson(SpecExample, "spec.json")).Transactions[0].Transaction;

    [Fact]
    public void Dependency_injection_registers_a_singleton_linter_and_runner()
    {
        using var provider = new ServiceCollection().AddL3Lint(o => o.Processor = "generic").BuildServiceProvider();

        var first = provider.GetRequiredService<ILinter>();
        first.ShouldBeSameAs(provider.GetRequiredService<ILinter>());
        provider.GetRequiredService<LintRunner>().ShouldNotBeNull();
        first.RulePacks.Count.ShouldBe(5);
        first.Lint(SpecTransaction).Summary.Warnings.ShouldBe(1);
    }

    [Fact]
    public void Lint_of_an_in_memory_transaction_matches_the_document_path()
    {
        var fromObject = Linters.Packs.Lint(SpecTransaction);
        var fromDocument = Linters.Packs.Lint(RawDocument.ParseJson(SpecExample, "spec.json"));

        fromObject.Findings.Select(f => f.RuleId).ShouldBe(fromDocument.Findings.Select(f => f.RuleId));
        fromObject.Format.ShouldBe("canonical");
        fromObject.InputSha256.Length.ShouldBe(64);
    }

    [Fact]
    public async Task Linter_is_thread_safe()
    {
        var expected = JsonSerializer.Serialize(Linters.Packs.Lint(SpecTransaction).Findings.Select(f => f.RuleId + f.Path.Canonical + f.Message));

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            Enumerable.Range(0, 100).Select(_ => JsonSerializer.Serialize(Linters.Packs.Lint(SpecTransaction).Findings.Select(f => f.RuleId + f.Path.Canonical + f.Message))).ToList())));

        results.SelectMany(r => r).ShouldAllBe(r => r == expected);
    }

    [Fact]
    public async Task Lint_many_streams_results_and_ends_with_the_batch_result()
    {
        // Twenty identical transactions reach GEN-DQ-007's default minimum batch size (Interfaces tab config table: 20, 0.80).
        var transactions = Enumerable.Range(0, 20).Select(i => SpecTransaction with { Id = "INV-" + i }).ToAsyncEnumerable();

        var results = new List<TransactionResult>();
        await foreach (var result in Linters.Packs.LintManyAsync(transactions, cancellationToken: TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        results.Count(r => r.Kind == TransactionResultKind.Transaction).ShouldBe(20);
        results[^1].Kind.ShouldBe(TransactionResultKind.Batch);
        results[^1].Findings.ShouldContain(f => f.RuleId == "GEN-DQ-007");
    }

    [Fact]
    public void Explain_and_rule_lookup()
    {
        Linters.Packs.Explain("visa-l3-004").Id.ShouldBe("VISA-L3-004");
        Should.Throw<KeyNotFoundException>(() => Linters.Packs.Explain("VISA-L3-999"));
        Linters.Packs.TryGetRule("L3LINT-ENGINE-001", out var engine).ShouldBeTrue();
        engine!.Title.ShouldNotBeNullOrWhiteSpace();
        Linters.Packs.Rules(new RuleQuery { Brand = CardBrand.Amex }).Count.ShouldBe(9);
    }

    [Fact]
    public void Unknown_processor_profile_is_an_argument_error()
    {
        Should.Throw<ArgumentException>(() => Linters.Packs.Lint(SpecTransaction, new LintOptions { Processor = "no-such-processor" }));
    }

    [Fact]
    public void Too_many_line_items_is_an_input_error()
    {
        var lines = string.Join(",", Enumerable.Range(1, 6).Select(i => "{ \"sequence\": " + i + ", \"description\": \"Gloves\", \"quantity\": 1, \"unitCost\": 1.00, \"total\": 1.00 }"));
        var document = RawDocument.ParseJson("{ \"schemaVersion\": \"1.0\", \"id\": \"A\", \"amount\": 6.00, \"currency\": \"USD\", \"lineItems\": [" + lines + "] }", "many.json");

        var result = Linters.Packs.Lint(document, new LintOptions { Limits = InputLimits.Default with { MaxLineItems = 5 } });

        result.InputErrors.ShouldHaveSingleItem().Kind.ShouldBe(InputErrorKind.TooManyLineItems);
        result.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public void A_failing_rule_is_isolated_and_reported_as_an_engine_finding()
    {
        var linter = new LinterBuilder()
            .AddRulePack(new General.GeneralRulePack())
            .AddRule(new ThrowingRule())
            .UseTimeProvider(Linters.Clock)
            .Build();

        var result = linter.Lint(SpecTransaction);

        result.HasInternalError.ShouldBeTrue();
        result.Findings.ShouldContain(f => f.RuleId == "L3LINT-ENGINE-001" && f.Message.Contains("GEN-TEST-001", StringComparison.Ordinal));
    }

    [Fact]
    public void Custom_adapters_and_checks_plug_in()
    {
        var linter = new LinterBuilder()
            .AddRulePack(new General.GeneralRulePack())
            .AddAdapter(new AcmeAdapter())
            .AddCustomCheck(new NeverCheck())
            .UseTimeProvider(Linters.Clock)
            .Build();

        var result = linter.Lint(RawDocument.ParseJson("{ \"acme\": { \"invoice\": \"X-1\" } }", "acme.json"));

        result.Format.ShouldBe("acme");
        linter.Adapters.Adapters.Select(a => a.Name).ShouldContain("acme");
        Should.Throw<ArgumentException>(() => new LinterBuilder().AddAdapter(new AcmeAdapter()).AddAdapter(new AcmeAdapter()).Build());
    }

    private sealed class ThrowingRule : IRule
    {
        public RuleMetadata Metadata { get; } = new()
        {
            Id = "GEN-TEST-001",
            Title = "Always throws",
            Level = RuleLevel.General,
            Severity = Severity.Warning,
            Confidence = Confidence.Policy,
            Impact = Impact.Hygiene,
            Message = "Never shown.",
            Fix = "None.",
            Sources = [new Uri("https://example.org/")],
            PackId = "test",
            PackVersion = "1.0.0",
            Check = new RuleCheck("custom", [], []),
        };

        public bool AppliesTo(in RuleContext context) => true;

        public void Evaluate(in RuleContext context, IFindingSink sink) => throw new InvalidOperationException("Deliberate failure for the isolation test.");
    }

    private sealed class NeverCheck : ICustomCheck
    {
        public string Name => "Never";

        public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
        {
            // Registered to prove a custom check can be added; no rule references it, so it never runs.
            throw new InvalidOperationException("No rule references this check.");
        }
    }

    private sealed class AcmeAdapter : IPayloadAdapter
    {
        public string Name => "acme";

        public InputKind Kind => InputKind.Json;

        public double Detect(RawDocument document) => document.Json.TryGetProperty("acme", out _) ? 1.0 : 0.0;

        public IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options) =>
            [new AdapterResult(new Transaction { Id = document.Json.GetProperty("acme").GetProperty("invoice").GetString() }, PathMap.Identity)];

        public string? ToCanonicalPointer(string sourcePointer) => sourcePointer;
    }
}
