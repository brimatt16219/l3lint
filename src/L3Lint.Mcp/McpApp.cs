using System.Globalization;
using System.Net;
using L3Lint.Core;
using L3Lint.Hosting;
using L3Lint.Mcp.Content;
using L3Lint.Mcp.Tools;
using ModelContextProtocol.Protocol;

namespace L3Lint.Mcp;

/// <summary>Builds and runs the server over stdio or Streamable HTTP.</summary>
internal static class McpApp
{
    /// <summary>The HTTP path the MCP endpoint is served on.</summary>
    public const string HttpPath = "/mcp";

    public static async Task<int> RunAsync(string[] args)
    {
        ServerSettings settings;
        try
        {
            settings = ServerSettings.Parse(args, Environment.GetEnvironmentVariable);
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            await Console.Error.WriteLineAsync(Resources.Strings.Usage).ConfigureAwait(false);
            return 2;
        }

        if (settings.ShowHelp)
        {
            await Console.Out.WriteLineAsync(Resources.Strings.Usage).ConfigureAwait(false);
            return 0;
        }

        if (settings.ShowVersion)
        {
            await Console.Out.WriteLineAsync("l3lint-mcp " + EngineInfo.Version).ConfigureAwait(false);
            return 0;
        }

        if (settings.Http)
        {
            await using var app = BuildHttp(settings);
            await app.RunAsync().ConfigureAwait(false);
            return 0;
        }

        var builder = Host.CreateApplicationBuilder([]);
        ConfigureLogging(builder.Logging);
        AddServer(builder.Services, settings).WithStdioServerTransport();
        using var host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
        return 0;
    }

    /// <summary>Builds the HTTP server bound to 127.0.0.1. Port 0 picks a free port (used by tests).</summary>
    public static WebApplication BuildHttp(ServerSettings settings)
    {
        var builder = WebApplication.CreateSlimBuilder([]);
        ConfigureLogging(builder.Logging);
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.AddServerHeader = false;
            kestrel.Listen(IPAddress.Loopback, settings.Port);
        });

        // The server keeps no session state, so it works with and without protocol-level sessions.
        AddServer(builder.Services, settings).WithHttpTransport(http => http.Stateless = true);
        var app = builder.Build();
        app.UseMiddleware<LocalRequestGuard>(settings);
        app.MapMcp(HttpPath);
        return app;
    }

    private static IMcpServerBuilder AddServer(IServiceCollection services, ServerSettings settings)
    {
        services.AddSingleton(settings);
        services.AddL3Lint();
        return services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "l3lint",
                    Title = "l3lint",
                    Version = EngineInfo.Version,
                    WebsiteUrl = EngineInfo.DocsUrl.ToString(),
                };
                options.ServerInstructions = Resources.Strings.ServerInstructions;
            })
            .WithTools<LintTools>()
            .WithResources<RuleResources>()
            .WithPrompts([typeof(ReviewPrompts)]);
    }

    // stdout carries JSON-RPC over stdio, so every log line goes to stderr (Interfaces tab, "Transports").
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole(console => console.LogToStandardErrorThreshold = LogLevel.Trace);
        logging.SetMinimumLevel(LogLevel.Warning);
    }

    /// <summary>Formats the endpoint URL for a port.</summary>
    public static Uri Endpoint(int port) => new(string.Format(CultureInfo.InvariantCulture, "http://127.0.0.1:{0}{1}", port, HttpPath));
}
