using System.Reflection;

namespace L3Lint.Architecture.Tests;

/// <summary>
/// No telemetry, crash reporting or update checks in any component (Architecture tab, "Privacy commitments";
/// NFR-005). The check walks every assembly each shipped assembly loads, directly or through a package.
/// </summary>
public sealed class NoTelemetryTests
{
    /// <summary>Assembly name prefixes of telemetry SDKs, exporters, crash reporters and self-updaters.</summary>
    private static readonly string[] Banned =
    [
        "Microsoft.ApplicationInsights",
        "Microsoft.VisualStudio.Telemetry",
        "Microsoft.Extensions.Telemetry",
        "Azure.Monitor",
        "OpenTelemetry",
        "Sentry",
        "Bugsnag",
        "Raygun",
        "Rollbar",
        "Datadog",
        "NewRelic",
        "Elastic.Apm",
        "Honeycomb",
        "Segment",
        "Mixpanel",
        "Amplitude",
        "PostHog",
        "Serilog.Sinks.Http",
        "NuGet.Protocol",
        "Octokit",
        "Squirrel",
        "Velopack",
        "NetSparkle",
        "AutoUpdater",
    ];

    [Theory]
    [MemberData(nameof(Layers.AllNames), MemberType = typeof(Layers))]
    public void No_shipped_assembly_loads_telemetry_or_update_code(string assemblyName)
    {
        var closure = Closure(Layers.Named(assemblyName).Load());

        var offending = closure.Where(name => Banned.Any(b => name.StartsWith(b, StringComparison.OrdinalIgnoreCase))).ToList();

        closure.Count.ShouldBeGreaterThan(1);
        offending.ShouldBeEmpty($"{assemblyName} loads telemetry or update-check assemblies: {string.Join(", ", offending)}");
    }

    [Theory]
    [MemberData(nameof(Layers.OfflineNames), MemberType = typeof(Layers))]
    public void Offline_assemblies_do_not_pull_in_a_web_server_or_HTTP_client_package(string assemblyName)
    {
        var closure = Closure(Layers.Named(assemblyName).Load());

        var offending = closure
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || name.StartsWith("ModelContextProtocol", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.Extensions.Http", StringComparison.Ordinal)
                || name.StartsWith("RestSharp", StringComparison.Ordinal)
                || name.StartsWith("Flurl", StringComparison.Ordinal)
                || name.StartsWith("Refit", StringComparison.Ordinal))
            .ToList();

        offending.ShouldBeEmpty($"{assemblyName} loads web or HTTP client packages: {string.Join(", ", offending)}");
    }

    [Fact]
    public void The_walk_reaches_package_and_framework_assemblies()
    {
        // Positive control: the MCP server's closure holds its SDK and ASP.NET Core, so the walk follows packages.
        var closure = Closure(Layers.Named(Layers.Mcp).Load());

        closure.ShouldContain("ModelContextProtocol.AspNetCore");
        closure.ShouldContain(name => name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
        closure.ShouldContain("YamlDotNet");
    }

    /// <summary>Every assembly name reachable through references, loading each one that can be loaded.</summary>
    private static SortedSet<string> Closure(Assembly root)
    {
        var seen = new SortedSet<string>(StringComparer.Ordinal) { root.GetName().Name! };
        var queue = new Queue<Assembly>([root]);
        while (queue.Count > 0)
        {
            foreach (var reference in queue.Dequeue().GetReferencedAssemblies())
            {
                if (reference.Name is not { } name || !seen.Add(name))
                {
                    continue;
                }

                if (TryLoad(reference) is { } loaded)
                {
                    queue.Enqueue(loaded);
                }
            }
        }

        return seen;
    }

    private static Assembly? TryLoad(AssemblyName name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (FileNotFoundException)
        {
            // A reference that is never resolved at run time (for example a facade for another platform) is still
            // checked by name above; there is nothing more to walk.
            return null;
        }
    }
}
