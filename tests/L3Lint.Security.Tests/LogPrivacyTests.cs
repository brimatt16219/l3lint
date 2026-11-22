using System.Text.Json;

namespace L3Lint.Security.Tests;

/// <summary>
/// Logs carry rule IDs, paths and counts, never field values, unless <c>--log-values</c> is set (FR-033, NFR-005;
/// Architecture tab, threat "Payload data leaking through logs").
/// </summary>
[Collection(ConsoleGroup.Name)]
public sealed class LogPrivacyTests : IDisposable
{
    private readonly ConsoleCli _cli = new();

    public static TheoryData<string, string> Commands() => new()
    {
        { "lint", "text" },
        { "lint", "json" },
        { "fix", "text" },
        { "fix", "json" },
        { "convert", "text" },
        { "convert", "json" },
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Trace_logs_carry_no_payload_values(string command, string logFormat)
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);
        var batch = _cli.Write("batch.ndjson", SecretPayloads.OneLine(SecretPayloads.Distinctive) + "\n" + SecretPayloads.OneLine(SecretPayloads.WithCards) + "\n");
        var input = command == "lint" ? batch : payload;

        var run = await _cli.RunAsync([.. Arguments(command, input), "--log-level", "trace", "--log-format", logFormat, "--no-color"]);

        run.ExitCode.ShouldBeOneOf(0, 1);
        var logs = LogLines(run.Error, logFormat);
        if (command == "lint")
        {
            // Positive control: lint logs each input it reads (event 1001, debug), so the scan sees real log lines.
            // fix and convert log nothing today, which meets the rule trivially; their standard error still must not
            // echo a value.
            logs.ShouldNotBeEmpty($"No log lines were captured, so the scan proves nothing. Standard error:\n{run.Error}");
        }

        foreach (var value in SecretValues())
        {
            run.Error.ShouldNotContain(value, Case.Sensitive, $"{command} with --log-format {logFormat} wrote a payload value to standard error.");
        }

        foreach (var card in SecretPayloads.Cards)
        {
            // Without --log-values even the masked form stays out of the logs: logs carry paths, not values.
            string.Join("\n", logs).ShouldNotContain(card.Masked);
        }
    }

    [Fact]
    public async Task Log_values_is_the_only_way_to_get_values_into_logs_and_they_are_masked()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);

        var run = await _cli.RunAsync("lint", payload, "--output", "json", "--log-level", "trace", "--log-format", "json", "--log-values", "--no-color");

        // A warning says values will be logged (Architecture tab: "a warning prints when it is set").
        run.Error.ShouldContain("--log-values");
        var logs = string.Join("\n", LogLines(run.Error, "json"));
        logs.ShouldContain(SecretPayloads.Visa.Masked);
        foreach (var raw in SecretPayloads.Cards.SelectMany(c => c.RawForms))
        {
            run.Error.ShouldNotContain(raw);
        }
    }

    public void Dispose() => _cli.Dispose();

    private static string[] Arguments(string command, string input) => command switch
    {
        "lint" => ["lint", input, "--output", "json"],
        "fix" => ["fix", input, "--dry-run"],
        _ => ["convert", input, "--to", "canonical"],
    };

    private static IEnumerable<string> SecretValues() =>
        [SecretPayloads.MerchantName, SecretPayloads.CustomerReference, SecretPayloads.Description, .. SecretPayloads.Cards.SelectMany(c => c.RawForms)];

    /// <summary>
    /// The lines the logger wrote: JSON objects with an EventId for <c>--log-format json</c>, and lines that start
    /// with a level tag such as <c>info:</c> or <c>trce:</c> for the simple console format.
    /// </summary>
    private static List<string> LogLines(string error, string format) =>
        [.. error.Split('\n').Where(line => format == "json" ? IsJsonLogLine(line) : IsTextLogLine(line))];

    private static bool IsTextLogLine(string line) =>
        line.Length > 5 && line[4] == ':' && line[..4] is "trce" or "dbug" or "info" or "warn" or "fail" or "crit";

    private static bool IsJsonLogLine(string line)
    {
        if (!line.StartsWith('{'))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.TryGetProperty("EventId", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
