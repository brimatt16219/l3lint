using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace L3Lint.Mcp.Tests;

/// <summary>Resource templates and prompts (Interfaces tab, "Resources and prompts").</summary>
public sealed class ResourceAndPromptTests(McpServerFixture server)
{
    [Fact]
    public async Task Three_resource_templates_are_listed()
    {
        var templates = await server.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken);

        templates.Select(t => t.UriTemplate).Order(StringComparer.Ordinal)
            .ShouldBe(["l3lint://examples/{brand}/{name}", "l3lint://rules/{ruleId}", "l3lint://schemas/{name}"]);
    }

    [Fact]
    public async Task Rule_resource_is_the_markdown_reference_page()
    {
        var text = await ReadAsync("l3lint://rules/VISA-L3-004");

        text.ShouldStartWith("# VISA-L3-004: ");
    }

    [Fact]
    public async Task Schema_resource_is_the_json_schema()
    {
        var text = await ReadAsync("l3lint://schemas/canonical");

        using var schema = JsonDocument.Parse(text);
        schema.RootElement.GetProperty("$id").GetString().ShouldEndWith("/canonical.schema.json");
    }

    [Theory]
    [InlineData("visa")]
    [InlineData("mastercard")]
    [InlineData("amex")]
    [InlineData("discover")]
    public async Task Each_brand_example_lints_without_errors(string brand)
    {
        var text = await ReadAsync($"l3lint://examples/{brand}/level3");

        // The samples are the brand base payloads, which meet every documented requirement as of 2026-10-19.
        var result = await server.Client.CallToolAsync("lint_payload", new Dictionary<string, object?> { ["payload"] = text, ["context"] = new Dictionary<string, object?> { ["asOf"] = "2026-10-19" } }, cancellationToken: TestContext.Current.CancellationToken);
        var structured = result.StructuredContent!.Value;
        structured.GetProperty("transactions")[0].GetProperty("brand").GetString().ShouldBe(brand);
        structured.GetProperty("summary").GetProperty("errors").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_resources_are_errors()
    {
        await Should.ThrowAsync<Exception>(() => ReadAsync("l3lint://rules/NOPE-1"));
        await Should.ThrowAsync<Exception>(() => ReadAsync("l3lint://examples/jcb/level3"));
    }

    [Fact]
    public async Task Two_prompts_are_listed_and_embed_the_payload()
    {
        var prompts = await server.Client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken);
        prompts.Select(p => p.Name).Order(StringComparer.Ordinal).ShouldBe(["explain_downgrade", "review_payload"]);

        var review = await server.Client.GetPromptAsync("review_payload", new Dictionary<string, object?> { ["payload"] = "{\"id\":\"A-1\"}" }, cancellationToken: TestContext.Current.CancellationToken);
        var reviewText = string.Concat(review.Messages.Select(m => m.Content).OfType<TextContentBlock>().Select(t => t.Text));
        reviewText.ShouldContain("lint_payload");
        reviewText.ShouldContain("suggest_fixes");
        reviewText.ShouldContain("{\"id\":\"A-1\"}");

        var downgrade = await server.Client.GetPromptAsync("explain_downgrade", new Dictionary<string, object?> { ["payload"] = "{}", ["category"] = "Commercial Standard" }, cancellationToken: TestContext.Current.CancellationToken);
        string.Concat(downgrade.Messages.Select(m => m.Content).OfType<TextContentBlock>().Select(t => t.Text)).ShouldContain("Commercial Standard");
    }

    private async Task<string> ReadAsync(string uri)
    {
        var result = await server.Client.ReadResourceAsync(uri, cancellationToken: TestContext.Current.CancellationToken);
        return string.Concat(result.Contents.OfType<TextResourceContents>().Select(c => c.Text));
    }
}
