using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Hosting;
using ModelContextProtocol.Client;

namespace L3Lint.AgentEval;

/// <summary>The outcome of one fixture.</summary>
internal sealed record AttemptResult(SeededFixture Fixture, bool Fixed, int ToolCalls, string Note);

/// <summary>All outcomes of one run.</summary>
internal sealed record EvalReport(IReadOnlyList<AttemptResult> Results)
{
    public int Attempted => Results.Count;

    public int Fixed => Results.Count(r => r.Fixed);

    public double Rate => Attempted == 0 ? 0 : Fixed / (double)Attempted;

    public string ToMarkdown(EvalOptions options)
    {
        var text = new StringBuilder();
        text.Append("# Agent evaluation\n\n");
        text.Append(string.Create(CultureInfo.InvariantCulture, $"Model {options.Model}. Fixed {Fixed} of {Attempted} seeded defects within two MCP tool calls ({Rate:P1}); target 90%.\n\n"));
        text.Append("| Operator | Attempted | Fixed |\n| --- | --- | --- |\n");
        foreach (var group in Results.GroupBy(r => r.Fixture.Operator, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            text.Append(string.Create(CultureInfo.InvariantCulture, $"| {group.Key} | {group.Count()} | {group.Count(r => r.Fixed)} |\n"));
        }

        text.Append("\n| Fixture | Fixed | Tool calls | Note |\n| --- | --- | --- | --- |\n");
        foreach (var result in Results)
        {
            text.Append(string.Create(CultureInfo.InvariantCulture, $"| {result.Fixture.Name} | {(result.Fixed ? "yes" : "no")} | {result.ToolCalls} | {result.Note.Replace("|", "/", StringComparison.Ordinal)} |\n"));
        }

        return text.ToString();
    }
}

/// <summary>
/// Gives an agent each seeded-defect payload and the l3lint MCP server's tools, allows at most two tool calls, then
/// lints the payload the agent returns. A defect counts as fixed when none of the fixture's expected findings remain.
/// </summary>
internal static partial class Evaluation
{
    private const int MaxToolCalls = 2;
    private const int MaxTurns = 6;

    private const string SystemPrompt = """
        You repair commercial card payment payloads so they meet Visa, Mastercard, American Express and Discover
        Level 2 and Level 3 requirements. You have the l3lint tools. You may make at most two tool calls in total, so
        plan them: lint_payload shows what is wrong and suggest_fixes gives safe mechanical patches. Fix every error
        the tools report, keeping the arithmetic consistent (line total = quantity x unit cost - discount; amount =
        line totals + tax + freight + duty - header discount). When a fix needs a value the payload lacks, choose a
        realistic one. Finish with the complete corrected payload as one JSON object in a ```json code block.
        """;

    public static async Task<EvalReport> RunAsync(EvalOptions options, TextWriter log, CancellationToken cancellationToken)
    {
        var root = SeededFixture.FindRoot();
        var fixtures = SeededFixture.Load(root, options.Limit);
        var server = Path.Combine(AppContext.BaseDirectory, "l3lint-mcp.dll");
        var transport = new StdioClientTransport(new StdioClientTransportOptions { Name = "l3lint", Command = "dotnet", Arguments = [server] });
        await using var mcp = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
        var mcpTools = await mcp.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        List<BetaToolUnion> tools = [.. mcpTools.Select(ToClaudeTool)];
        var claude = new AnthropicClient();
        var linter = L3LintFactory.Create(new ResolvedSettings { Options = LintOptions.Default });

        var results = new List<AttemptResult>();
        foreach (var fixture in fixtures)
        {
            var result = await AttemptAsync(fixture, claude, mcp, tools, linter, root, options, cancellationToken).ConfigureAwait(false);
            log.WriteLine($"{fixture.Name}: {(result.Fixed ? "fixed" : "not fixed")} ({result.ToolCalls} tool calls) {result.Note}");
            results.Add(result);
        }

        return new EvalReport(results);
    }

    private static async Task<AttemptResult> AttemptAsync(SeededFixture fixture, AnthropicClient claude, McpClient mcp, List<BetaToolUnion> tools, ILinter linter, string root, EvalOptions options, CancellationToken cancellationToken)
    {
        var context = string.Join(", ", fixture.Context.Select(p => p.Key + "=" + p.Value));
        List<BetaMessageParam> messages =
        [
            new BetaUserMessageParam { Content = $"Repair this canonical payload. Lint context: {context}.\n\n```json\n{fixture.Payload}\n```" },
        ];
        var toolCalls = 0;
        string? finalText = null;
        for (var turn = 0; turn < MaxTurns && finalText is null; turn++)
        {
            // Refusal fallbacks are on by default: a declined request is re-served by a fallback model in the same call.
            var response = await claude.Beta.Messages.Create(
                new MessageCreateParams
                {
                    Model = options.Model,
                    MaxTokens = 16000,
                    System = SystemPrompt,
                    Tools = tools,
                    Messages = messages,
                    Betas = ["server-side-fallback-2026-06-01"],
                    Fallbacks = new List<BetaFallbackParam> { new(Anthropic.Models.Messages.Model.ClaudeOpus4_8) },
                },
                cancellationToken).ConfigureAwait(false);
            if (response.StopReason == "refusal")
            {
                return new AttemptResult(fixture, false, toolCalls, "the model declined");
            }

            List<BetaContentBlockParam> assistant = [];
            List<BetaContentBlockParam> toolResults = [];
            var text = new StringBuilder();
            foreach (var block in response.Content)
            {
                if (block.TryPickText(out var textBlock))
                {
                    assistant.Add(new BetaTextBlockParam { Text = textBlock.Text });
                    text.Append(textBlock.Text);
                }
                else if (block.TryPickThinking(out var thinking))
                {
                    assistant.Add(new BetaThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
                }
                else if (block.TryPickRedactedThinking(out var redacted))
                {
                    assistant.Add(new BetaRedactedThinkingBlockParam { Data = redacted.Data });
                }
                else if (block.TryPickToolUse(out var toolUse))
                {
                    assistant.Add(new BetaToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input });
                    toolResults.Add(await CallToolAsync(mcp, toolUse, ++toolCalls, cancellationToken).ConfigureAwait(false));
                }
            }

            messages.Add(new BetaAssistantMessageParam { Content = assistant });
            if (toolResults.Count == 0)
            {
                finalText = text.ToString();
            }
            else
            {
                messages.Add(new BetaUserMessageParam { Content = toolResults });
            }
        }

        if (finalText is null || LastJsonBlock().Matches(finalText).LastOrDefault() is not { } match)
        {
            return new AttemptResult(fixture, false, Math.Min(toolCalls, MaxToolCalls), "no corrected payload in the answer");
        }

        return Judge(fixture, match.Groups["json"].Value, linter, root, Math.Min(toolCalls, MaxToolCalls));
    }

    // Calls beyond the limit are answered with an error so the agent must finish with what it has.
    private static async Task<BetaContentBlockParam> CallToolAsync(McpClient mcp, BetaToolUseBlock toolUse, int callNumber, CancellationToken cancellationToken)
    {
        if (callNumber > MaxToolCalls)
        {
            return new BetaToolResultBlockParam { ToolUseID = toolUse.ID, Content = "The two-call limit is used up. Answer now with the corrected payload.", IsError = true };
        }

        var arguments = toolUse.Input.ToDictionary(p => p.Key, p => (object?)p.Value, StringComparer.Ordinal);
        var result = await mcp.CallToolAsync(toolUse.Name, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        var text = result.StructuredContent is { } structured
            ? structured.GetRawText()
            : string.Concat(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(t => t.Text));
        return new BetaToolResultBlockParam { ToolUseID = toolUse.ID, Content = text, IsError = result.IsError == true };
    }

    private static AttemptResult Judge(SeededFixture fixture, string json, ILinter linter, string root, int toolCalls)
    {
        LintResult linted;
        try
        {
            var overrides = new LintOverrides
            {
                Brand = fixture.Context.GetValueOrDefault("brand"),
                CardProduct = fixture.Context.GetValueOrDefault("cardProduct"),
                Mcc = fixture.Context.GetValueOrDefault("mcc"),
                Processor = fixture.Context.GetValueOrDefault("processor"),
                AsOf = fixture.Context.TryGetValue("asOf", out var asOf) ? DateOnly.ParseExact(asOf, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
            };
            var options = SettingsResolver.Resolve(L3LintConfiguration.Default, overrides, root).Options;
            linted = linter.Lint(RawDocument.ParseJson(json, fixture.Name), options);
        }
        catch (InputException ex)
        {
            return new AttemptResult(fixture, false, toolCalls, "the answer is not a valid payload: " + ex.Message);
        }

        var remaining = fixture.Defects.Where(d => linted.Findings.Any(f => f.RuleId == d.RuleId && f.Path.Canonical == d.Path)).ToList();
        return remaining.Count == 0
            ? new AttemptResult(fixture, true, toolCalls, string.Empty)
            : new AttemptResult(fixture, false, toolCalls, "still reported: " + string.Join(", ", remaining.Select(d => d.RuleId + " " + d.Path)));
    }

    // The MCP tool's JSON Schema becomes the Claude tool's input schema unchanged.
    private static BetaToolUnion ToClaudeTool(McpClientTool tool)
    {
        var schema = tool.ProtocolTool.InputSchema;
        var properties = schema.TryGetProperty("properties", out var p)
            ? p.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.Ordinal)
            : [];
        List<string> required = schema.TryGetProperty("required", out var r) ? [.. r.EnumerateArray().Select(x => x.GetString()!)] : [];
        return new BetaTool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new() { Properties = properties, Required = required },
        };
    }

    [GeneratedRegex("```json\\s*(?<json>\\{.*?\\})\\s*```", RegexOptions.Singleline | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LastJsonBlock();
}
