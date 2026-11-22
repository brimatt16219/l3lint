using L3Lint.Core.Input;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Tests;

/// <summary>
/// The pack loader refuses anything it cannot trust or compile, with the file and pointer in the error
/// (clarification 14, ADR-0024, ADR-0038), and every declarative check kind works through the engine.
/// </summary>
public sealed class RulePackLoaderTests
{
    private const string Payload = """
        { "schemaVersion": "1.0", "id": "A-1", "amount": 30.00, "currency": "USD", "card": { "brand": "visa", "product": "purchasing" },
          "merchant": { "mcc": "5047", "country": "US" }, "tax": { "amount": 2.10, "rate": 0.07 },
          "lineItems": [ { "sequence": 1, "description": "Gloves", "quantity": 3, "unitCost": 10.00, "unitOfMeasure": "BX", "total": 30.00 } ] }
        """;

    [Fact]
    public void Range_check_reports_values_outside_the_bounds()
    {
        using var pack = new TestPack(TestPack.Pack(
            TestPack.Rule("GEN-T-001", "{ kind: range, field: /tax/rate, min: 0.10, max: 0.20 }", "Rate {actual} is outside {min} to {max}."),
            TestPack.Rule("GEN-T-002", "{ kind: range, field: /lineItems/*/quantity, max: 3, exclusive: true }", "Line {index} quantity is {actual}.")));
        var linter = pack.Build();

        var result = linter.Lint(RawDocument.ParseJson(Payload, "p.json"));

        var rate = result.Findings.Single(f => f.RuleId == "GEN-T-001");
        rate.Message.ShouldBe("Rate 0.07 is outside 0.10 to 0.20.");
        rate.Path.Canonical.ShouldBe("/tax/rate");
        var quantity = result.Findings.Single(f => f.RuleId == "GEN-T-002");
        quantity.Message.ShouldBe("Line 1 quantity is 3.");
        linter.Rules().Single(r => r.Id == "GEN-T-001").Check.Kind.ShouldBe("range");
    }

    [Fact]
    public void Pattern_any_of_ratio_and_max_count_checks_work()
    {
        using var pack = new TestPack(TestPack.Pack(
            TestPack.Rule("GEN-T-010", "{ kind: pattern, field: /lineItems/*/unitOfMeasure, regex: '^[A-Z]{3}$' }"),
            TestPack.Rule("GEN-T-011", "{ kind: anyOf, fields: [/customer/reference, /order/purchaseOrderNumber] }"),
            TestPack.Rule("GEN-T-012", "{ kind: ratio, numerator: /tax/amount, denominator: /amount, min: 0.10, max: 0.20 }"),
            TestPack.Rule("GEN-T-013", "{ kind: maxCount, collection: /lineItems, max: 0 }")));

        var result = pack.Build().Lint(RawDocument.ParseJson(Payload, "p.json"));

        result.Findings.Select(f => f.RuleId).Order(StringComparer.Ordinal).ShouldBe(["GEN-T-010", "GEN-T-011", "GEN-T-012", "GEN-T-013"]);
    }

    [Fact]
    public void Lists_and_in_list_checks_load_from_csv()
    {
        var yaml = TestPack.Header + "\n  lists:\n    units: lists/units.csv\nrules:\n"
            + TestPack.Rule("GEN-T-020", "{ kind: inList, field: /lineItems/*/unitOfMeasure, list: units }", "Unit {actual} is not listed.");
        using var pack = new TestPack(yaml).With("lists/units.csv", "# test units\ncode\nEA\nCS\n");

        var result = pack.Build().Lint(RawDocument.ParseJson(Payload, "p.json"));

        result.Findings.ShouldHaveSingleItem().Message.ShouldBe("Unit BX is not listed.");
    }

    [Fact]
    public void Missing_manifest_is_refused()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));

        Should.Throw<RulePackException>(() => TestPack.Linter(pack.Write(manifest: false))).Message.ShouldContain("manifest");
    }

    [Fact]
    public void Changed_file_fails_the_checksum()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));

        var error = Should.Throw<RulePackException>(() => TestPack.Linter(pack.Write(h => h["test.rules.yaml"] = new string('0', 64))));

        error.File.ShouldBe("test.rules.yaml");
    }

    [Fact]
    public void Unlisted_and_missing_files_are_refused()
    {
        using var extra = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }"))).With("notes.txt", "hello");
        Should.Throw<RulePackException>(() => TestPack.Linter(extra.Write(h => h.Remove("notes.txt")))).File.ShouldBe("notes.txt");

        using var missing = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));
        Should.Throw<RulePackException>(() => TestPack.Linter(missing.Write(h => h["gone.csv"] = new string('a', 64)))).Message.ShouldContain("gone.csv");
    }

    [Fact]
    public void Corrupt_manifest_is_refused()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));
        pack.Write();
        File.WriteAllText(Path.Combine(pack.Folder, "manifest.json"), "{ not json");

        Should.Throw<RulePackException>(() => TestPack.Linter(new DirectoryRulePackSource(pack.Folder)));
    }

    [Theory]
    [InlineData("rules: [unclosed", "test.rules.yaml")]
    [InlineData("pack: { id: test.rules.general }\nrules: []", "test.rules.yaml")]
    public void Yaml_and_schema_errors_name_the_file(string yaml, string file)
    {
        using var pack = new TestPack(yaml);

        Should.Throw<RulePackException>(() => pack.Build()).File.ShouldBe(file);
    }

    [Fact]
    public void Header_must_match_the_manifest()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")).Replace("version: 1.0.0", "version: 1.0.1", StringComparison.Ordinal));

        Should.Throw<RulePackException>(() => pack.Build()).Message.ShouldContain("1.0.1");
    }

    [Theory]
    [InlineData("VISA-T-001", "{ kind: required, field: /id }", "Problem.", "GEN")]
    [InlineData("GEN-T-001", "{ kind: custom, check: NoSuchCheck }", "Problem.", "NoSuchCheck")]
    [InlineData("GEN-T-001", "{ kind: inList, field: /currency, list: noSuchList }", "Problem.", "noSuchList")]
    [InlineData("GEN-T-001", "{ kind: pattern, field: /id, regex: '([a-z' }", "Problem.", "([a-z")]
    [InlineData("GEN-T-001", "{ kind: maxCount, collection: /amounts, max: 2 }", "Problem.", "/amounts")]
    [InlineData("GEN-T-001", "{ kind: required, field: /id }", "Problem with {nonsense}.", "nonsense")]
    public void Rules_that_cannot_compile_are_refused_with_the_reason(string id, string check, string message, string expected)
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule(id, check, message)));

        var error = Should.Throw<RulePackException>(() => pack.Build());

        error.Message.ShouldContain(expected);
    }

    [Fact]
    public void Missing_list_and_example_files_are_refused()
    {
        var withList = TestPack.Header + "\n  lists:\n    units: lists/missing.csv\nrules:\n" + TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }");
        using var list = new TestPack(withList);
        Should.Throw<RulePackException>(() => list.Build()).Message.ShouldContain("lists/missing.csv");

        var withExample = TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }", extra: "    examples: { pass: examples/p.json, fail: examples/f.json }\n"));
        using var example = new TestPack(withExample);
        Should.Throw<RulePackException>(() => example.Build()).Message.ShouldContain("examples/");
    }

    [Fact]
    public void Duplicate_rule_ids_across_packs_are_refused()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));

        var builder = new LinterBuilder().AddRulePack(pack.Write()).AddRule(new DuplicateRule());

        Should.Throw<RulePackException>(() => builder.Build()).Message.ShouldContain("GEN-T-001");
    }

    [Fact]
    public void Required_pack_versions_are_enforced()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));

        Should.Throw<RulePackException>(() => new LinterBuilder().AddRulePack(pack.Write()).RequirePackVersions(new Dictionary<string, string> { ["test.rules.general"] = "2.0.0" }).Build());
        Should.Throw<RulePackException>(() => new LinterBuilder().AddRulePack(pack.Write()).RequirePackVersions(new Dictionary<string, string> { ["other.pack"] = "1.0.0" }).Build());
        new LinterBuilder().AddRulePack(pack.Write()).RequirePackVersions(new Dictionary<string, string> { ["test.rules.general"] = "1.0.0" }).Build().RulePacks.ShouldHaveSingleItem().Version.ShouldBe("1.0.0");
    }

    [Fact]
    public void Directory_source_never_opens_files_outside_its_folder()
    {
        using var pack = new TestPack(TestPack.Pack(TestPack.Rule("GEN-T-001", "{ kind: required, field: /id }")));
        var source = pack.Write();

        Should.Throw<FileNotFoundException>(() => source.Open("../outside.txt"));
        Should.Throw<DirectoryNotFoundException>(() => new DirectoryRulePackSource(Path.Combine(pack.Folder, "nope")));
    }

    private sealed class DuplicateRule : IRule
    {
        public RuleMetadata Metadata { get; } = new()
        {
            Id = "GEN-T-001",
            Title = "Duplicate",
            Level = RuleLevel.General,
            Severity = Severity.Warning,
            Confidence = Confidence.Policy,
            Impact = Impact.Hygiene,
            Message = "Duplicate.",
            Fix = "None.",
            Sources = [new Uri("https://example.org/")],
            PackId = "custom",
            PackVersion = "1.0.0",
            Check = new RuleCheck("custom", [], []),
        };

        public bool AppliesTo(in RuleContext context) => false;

        public void Evaluate(in RuleContext context, IFindingSink sink) => throw new InvalidOperationException("A rule that never applies is never evaluated.");
    }
}
