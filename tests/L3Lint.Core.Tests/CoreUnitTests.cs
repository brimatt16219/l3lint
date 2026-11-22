using System.Text;
using System.Text.Json.Nodes;
using L3Lint.Core.Engine;
using L3Lint.Core.Fixes;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Yaml;

namespace L3Lint.Core.Tests;

/// <summary>RFC 6902 operations used by <c>fix</c> (FR-027): add, replace and remove only.</summary>
public sealed class JsonPatchTests
{
    private static readonly JsonNode Document = JsonNode.Parse("""{ "a": { "b": 1 }, "list": [10, 20, 30] }""")!;

    [Fact]
    public void Add_replace_and_remove_on_objects_and_arrays()
    {
        var patched = JsonPatch.Apply(Document,
        [
            new PatchOperation("add", "/a/c", JsonValue.Create(2)),
            new PatchOperation("replace", "/a/b", JsonValue.Create(5)),
            new PatchOperation("add", "/list/1", JsonValue.Create(15)),
            new PatchOperation("add", "/list/-", JsonValue.Create(40)),
            new PatchOperation("replace", "/list/0", JsonValue.Create(11)),
            new PatchOperation("remove", "/list/2", null),
            new PatchOperation("remove", "/a/c", null),
        ]);

        patched.ToJsonString().ShouldBe("""{"a":{"b":5},"list":[11,15,30,40]}""");
        Document.ToJsonString().ShouldBe("""{"a":{"b":1},"list":[10,20,30]}""");
    }

    [Fact]
    public void Root_can_be_replaced()
    {
        JsonPatch.Apply(Document, [new PatchOperation("replace", string.Empty, JsonValue.Create(1))]).ToJsonString().ShouldBe("1");
    }

    [Theory]
    [InlineData("replace", "/a/missing")]
    [InlineData("remove", "/a/missing")]
    [InlineData("add", "/missing/child")]
    [InlineData("add", "/list/9")]
    [InlineData("remove", "/list/x")]
    [InlineData("copy", "/a/b")]
    [InlineData("remove", "")]
    public void Operations_that_do_not_fit_the_document_are_refused(string op, string path)
    {
        Should.Throw<ArgumentException>(() => JsonPatch.Apply(Document, [new PatchOperation(op, path, op == "remove" ? null : JsonValue.Create(1))]));
    }
}

/// <summary>YAML 1.2 core schema reading for packs, config, profiles and rate tables (NFR-006 hardening).</summary>
public sealed class YamlJsonTests
{
    [Fact]
    public void Scalars_follow_the_core_schema()
    {
        var node = YamlJson.Parse("""
            t: true
            f: False
            n: ~
            e:
            i: -42
            d: 1.50
            x: 1e3
            q: "true"
            s: 'plain'
            date: 2026-10-19
            code: 0042x
            """)!;

        node.ToJsonString().ShouldBe("""{"t":true,"f":false,"n":null,"e":null,"i":-42,"d":1.50,"x":1000,"q":"true","s":"plain","date":"2026-10-19","code":"0042x"}""");
    }

    [Theory]
    [InlineData("a: &a [*a]\n")]
    [InlineData("&a {k: *a}\n")]
    [InlineData("x: &p [&q [*p]]\n")]
    public void An_alias_that_loops_back_to_its_parent_is_refused_not_followed(string yaml)
    {
        Should.Throw<YamlFormatException>(() => YamlJson.Parse(yaml));
    }

    [Fact]
    public void A_string_tag_keeps_a_plain_scalar_as_text()
    {
        // YAML 1.2: !!str and the non-specific tag ! both make the scalar a string.
        YamlJson.Parse("mcc: !!str 5047\nflag: ! true\nn: 5047\n")!.ToJsonString().ShouldBe("""{"mcc":"5047","flag":"true","n":5047}""");
    }

    [Fact]
    public void Nesting_is_limited_before_the_tree_is_built()
    {
        var limit = new string('[', YamlJson.MaxDepth) + new string(']', YamlJson.MaxDepth);
        var tooDeep = "[" + limit + "]";

        YamlJson.Parse(limit).ShouldNotBeNull();
        Should.Throw<YamlFormatException>(() => YamlJson.Parse(tooDeep)).Line.ShouldBe(1);
    }

    [Fact]
    public void Empty_text_is_null_and_sequences_work()
    {
        YamlJson.Parse(string.Empty).ShouldBeNull();
        YamlJson.Parse("- 1\n- [a, b]\n")!.ToJsonString().ShouldBe("""[1,["a","b"]]""");
    }

    [Theory]
    [InlineData("a: 1\na: 2\n", 2)]
    [InlineData("a: 1\n---\nb: 2\n", 3)]
    [InlineData("a: !!python/object:os.system x\n", 1)]
    [InlineData("a: [1, 2\n", 2)]
    public void Refused_or_broken_yaml_reports_the_line(string yaml, int line)
    {
        Should.Throw<YamlFormatException>(() => YamlJson.Parse(yaml)).Line.ShouldBe(line);
    }

    [Fact]
    public void Alias_expansion_has_a_budget()
    {
        // The "billion laughs" shape: each level references the previous one ten times.
        var builder = new StringBuilder("a0: &a0 [x, x, x, x, x, x, x, x, x, x]\n");
        for (var i = 1; i < 9; i++)
        {
            builder.Append('a').Append(i).Append(": &a").Append(i).Append(" [");
            builder.Append(string.Join(", ", Enumerable.Repeat("*a" + (i - 1), 10)));
            builder.Append("]\n");
        }

        Should.Throw<YamlFormatException>(() => YamlJson.Parse(builder.ToString()));
    }
}

/// <summary>Canonical JSON names for every enumeration value round-trip (canonical schema v1).</summary>
public sealed class CanonicalNamesTests
{
    [Fact]
    public void Every_value_round_trips()
    {
        foreach (var value in Enum.GetValues<CardBrand>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out CardBrand parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }

        foreach (var value in Enum.GetValues<CardProduct>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out CardProduct parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }

        foreach (var value in Enum.GetValues<TaxIndicator>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out TaxIndicator parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }

        foreach (var value in Enum.GetValues<DebitCredit>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out DebitCredit parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }

        foreach (var value in Enum.GetValues<Channel>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out Channel parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }

        foreach (var value in Enum.GetValues<CommodityScheme>())
        {
            CanonicalNames.TryParse(CanonicalNames.ToName(value), out CommodityScheme parsed).ShouldBeTrue();
            parsed.ShouldBe(value);
        }
    }

    [Fact]
    public void Unknown_names_are_not_parsed()
    {
        CanonicalNames.TryParse("jcb", out CardBrand _).ShouldBeFalse();
        CanonicalNames.TryParse("Visa", out CardBrand _).ShouldBeFalse();
        CanonicalNames.TryParse(null, out CardProduct _).ShouldBeFalse();
        CanonicalNames.TryParse("zero", out TaxIndicator _).ShouldBeFalse();
        CanonicalNames.TryParse("refund", out DebitCredit _).ShouldBeFalse();
        CanonicalNames.TryParse("moto", out Channel _).ShouldBeFalse();
        CanonicalNames.TryParse("cpv", out CommodityScheme _).ShouldBeFalse();
    }

    [Fact]
    public void Wire_names_cover_every_value()
    {
        Enum.GetValues<Severity>().Select(WireNames.Of).ShouldBe(["information", "warning", "error"], ignoreOrder: true);
        Enum.GetValues<Confidence>().Select(WireNames.Of).ShouldBe(["public-brand", "processor-doc", "inferred", "policy"], ignoreOrder: true);
        Enum.GetValues<Impact>().Select(WireNames.Of).Distinct().Count().ShouldBe(Enum.GetValues<Impact>().Length);
        Enum.GetValues<Rules.RuleLevel>().Select(WireNames.Of).ShouldBe(["general", "L2", "L3"], ignoreOrder: true);
        Enum.GetValues<Tiers.TierStatus>().Select(WireNames.Of).Distinct().Count().ShouldBe(Enum.GetValues<Tiers.TierStatus>().Length);
        Enum.GetValues<Adapters.AdapterNoteKind>().Select(WireNames.Of).ShouldBe(["ignored", "ambiguous", "derived", "invalid", "defaulted"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("error", Severity.Error)]
    [InlineData("WARN", Severity.Warning)]
    [InlineData("Information", Severity.Information)]
    [InlineData("info", Severity.Information)]
    public void Severity_parses_case_insensitively(string text, Severity expected)
    {
        WireNames.TryParseSeverity(text, out var severity).ShouldBeTrue();
        severity.ShouldBe(expected);
        WireNames.TryParseSeverity("fatal", out _).ShouldBeFalse();
        WireNames.TryParseSeverity(null, out _).ShouldBeFalse();
    }
}

/// <summary>Context precedence: flags beat the payload, which beats config defaults (ADR-0042).</summary>
public sealed class ContextOverridesTests
{
    private static readonly Transaction Sent = new()
    {
        Card = new CardInfo { Brand = CardBrand.Visa, Product = CardProduct.Purchasing },
        Merchant = new MerchantInfo { Mcc = "5047", Country = "US" },
    };

    [Fact]
    public void Flags_override_the_payload_with_a_note_each()
    {
        var notes = new List<string>();
        var options = new LintOptions { Brand = CardBrand.Mastercard, CardProduct = CardProduct.Corporate, Mcc = "5999", MerchantCountry = "CA" };

        var effective = ContextOverrides.Apply(Sent, options, notes);

        effective.Card.Brand.ShouldBe(CardBrand.Mastercard);
        effective.Card.Product.ShouldBe(CardProduct.Corporate);
        effective.Merchant.Mcc.ShouldBe("5999");
        effective.Merchant.Country.ShouldBe("CA");
        notes.Count.ShouldBe(4);
        notes[0].ShouldBe("Card brand set to mastercard by option; the payload said visa.");
    }

    [Fact]
    public void Defaults_fill_only_what_the_payload_left_out()
    {
        var notes = new List<string>();
        var options = new LintOptions { Defaults = new ContextDefaults { Brand = CardBrand.Amex, CardProduct = CardProduct.Fleet, Mcc = "4111", MerchantCountry = "GB" } };

        var filled = ContextOverrides.Apply(new Transaction(), options, notes);
        var kept = ContextOverrides.Apply(Sent, options, notes);

        filled.Card.Brand.ShouldBe(CardBrand.Amex);
        filled.Card.Product.ShouldBe(CardProduct.Fleet);
        filled.Merchant.Mcc.ShouldBe("4111");
        filled.Merchant.Country.ShouldBe("GB");
        kept.ShouldBeSameAs(Sent);
        notes.ShouldBeEmpty();
    }

    [Fact]
    public void Merchant_country_defaults_to_us_and_silent_payloads_get_no_note()
    {
        var notes = new List<string>();

        var effective = ContextOverrides.Apply(new Transaction(), new LintOptions { CardProduct = CardProduct.Business }, notes);

        effective.Merchant.Country.ShouldBe(ContextOverrides.DefaultMerchantCountry);
        effective.Card.Product.ShouldBe(CardProduct.Business);
        notes.ShouldBeEmpty();
    }

    [Fact]
    public void All_brands_leaves_the_brand_alone()
    {
        var effective = ContextOverrides.Apply(Sent, new LintOptions { AllBrands = true, Brand = CardBrand.Discover }, []);

        effective.Card.Brand.ShouldBe(CardBrand.Visa);
    }
}

/// <summary>Input parsing errors carry a kind, the file and a position (ADR-0037, FR-034).</summary>
public sealed class RawDocumentTests
{
    [Theory]
    [InlineData("{ \"a\": 1, \"a\": 2 }", InputErrorKind.DuplicateProperty)]
    [InlineData("{ \"a\": [1, 2 }", InputErrorKind.Malformed)]
    [InlineData("[1, 2]", InputErrorKind.NotAnObject)]
    [InlineData("{ \"a\": 1, }", InputErrorKind.Malformed)]
    [InlineData("{ /* note */ \"a\": 1 }", InputErrorKind.Malformed)]
    public void Bad_json_is_classified(string json, InputErrorKind kind)
    {
        Should.Throw<InputException>(() => RawDocument.ParseJson(json, "p.json")).Kind.ShouldBe(kind);
    }

    [Fact]
    public void Depth_and_size_limits_apply()
    {
        var deep = new string('[', 10) + new string(']', 10);
        var limits = InputLimits.Default with { MaxJsonDepth = 5, MaxFileBytes = 64 };

        Should.Throw<InputException>(() => RawDocument.ParseJson(Encoding.UTF8.GetBytes("{\"a\":" + deep + "}"), "p.json", limits)).Kind.ShouldBe(InputErrorKind.TooDeep);
        Should.Throw<InputException>(() => RawDocument.ParseJson(new byte[65], "p.json", limits)).Kind.ShouldBe(InputErrorKind.TooLarge);
        Should.Throw<InputException>(() => RawDocument.FromCsv(new byte[65], "p.csv", limits)).Kind.ShouldBe(InputErrorKind.TooLarge);
    }

    [Fact]
    public void Error_lines_count_from_the_first_line_of_an_ndjson_record()
    {
        var error = Should.Throw<InputException>(() => RawDocument.ParseJson(Encoding.UTF8.GetBytes("{ \"a\": }"), "batch.ndjson", null, firstLine: 7));

        error.Line.ShouldBe(7);
        error.SourceName.ShouldBe("batch.ndjson");
    }

    [Fact]
    public void Byte_order_mark_is_skipped_and_positions_are_found()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("{\n  \"id\": \"A\",\n  \"lineItems\": [\n    { \"total\": 1 }\n  ]\n}")).ToArray();

        var document = RawDocument.ParseJson(bytes, "p.json");

        document.GetText().ShouldStartWith("{");
        document.Locate("/id").ShouldBe(new SourceLocation(2, 9));
        document.Locate("/lineItems/0/total").ShouldBe(new SourceLocation(4, 16));
        document.Locate("/lineItems/0/missing")!.Value.Line.ShouldBe(4);
        document.Locate(null).ShouldBeNull();
        RawDocument.FromCsv(Encoding.UTF8.GetBytes("a,b\n"), "p.csv").Locate("/a").ShouldBeNull();
    }
}
