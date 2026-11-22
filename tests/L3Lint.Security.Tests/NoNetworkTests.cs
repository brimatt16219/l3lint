using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace L3Lint.Security.Tests;

/// <summary>
/// The CLI touches no network while it runs (FR-035, NFR-005; Architecture tab, threat "Data leaving the machine").
/// </summary>
/// <remarks>
/// The spec's authoritative check runs the CLI with outbound network blocked in CI (a Linux network namespace,
/// workflow work). This portable probe runs on every operating system: while the CLI lints, fixes and converts in
/// this process, it records every networking assembly that loads and every connect, DNS and HTTP event the runtime
/// raises. Architecture.Tests checks the same rule statically on the compiled types.
/// </remarks>
[Collection(ConsoleGroup.Name)]
public sealed class NoNetworkTests : IDisposable
{
    private static readonly string[] NetworkAssemblies =
        ["System.Net.Http", "System.Net.Sockets", "System.Net.NameResolution", "System.Net.Security", "System.Net.Requests", "System.Net.WebClient", "System.Net.Quic"];

    private readonly ConsoleCli _cli = new();

    [Fact]
    public async Task Lint_fix_and_convert_open_no_connection()
    {
        var payload = _cli.Write("payload.json", SecretPayloads.WithCards);
        var batch = _cli.Write("batch.ndjson", SecretPayloads.OneLine(SecretPayloads.Distinctive) + "\n" + SecretPayloads.OneLine(SecretPayloads.SpecExample) + "\n");
        var loaded = new ConcurrentQueue<string>();
        void OnLoad(object? sender, AssemblyLoadEventArgs e) => loaded.Enqueue(e.LoadedAssembly.GetName().Name ?? string.Empty);

        using var listener = new NetworkEventListener();
        AppDomain.CurrentDomain.AssemblyLoad += OnLoad;
        CliRun[] runs;
        try
        {
            runs =
            [
                await _cli.RunAsync("lint", batch, "--output", "json", "--output", "sarif=" + _cli.PathOf("out.sarif"), "--log-level", "trace", "--no-color"),
                await _cli.RunAsync("fix", payload, "--write", _cli.PathOf("fixed.json")),
                await _cli.RunAsync("convert", payload, "--to", "canonical"),
                await _cli.RunAsync("rules", "list"),
                await _cli.RunAsync("explain", "VISA-L3-004"),
            ];
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnLoad;
        }

        runs.Select(r => r.ExitCode).ShouldAllBe(code => code == 0 || code == 1);
        loaded.Where(name => NetworkAssemblies.Contains(name, StringComparer.Ordinal)).ShouldBeEmpty("The CLI loaded a networking assembly.");
        listener.Events.ShouldBeEmpty("The runtime reported network activity while the CLI ran.");
    }

    public void Dispose() => _cli.Dispose();

    /// <summary>Records connect, DNS, TLS and HTTP events from the runtime's networking event sources.</summary>
    private sealed class NetworkEventListener : EventListener
    {
        private static readonly string[] Sources = ["System.Net.Http", "System.Net.Sockets", "System.Net.NameResolution", "System.Net.Security", "System.Net.Quic"];

        public ConcurrentQueue<string> Events { get; } = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (Sources.Contains(eventSource.Name, StringComparer.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var payload = string.Join(", ", eventData.Payload ?? []);

            // The test host may talk to its own runner over a local (Unix domain) socket; that is not the CLI.
            if (payload.StartsWith("Unix", StringComparison.Ordinal) || eventData.EventName is null)
            {
                return;
            }

            Events.Enqueue($"{eventData.EventSource.Name}/{eventData.EventName}: {payload}");
        }
    }
}
