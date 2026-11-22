using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

[assembly: AssemblyFixture(typeof(L3Lint.Mcp.Tests.McpServerFixture))]

namespace L3Lint.Mcp.Tests;

/// <summary>
/// One in-process HTTP server on a free loopback port, shared by the tests, with an SDK client connected to it.
/// </summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public McpClient Client { get; private set; } = null!;

    public Uri Endpoint { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        (_app, Endpoint) = await StartAsync(new ServerSettings { Http = true, Port = 0 });
        Client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions { Endpoint = Endpoint }));
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>Starts a server with the given settings and returns it with its endpoint.</summary>
    internal static async Task<(WebApplication App, Uri Endpoint)> StartAsync(ServerSettings settings)
    {
        var app = McpApp.BuildHttp(settings);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return (app, new Uri(new Uri(address), McpApp.HttpPath));
    }
}
