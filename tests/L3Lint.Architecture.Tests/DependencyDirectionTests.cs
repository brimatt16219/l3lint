using NetArchTest.Rules;

namespace L3Lint.Architecture.Tests;

/// <summary>Enforces the dependency direction in the Architecture tab (NFR-013).</summary>
public sealed class DependencyDirectionTests
{
    [Theory]
    [MemberData(nameof(Layers.AllNames), MemberType = typeof(Layers))]
    public void Assembly_references_only_the_layers_below_it(string assemblyName)
    {
        var layer = Layers.Named(assemblyName);

        var offending = layer.Load().GetReferencedAssemblies()
            .Select(r => r.Name)
            .Where(Layers.IsOwn)
            .Where(name => !layer.Allowed.Contains(name!))
            .Order(StringComparer.Ordinal)
            .ToList();

        offending.ShouldBeEmpty(
            $"{assemblyName} may reference only [{string.Join(", ", layer.Allowed)}] but references [{string.Join(", ", offending)}].");
    }

    [Theory]
    [MemberData(nameof(Layers.AllNames), MemberType = typeof(Layers))]
    public void No_type_uses_a_layer_it_must_not_depend_on(string assemblyName)
    {
        var layer = Layers.Named(assemblyName);
        var forbidden = Layers.All
            .Where(l => l.AssemblyName != assemblyName && !layer.Allowed.Contains(l.AssemblyName))
            .Select(l => l.Namespace)
            .ToArray();

        var result = Types.InAssembly(layer.Load()).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Offenders(result).ShouldBeEmpty(
            $"Types in {assemblyName} depend on [{string.Join(", ", forbidden)}], which the Architecture tab forbids:\n  "
            + string.Join("\n  ", Offenders(result)));
    }

    [Theory]
    [InlineData(Layers.Cli)]
    [InlineData(Layers.Mcp)]
    public void Hosts_sit_on_Hosting(string assemblyName)
    {
        var references = Layers.Named(assemblyName).Load().GetReferencedAssemblies().Select(r => r.Name).ToList();

        references.ShouldContain(Layers.Hosting);
    }

    [Fact]
    public void Nothing_depends_on_a_host()
    {
        var hostReferences = Layers.All
            .SelectMany(l => l.Load().GetReferencedAssemblies().Select(r => (From: l.AssemblyName, To: r.Name)))
            .Where(pair => pair.To is Layers.Cli or Layers.Mcp)
            .Select(pair => pair.From + " -> " + pair.To)
            .ToList();

        hostReferences.ShouldBeEmpty("No l3lint assembly may depend on the CLI or the MCP server.");
    }

    [Fact]
    public void The_layer_table_covers_every_production_project()
    {
        var projects = Directory.EnumerateFiles(Path.Combine(Repository.Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Order(StringComparer.Ordinal)
            .ToList();
        var namespaces = Layers.All.Select(l => l.Namespace).Order(StringComparer.Ordinal).ToList();

        // Each project folder is named after its root namespace, so a new project shows up here until it is placed in a layer.
        namespaces.ShouldBe(projects);
    }

    internal static IReadOnlyList<string> Offenders(NetArchTest.Rules.TestResult result) =>
        result.IsSuccessful ? [] : [.. (result.FailingTypeNames ?? []).Order(StringComparer.Ordinal)];
}
