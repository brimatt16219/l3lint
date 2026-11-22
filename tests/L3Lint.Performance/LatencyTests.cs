using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Hosting;
using ModelContextProtocol.Client;

namespace L3Lint.Performance;

/// <summary>
/// Latency targets from the user stories. The thresholds are the spec's; BenchmarkDotNet gives the detailed numbers
/// (docs/dev/benchmarks.md).
/// </summary>
public sealed class LatencyTests
{
    private static readonly string SpecExample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    [Fact]
    public void Ten_line_payload_lints_in_under_a_millisecond_when_warm()
    {
        // US-09: "A 10-line payload lints in under 1 ms on a warm process."
        var linter = L3LintFactory.Create(new ResolvedSettings { Options = LintOptions.Default });
        var spec = linter.Map(RawDocument.ParseJson(SpecExample, "spec.json")).Transactions[0].Transaction;
        var transaction = spec with
        {
            LineItems = [.. Enumerable.Range(1, 10).Select(i => spec.LineItems[0] with { Sequence = i })],
            Amount = 1215.00m + 17.36m,
        };
        for (var i = 0; i < 500; i++)
        {
            linter.Lint(transaction);
        }

        // Median of 1,001 calls, so one garbage collection cannot decide the result.
        var times = new List<double>(1001);
        for (var i = 0; i < 1001; i++)
        {
            var watch = Stopwatch.StartNew();
            linter.Lint(transaction);
            times.Add(watch.Elapsed.TotalMilliseconds);
        }

        times.Order().ElementAt(500).ShouldBeLessThan(1.0);
    }

    [Fact]
    public async Task Lint_payload_with_100_lines_returns_within_200_ms()
    {
        // US-08: "lint_payload returns within 200 ms for 100 line items", measured through a real MCP client over stdio.
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "l3lint",
            Command = "dotnet",
            Arguments = [Path.Combine(AppContext.BaseDirectory, "l3lint-mcp.dll")],
        });
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
        var payload = HundredLines();
        var arguments = new Dictionary<string, object?> { ["payload"] = payload };
        for (var i = 0; i < 20; i++)
        {
            await client.CallToolAsync("lint_payload", arguments, cancellationToken: TestContext.Current.CancellationToken);
        }

        var times = new List<double>(21);
        for (var i = 0; i < 21; i++)
        {
            var watch = Stopwatch.StartNew();
            var result = await client.CallToolAsync("lint_payload", arguments, cancellationToken: TestContext.Current.CancellationToken);
            times.Add(watch.Elapsed.TotalMilliseconds);
            result.IsError.ShouldNotBe(true);
        }

        times.Order().ElementAt(10).ShouldBeLessThan(200);
    }

    private static JsonElement HundredLines()
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        var line = root["lineItems"]![0]!;
        var lines = new JsonArray();
        for (var i = 0; i < 100; i++)
        {
            var copy = line.DeepClone();
            copy["sequence"] = i + 1;
            lines.Add(copy);
        }

        root["lineItems"] = lines;
        using var document = JsonDocument.Parse(root.ToJsonString());
        return document.RootElement.Clone();
    }
}
