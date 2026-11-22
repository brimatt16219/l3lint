using NetArchTest.Rules;

namespace L3Lint.Architecture.Tests;

/// <summary>
/// The shipped tool makes no network calls (FR-035, NFR-005, ADR-0003). Only the MCP server may use ASP.NET Core,
/// and only for its local HTTP transport.
/// </summary>
public sealed class NoNetworkTests
{
    /// <summary>Namespaces and types that open connections or resolve names.</summary>
    private static readonly string[] NetworkApis =
    [
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Net.WebSockets",
        "System.Net.WebClient",
        "System.Net.WebRequest",
        "System.Net.HttpWebRequest",
        "System.Net.FtpWebRequest",
        "System.Net.Dns",
        "System.Net.Mail",
        "System.Net.NetworkInformation",
        "System.Net.Security",
        "System.Net.Quic",
    ];

    /// <summary>Framework assemblies that hold those APIs.</summary>
    private static readonly string[] NetworkAssemblies =
    [
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Net.WebSockets",
        "System.Net.WebSockets.Client",
        "System.Net.Requests",
        "System.Net.WebClient",
        "System.Net.NameResolution",
        "System.Net.Mail",
        "System.Net.NetworkInformation",
        "System.Net.Security",
        "System.Net.Quic",
    ];

    [Theory]
    [MemberData(nameof(Layers.OfflineNames), MemberType = typeof(Layers))]
    public void Offline_assemblies_use_no_network_api(string assemblyName)
    {
        var result = Types.InAssembly(Layers.Named(assemblyName).Load())
            .ShouldNot().HaveDependencyOnAny(NetworkApis)
            .GetResult();

        DependencyDirectionTests.Offenders(result).ShouldBeEmpty(
            $"Types in {assemblyName} use a network API:\n  " + string.Join("\n  ", DependencyDirectionTests.Offenders(result)));
    }

    [Theory]
    [MemberData(nameof(Layers.OfflineNames), MemberType = typeof(Layers))]
    public void Offline_assemblies_reference_no_network_assembly(string assemblyName)
    {
        var offending = Layers.Named(assemblyName).Load().GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(name => NetworkAssemblies.Contains(name, StringComparer.Ordinal) || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToList();

        offending.ShouldBeEmpty($"{assemblyName} references network assemblies: {string.Join(", ", offending)}");
    }

    [Theory]
    [MemberData(nameof(Layers.OfflineNames), MemberType = typeof(Layers))]
    public void Only_the_MCP_server_uses_ASP_NET_Core(string assemblyName)
    {
        var result = Types.InAssembly(Layers.Named(assemblyName).Load())
            .ShouldNot().HaveDependencyOnAny("Microsoft.AspNetCore", "ModelContextProtocol")
            .GetResult();

        DependencyDirectionTests.Offenders(result).ShouldBeEmpty(
            $"Types in {assemblyName} use ASP.NET Core or the MCP SDK:\n  " + string.Join("\n  ", DependencyDirectionTests.Offenders(result)));
    }

    [Fact]
    public void The_MCP_server_never_makes_outbound_calls()
    {
        // The MCP server listens on loopback (System.Net.IPAddress is allowed) but never connects anywhere.
        var result = Types.InAssembly(Layers.Named(Layers.Mcp).Load())
            .ShouldNot().HaveDependencyOnAny(NetworkApis)
            .GetResult();

        DependencyDirectionTests.Offenders(result).ShouldBeEmpty(
            "Types in the MCP server use an outbound network API:\n  " + string.Join("\n  ", DependencyDirectionTests.Offenders(result)));
    }

    [Fact]
    public void MCP_tools_and_content_do_not_touch_the_web_stack()
    {
        // ASP.NET Core is only for the local Streamable HTTP transport, so the tools and content stay transport-free.
        var result = Types.InAssembly(Layers.Named(Layers.Mcp).Load())
            .That().ResideInNamespace("L3Lint.Mcp.Tools").Or().ResideInNamespace("L3Lint.Mcp.Content")
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        DependencyDirectionTests.Offenders(result).ShouldBeEmpty(
            "MCP tools or content depend on ASP.NET Core:\n  " + string.Join("\n  ", DependencyDirectionTests.Offenders(result)));
    }

    [Fact]
    public void MCP_tools_never_open_local_files()
    {
        // A tool argument can never name a file to read: the tool code has no file system API at all
        // (Architecture tab, "The MCP server processes only what the client sends in the tool call").
        var result = Types.InAssembly(Layers.Named(Layers.Mcp).Load())
            .That().ResideInNamespace("L3Lint.Mcp.Tools").Or().ResideInNamespace("L3Lint.Mcp.Content")
            .ShouldNot().HaveDependencyOnAny(
                "System.IO.File",
                "System.IO.FileInfo",
                "System.IO.FileStream",
                "System.IO.Directory",
                "System.IO.DirectoryInfo",
                "L3Lint.Core.Rules.DirectoryRulePackSource",
                "L3Lint.Hosting.Runs.InputFile",
                "L3Lint.Hosting.Runs.LintRunner",
                "L3Lint.Hosting.LockFile")
            .GetResult();

        DependencyDirectionTests.Offenders(result).ShouldBeEmpty(
            "MCP tool or content types use a file system API:\n  " + string.Join("\n  ", DependencyDirectionTests.Offenders(result)));
    }

    [Fact]
    public void Checks_see_real_dependencies()
    {
        // Positive controls: the same checks do find dependencies that exist, so a green run is not vacuous.
        var mcp = Layers.Named(Layers.Mcp).Load();
        DependencyDirectionTests.Offenders(Types.InAssembly(mcp).ShouldNot().HaveDependencyOn("Microsoft.AspNetCore").GetResult())
            .ShouldContain("L3Lint.Mcp.LocalRequestGuard");
        DependencyDirectionTests.Offenders(Types.InAssembly(mcp).ShouldNot().HaveDependencyOn("System.Net.IPAddress").GetResult())
            .ShouldContain("L3Lint.Mcp.McpApp");
        DependencyDirectionTests.Offenders(Types.InAssembly(Layers.Named(Layers.Hosting).Load()).ShouldNot().HaveDependencyOn("L3Lint.Core").GetResult())
            .ShouldNotBeEmpty();
    }
}
