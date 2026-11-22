using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Hosting;

namespace L3Lint.Security.Tests;

/// <summary>
/// Hostile JSON input (Architecture tab, threat "Hostile input": 25 MB file cap, depth 64, 1,000 lines per
/// transaction, streaming NDJSON; NFR-006; FR-034; ADR-0048 for the per-record NDJSON limit).
/// </summary>
public sealed class JsonHardeningTests
{
    private static readonly ILinter Linter = L3LintFactory.CreateBuilder().Build();

    [Fact]
    public void Depth_64_is_accepted_and_depth_65_is_refused()
    {
        // The root object is depth 1, so an object holding n nested arrays has depth n + 1.
        RawDocument.ParseJson(Nested(63), "depth-64.json").Json.ValueKind.ShouldBe(JsonValueKind.Object);

        var exception = Should.Throw<InputException>(() => RawDocument.ParseJson(Nested(64), "depth-65.json"));

        exception.Kind.ShouldBe(InputErrorKind.TooDeep);
    }

    [Fact]
    public void Very_deep_nesting_is_refused_without_recursion()
    {
        var exception = Should.Throw<InputException>(() => RawDocument.ParseJson(Nested(200_000), "very-deep.json"));

        exception.Kind.ShouldBe(InputErrorKind.TooDeep);
    }

    [Theory]
    [InlineData("""{ "amount": 1, "amount": 2 }""")]
    [InlineData("""{ "merchant": { "name": "A", "name": "B" } }""")]
    [InlineData("""{ "lineItems": [ { "total": 1, "total": 2 } ] }""")]
    [InlineData("{ \"amount\": 1, \"\\" + "u0061mount\": 2 }")]
    public void Duplicate_properties_are_refused(string json)
    {
        // Two values for one field would let a reviewer and a processor read different amounts.
        var exception = Should.Throw<InputException>(() => RawDocument.ParseJson(json, "duplicates.json"));

        exception.Kind.ShouldBe(InputErrorKind.DuplicateProperty);
    }

    [Fact]
    public void A_document_over_the_size_limit_is_refused_before_parsing()
    {
        var limits = InputLimits.Default with { MaxFileBytes = 1000 };
        var body = Encoding.UTF8.GetBytes("{\"merchant\":{\"name\":\"" + new string('x', 1000) + "\"}}");

        var exception = Should.Throw<InputException>(() => RawDocument.ParseJson(body, "big.json", limits));

        exception.Kind.ShouldBe(InputErrorKind.TooLarge);
    }

    [Fact]
    public void The_default_limits_are_the_spec_values()
    {
        // Architecture tab: "25 MB file cap, depth 64, 1,000 lines per transaction".
        InputLimits.Default.MaxFileBytes.ShouldBe(25L * 1024 * 1024);
        InputLimits.Default.MaxJsonDepth.ShouldBe(64);
        InputLimits.Default.MaxLineItems.ShouldBe(1000);
    }

    [Fact]
    public void More_than_1000_line_items_is_an_input_error_not_a_lint()
    {
        var payload = SecretPayloads.Mutate(p =>
        {
            var lines = p["lineItems"]!.AsArray();
            var template = lines[0]!.ToJsonString();
            while (lines.Count < 1001)
            {
                lines.Add(JsonNode.Parse(template));
            }
        });

        var result = Linter.Lint(RawDocument.ParseJson(payload, "many-lines.json"));

        result.InputErrors.ShouldHaveSingleItem().Kind.ShouldBe(InputErrorKind.TooManyLineItems);
        result.Findings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    [InlineData("null")]
    public void Only_an_object_is_a_payload(string json)
    {
        Should.Throw<InputException>(() => RawDocument.ParseJson(json, "not-object.json")).Kind.ShouldBe(InputErrorKind.NotAnObject);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF })]
    [InlineData(new byte[] { 0xC0, 0x80 })]
    [InlineData(new byte[] { 0xED, 0xA0, 0x80 })]
    public void Invalid_UTF8_in_a_string_is_an_input_error(byte[] badBytes)
    {
        // A file with broken UTF-8 is malformed input (exit 2), whichever stage first reads the string.
        var body = Encoding.UTF8.GetBytes("{\"schemaVersion\":\"1.0\",\"merchant\":{\"name\":\"A").Concat(badBytes).Concat(Encoding.UTF8.GetBytes("\"}}")).ToArray();

        Should.Throw<InputException>(() =>
        {
            var document = RawDocument.ParseJson(body, "bad-utf8.json");
            Linter.Lint(document);
        });
    }

    private static string Nested(int arrays) => "{\"a\":" + new string('[', arrays) + new string(']', arrays) + "}";
}

/// <summary>Size limits through the CLI, where the user meets them.</summary>
[Collection(ConsoleGroup.Name)]
public sealed class CliLimitTests : IDisposable
{
    private readonly ConsoleCli _cli = new();

    [Fact]
    public async Task A_file_one_byte_over_25_MB_is_a_usage_error()
    {
        var path = _cli.PathOf("big.json");
        var prefix = "{\"merchant\":{\"name\":\"";
        var suffix = "\"}}";
        var filler = (int)InputLimits.DefaultMaxFileBytes + 1 - prefix.Length - suffix.Length;
        await File.WriteAllTextAsync(path, prefix + new string('x', filler) + suffix, TestContext.Current.CancellationToken);
        new FileInfo(path).Length.ShouldBe(InputLimits.DefaultMaxFileBytes + 1);

        var run = await _cli.RunAsync("lint", path, "--output", "json", "--no-color");

        run.ExitCode.ShouldBe(2);
        run.Out.ShouldNotContain("xxxxxxxxxx");
    }

    [Fact]
    public async Task The_size_limit_applies_to_each_NDJSON_record_and_the_rest_still_lints()
    {
        // ADR-0048: for .ndjson files the limit is per record; an oversized record is an input error for its line.
        _cli.Write(".l3lint.yaml", "inputs:\n  limits:\n    maxFileBytes: 2000\n");
        var small = SecretPayloads.OneLine(SecretPayloads.SpecExample);
        var large = SecretPayloads.OneLine(SecretPayloads.Mutate(p => p["merchant"]!["name"] = new string('x', 3000)));
        small.Length.ShouldBeLessThan(2000);
        var batch = _cli.Write("batch.ndjson", small + "\n" + large + "\n" + small + "\n");

        var run = await _cli.RunAsync("lint", batch, "--output", "json", "--no-color");

        run.ExitCode.ShouldBe(2);
        using var report = JsonDocument.Parse(run.Out);
        report.RootElement.GetProperty("transactions").GetArrayLength().ShouldBe(2);
        var error = report.RootElement.GetProperty("inputErrors").EnumerateArray().ShouldHaveSingleItem();
        error.GetProperty("line").ValueKind.ShouldBe(JsonValueKind.Number, "ADR-0048: the oversized record is reported for its line, so the error must name line 2.");
        error.GetProperty("line").GetInt32().ShouldBe(2);
        run.Out.ShouldNotContain(new string('x', 100));
    }

    public void Dispose() => _cli.Dispose();
}
