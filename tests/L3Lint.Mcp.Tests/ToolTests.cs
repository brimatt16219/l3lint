using System.Text.Json;
using Json.Schema;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace L3Lint.Mcp.Tests;

/// <summary>The six tools over Streamable HTTP with the SDK client (Interfaces tab "MCP server", FR-029, US-08).</summary>
public sealed class ToolTests(McpServerFixture server)
{
    private McpClient Client => server.Client;

    [Fact]
    public async Task Six_read_only_closed_world_tools_are_listed()
    {
        var tools = await Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        tools.Select(t => t.Name).Order(StringComparer.Ordinal).ShouldBe(["explain_rule", "get_schema", "lint_payload", "list_rules", "map_payload", "suggest_fixes"]);
        foreach (var tool in tools)
        {
            var annotations = tool.ProtocolTool.Annotations.ShouldNotBeNull(tool.Name);
            annotations.ReadOnlyHint.ShouldBe(true, tool.Name);
            annotations.IdempotentHint.ShouldBe(true, tool.Name);
            annotations.OpenWorldHint.ShouldBe(false, tool.Name);
            tool.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Lint_payload_returns_the_results_document_and_a_text_summary()
    {
        var result = await CallAsync("lint_payload", new() { ["payload"] = Payloads.SpecExample });

        result.IsError.ShouldNotBe(true);
        var structured = result.StructuredContent.ShouldNotBeNull();
        Validate("results", structured);
        var findings = structured.GetProperty("transactions")[0].GetProperty("findings");
        findings.GetArrayLength().ShouldBe(1);
        findings[0].GetProperty("ruleId").GetString().ShouldBe("VISA-L3-010");
        Text(result).ShouldContain("1 transaction, 0 errors, 1 warning.");
    }

    [Fact]
    public async Task Lint_payload_accepts_a_json_string()
    {
        var result = await CallAsync("lint_payload", new() { ["payload"] = Payloads.SpecExampleText });

        result.IsError.ShouldNotBe(true);
        result.StructuredContent!.Value.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Lint_payload_context_beats_the_payload()
    {
        var result = await CallAsync("lint_payload", new()
        {
            ["payload"] = Payloads.SpecExample,
            ["context"] = new Dictionary<string, object?> { ["brand"] = "mastercard" },
        });

        // The spec example's BX and CA units are not on the Mastercard list (Card brand rules tab, MC-L3-005).
        var findings = result.StructuredContent!.Value.GetProperty("transactions")[0].GetProperty("findings").EnumerateArray().ToList();
        findings.Count(f => f.GetProperty("ruleId").GetString() == "MC-L3-005").ShouldBe(2);
        result.StructuredContent!.Value.GetProperty("exitCode").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Lint_payload_honors_fail_on_and_max_findings()
    {
        var result = await CallAsync("lint_payload", new()
        {
            ["payload"] = Payloads.Mutate(p => p["card"]!["brand"] = "mastercard"),
            ["failOn"] = "none",
            ["maxFindings"] = 1,
        });

        var structured = result.StructuredContent!.Value;
        structured.GetProperty("exitCode").GetInt32().ShouldBe(0);
        structured.GetProperty("transactions")[0].GetProperty("findings").GetArrayLength().ShouldBe(1);
        structured.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2, 3]")]
    public async Task Bad_payloads_are_tool_errors_the_model_can_read(string payload)
    {
        var result = await CallAsync("lint_payload", new() { ["payload"] = payload });

        result.IsError.ShouldBe(true);
        Text(result).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Payload_over_two_megabytes_is_rejected()
    {
        var big = Payloads.Mutate(p => p["merchant"]!["name"] = new string('x', 2 * 1024 * 1024));

        var result = await CallAsync("lint_payload", new() { ["payload"] = big });

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("2 MB");
    }

    [Theory]
    [InlineData("format", "paypal")]
    [InlineData("format", "csv")]
    [InlineData("failOn", "fatal")]
    public async Task Bad_arguments_are_tool_errors(string name, string value)
    {
        var result = await CallAsync("lint_payload", new() { ["payload"] = Payloads.SpecExample, [name] = value });

        result.IsError.ShouldBe(true);
    }

    [Fact]
    public async Task Processor_must_be_a_built_in_profile_so_no_file_is_read()
    {
        var result = await CallAsync("lint_payload", new()
        {
            ["payload"] = Payloads.SpecExample,
            ["context"] = new Dictionary<string, object?> { ["processor"] = "../../secrets.yaml" },
        });

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("generic");
    }

    [Fact]
    public async Task Explain_rule_returns_metadata_and_examples()
    {
        var result = await CallAsync("explain_rule", new() { ["ruleId"] = "VISA-L3-004" });

        result.IsError.ShouldNotBe(true);
        var rule = result.StructuredContent!.Value;
        rule.GetProperty("id").GetString().ShouldBe("VISA-L3-004");
        rule.GetProperty("confidence").GetString().ShouldBe("processor-doc");
        Text(result).ShouldStartWith("VISA-L3-004: ");
    }

    [Fact]
    public async Task Explain_unknown_rule_tells_the_model_what_to_do()
    {
        var result = await CallAsync("explain_rule", new() { ["ruleId"] = "VISA-L3-999" });

        result.IsError.ShouldBe(true);
        Text(result).ShouldContain("list_rules");
    }

    [Fact]
    public async Task List_rules_filters()
    {
        var all = await CallAsync("list_rules", []);
        var discover = await CallAsync("list_rules", new() { ["brand"] = "discover" });
        var bad = await CallAsync("list_rules", new() { ["level"] = "L4" });

        all.StructuredContent!.Value.GetProperty("rules").GetArrayLength().ShouldBe(78);
        discover.StructuredContent!.Value.GetProperty("rules").EnumerateArray().Select(r => r.GetProperty("id").GetString())
            .ShouldBe(["DISC-ELIG-001", "DISC-FMT-001", "DISC-L2-001"]);
        bad.IsError.ShouldBe(true);
    }

    [Fact]
    public async Task Map_payload_returns_canonical_json_and_the_path_map()
    {
        var result = await CallAsync("map_payload", new() { ["payload"] = Payloads.SpecExample });

        var mapping = result.StructuredContent!.Value;
        mapping.GetProperty("format").GetString().ShouldBe("canonical");
        var transaction = mapping.GetProperty("transactions")[0];
        transaction.GetProperty("canonical").GetProperty("id").GetString().ShouldBe("INV-20417");
        transaction.GetProperty("pathMap").GetProperty("/amount").GetString().ShouldBe("/amount");
    }

    [Fact]
    public async Task Map_payload_masks_card_numbers()
    {
        var payload = Payloads.Mutate(p => p["customer"]!["reference"] = "4111 1111 1111 1111");

        var result = await CallAsync("map_payload", new() { ["payload"] = payload });

        var json = result.StructuredContent!.Value.GetRawText();
        json.ShouldNotContain("4111 1111 1111 1111");
        json.ShouldNotContain("4111111111111111");
        result.StructuredContent!.Value.GetProperty("cardNumbersMasked").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Suggest_fixes_patches_the_line_total_but_never_the_amount()
    {
        var result = await CallAsync("suggest_fixes", new() { ["payload"] = Payloads.WrongLineTotal });

        var fixes = result.StructuredContent!.Value;
        var total = fixes.GetProperty("patch").EnumerateArray().Single(o => o.GetProperty("path").GetString() == "/lineItems/0/total");
        total.GetProperty("op").GetString().ShouldBe("add");
        total.GetProperty("value").GetDecimal().ShouldBe(120.00m);
        fixes.GetProperty("patch").EnumerateArray().ShouldNotContain(o => o.GetProperty("path").GetString() == "/amount");
        fixes.GetProperty("targetsCanonical").GetBoolean().ShouldBeFalse();
        fixes.GetProperty("needsHuman").EnumerateArray().ShouldContain(f => f.GetProperty("ruleId").GetString() == "GEN-ARITH-002");
    }

    [Theory]
    [InlineData("canonical")]
    [InlineData("results")]
    [InlineData("config")]
    [InlineData("rulepack")]
    public async Task Get_schema_returns_each_schema(string name)
    {
        var result = await CallAsync("get_schema", new() { ["name"] = name });

        result.StructuredContent!.Value.GetProperty("$id").GetString().ShouldEndWith("/" + name + ".schema.json");
        (await CallAsync("get_schema", new() { ["name"] = "sarif" })).IsError.ShouldBe(true);
    }

    private async Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?> arguments) =>
        await Client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);

    private static string Text(CallToolResult result) => string.Concat(result.Content.OfType<TextContentBlock>().Select(t => t.Text));

    private static void Validate(string schemaName, JsonElement instance)
    {
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Schemas", schemaName + ".schema.json")), new BuildOptions { SchemaRegistry = new SchemaRegistry() });
        var evaluation = schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });
        evaluation.IsValid.ShouldBeTrue(string.Join("; ", (evaluation.Details ?? []).Where(d => d.Errors is not null).SelectMany(d => d.Errors!.Select(e => d.InstanceLocation + " " + e.Value))));
    }
}
