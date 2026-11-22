using System.Reflection;

namespace L3Lint.Architecture.Tests;

/// <summary>One shipped l3lint assembly: its file name, its root namespace and the l3lint assemblies it may use.</summary>
/// <param name="AssemblyName">The assembly name, for example <c>L3Lint.Core</c> or <c>l3lint</c> for the CLI.</param>
/// <param name="Namespace">The root namespace of its types, which other assemblies would reference.</param>
/// <param name="Allowed">The l3lint assemblies it may depend on (Architecture tab, "Dependency direction").</param>
public sealed record Layer(string AssemblyName, string Namespace, IReadOnlyList<string> Allowed)
{
    public Assembly Load() => Assembly.Load(new AssemblyName(AssemblyName));

    public override string ToString() => AssemblyName;
}

/// <summary>
/// The dependency direction from the Architecture tab: Rules, Adapters and Reporting depend on Core; Hosting
/// depends on those; Cli and Mcp depend on Hosting; nothing depends on Cli or Mcp (NFR-013).
/// </summary>
public static class Layers
{
    public const string Core = "L3Lint.Core";
    public const string Adapters = "L3Lint.Adapters";
    public const string Reporting = "L3Lint.Reporting";
    public const string Hosting = "L3Lint.Hosting";
    public const string Cli = "l3lint";
    public const string Mcp = "l3lint-mcp";

    public static IReadOnlyList<string> Packs { get; } =
    [
        "L3Lint.Rules.General",
        "L3Lint.Rules.Visa",
        "L3Lint.Rules.Mastercard",
        "L3Lint.Rules.Amex",
        "L3Lint.Rules.Discover",
    ];

    private static readonly string[] BelowHosts = [Core, .. Packs, Adapters, Reporting, Hosting];

    public static IReadOnlyList<Layer> All { get; } =
    [
        new(Core, "L3Lint.Core", []),
        .. Packs.Select(p => new Layer(p, p, [Core])),
        new(Adapters, "L3Lint.Adapters", [Core]),
        new(Reporting, "L3Lint.Reporting", [Core]),
        new(Hosting, "L3Lint.Hosting", [Core, .. Packs, Adapters, Reporting]),
        new(Cli, "L3Lint.Cli", BelowHosts),
        new(Mcp, "L3Lint.Mcp", BelowHosts),
    ];

    /// <summary>The assemblies the spec says may never open a network connection (Architecture tab, threat table).</summary>
    public static IReadOnlyList<Layer> Offline { get; } = [.. All.Where(l => l.AssemblyName != Mcp)];

    public static Layer Named(string assemblyName) => All.Single(l => l.AssemblyName == assemblyName);

    public static TheoryData<string> AllNames() => [.. All.Select(l => l.AssemblyName)];

    public static TheoryData<string> OfflineNames() => [.. Offline.Select(l => l.AssemblyName)];

    /// <summary>Whether a referenced assembly name belongs to l3lint.</summary>
    public static bool IsOwn(string? name) => name is not null && All.Any(l => string.Equals(l.AssemblyName, name, StringComparison.Ordinal));
}
