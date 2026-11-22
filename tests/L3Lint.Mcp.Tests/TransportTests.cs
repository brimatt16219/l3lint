using System.Net;
using System.Text;
using ModelContextProtocol.Client;

namespace L3Lint.Mcp.Tests;

/// <summary>
/// stdio and HTTP transports and the HTTP safeguards: loopback Host only, Origin check, optional bearer token
/// (Interfaces tab "Transports", Architecture tab threat table).
/// </summary>
public sealed class TransportTests
{
    private const string Initialize = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}""";

    [Fact]
    public async Task Stdio_server_lists_tools_and_lints()
    {
        var server = Path.Combine(AppContext.BaseDirectory, "l3lint-mcp.dll");
        File.Exists(server).ShouldBeTrue(server);
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "l3lint",
            Command = "dotnet",
            Arguments = [server],
        });

        await using var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await client.CallToolAsync("lint_payload", new Dictionary<string, object?> { ["payload"] = Payloads.SpecExample }, cancellationToken: TestContext.Current.CancellationToken);

        tools.Count.ShouldBe(6);
        client.ServerInfo.Name.ShouldBe("l3lint");
        result.StructuredContent!.Value.GetProperty("summary").GetProperty("warnings").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Every_tool_answers_over_stdio()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "l3lint",
            Command = "dotnet",
            Arguments = [Path.Combine(AppContext.BaseDirectory, "l3lint-mcp.dll")],
        });
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
        var calls = new Dictionary<string, Dictionary<string, object?>>(StringComparer.Ordinal)
        {
            ["lint_payload"] = new() { ["payload"] = Payloads.SpecExample },
            ["explain_rule"] = new() { ["ruleId"] = "VISA-L3-004" },
            ["list_rules"] = new() { ["brand"] = "visa" },
            ["map_payload"] = new() { ["payload"] = Payloads.SpecExample },
            ["suggest_fixes"] = new() { ["payload"] = Payloads.WrongLineTotal },
            ["get_schema"] = new() { ["name"] = "canonical" },
        };

        foreach (var (tool, arguments) in calls)
        {
            var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);
            result.IsError.ShouldNotBe(true, tool);
            result.StructuredContent.ShouldNotBeNull(tool);
        }

        (await client.ListResourceTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Count.ShouldBe(3);
        (await client.ListPromptsAsync(cancellationToken: TestContext.Current.CancellationToken)).Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("http://evil.example")]
    [InlineData("https://localhost.evil.example")]
    [InlineData("null")]
    public async Task Foreign_browser_origins_are_rejected(string origin)
    {
        var (app, endpoint) = await McpServerFixture.StartAsync(new ServerSettings { Http = true, Port = 0 });
        await using var _ = app;
        using var http = new HttpClient();

        using var response = await http.SendAsync(Post(endpoint, request => request.Headers.Add("Origin", origin)), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Local_and_allowed_origins_pass()
    {
        var (app, endpoint) = await McpServerFixture.StartAsync(new ServerSettings { Http = true, Port = 0, AllowedOrigins = ["https://tools.example"] });
        await using var _ = app;
        using var http = new HttpClient();

        foreach (var origin in new[] { "http://localhost:5173", "http://127.0.0.1:3000", "https://tools.example" })
        {
            using var response = await http.SendAsync(Post(endpoint, request => request.Headers.Add("Origin", origin)), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, origin);
        }
    }

    [Fact]
    public async Task Requests_for_another_host_name_are_rejected()
    {
        var (app, endpoint) = await McpServerFixture.StartAsync(new ServerSettings { Http = true, Port = 0 });
        await using var _ = app;
        using var http = new HttpClient();

        // DNS rebinding: the browser sends the attacker's host name to the loopback address.
        using var response = await http.SendAsync(Post(endpoint, request => request.Headers.Host = "rebind.example:" + endpoint.Port), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bearer_token_is_required_when_set()
    {
        var (app, endpoint) = await McpServerFixture.StartAsync(new ServerSettings { Http = true, Port = 0, Token = "s3cret-token" });
        await using var _ = app;
        using var http = new HttpClient();

        using var missing = await http.SendAsync(Post(endpoint, _ => { }), TestContext.Current.CancellationToken);
        using var wrong = await http.SendAsync(Post(endpoint, r => r.Headers.Add("Authorization", "Bearer nope")), TestContext.Current.CancellationToken);

        missing.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpoint,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer s3cret-token" },
        });
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
        (await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken)).Count.ShouldBe(6);
    }

    [Fact]
    public void Settings_parse_the_command_line()
    {
        var settings = ServerSettings.Parse(["--http", "--port", "8080", "--allow-origin", "https://a.example/", "--token", "t"], _ => null);

        settings.Http.ShouldBeTrue();
        settings.Port.ShouldBe(8080);
        settings.Token.ShouldBe("t");
        settings.AllowedOrigins.ShouldBe(["https://a.example"]);
        ServerSettings.Parse([], name => name == ServerSettings.TokenVariable ? "from-env" : null).Token.ShouldBe("from-env");
        ServerSettings.Parse([], _ => null).Port.ShouldBe(7341);
        Should.Throw<ArgumentException>(() => ServerSettings.Parse(["--port", "70000"], _ => null));
        Should.Throw<ArgumentException>(() => ServerSettings.Parse(["--port"], _ => null));
        Should.Throw<ArgumentException>(() => ServerSettings.Parse(["--verbose"], _ => null));
    }

    private static HttpRequestMessage Post(Uri endpoint, Action<HttpRequestMessage> configure)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(Initialize, Encoding.UTF8, "application/json") };
        request.Headers.Add("Accept", "application/json, text/event-stream");
        configure(request);
        return request;
    }
}
