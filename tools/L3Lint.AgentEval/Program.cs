using System.Globalization;
using L3Lint.AgentEval;

// Usage: L3Lint.AgentEval [--limit N] [--model ID] [--output report.md]
// Measures how many seeded defects an AI agent fixes within two calls to the l3lint MCP server tools
// (Test and delivery tab: target 90%). It needs Claude API credentials and spends API tokens, so it runs only when
// credentials are configured; without them it explains what it needs and exits 0 without sending anything.
var options = EvalOptions.Parse(args);
if (options is null)
{
    Console.Error.WriteLine(EvalOptions.Usage);
    return 2;
}

if (!Credentials.Configured(Environment.GetEnvironmentVariable))
{
    Console.WriteLine("No Claude API credentials found (ANTHROPIC_API_KEY, ANTHROPIC_AUTH_TOKEN or ANTHROPIC_PROFILE). Nothing was sent.");
    Console.WriteLine("Set one of them to run the agent evaluation; each run spends API tokens.");
    return 0;
}

var report = await Evaluation.RunAsync(options, Console.Out, CancellationToken.None).ConfigureAwait(false);
if (options.Output is { } path)
{
    await File.WriteAllTextAsync(path, report.ToMarkdown(options), CancellationToken.None).ConfigureAwait(false);
}

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Fixed {report.Fixed} of {report.Attempted} seeded defects within two tool calls ({report.Rate:P1}); target 90%."));
return 0;
