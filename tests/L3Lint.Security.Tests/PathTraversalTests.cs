using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;

namespace L3Lint.Security.Tests;

/// <summary>
/// A local rule pack (<c>--rule-pack</c>) is data only and cannot reach files outside its own folder (Architecture
/// tab, threat "Malicious rule pack"). The GitHub Action script has its own traversal tests in L3Lint.Cli.Tests.
/// </summary>
public sealed class PathTraversalTests : IDisposable
{
    private const string SecretMarker = "SECRET-OUTSIDE-THE-PACK-4471";

    private readonly TempFolder _root = new();

    public PathTraversalTests()
    {
        Directory.CreateDirectory(Pack);
        Directory.CreateDirectory(Sibling);
        File.WriteAllText(Path.Combine(Pack, "inside.csv"), "value\ninside\n");
        File.WriteAllText(Path.Combine(_root.Path, "secret.csv"), "value\n" + SecretMarker + "\n");
        File.WriteAllText(Path.Combine(Sibling, "secret.csv"), "value\n" + SecretMarker + "\n");
    }

    private string Pack => Path.Combine(_root.Path, "pack");

    // A sibling folder whose name starts with the pack folder's name.
    private string Sibling => Path.Combine(_root.Path, "pack-evil");

    [Fact]
    public void Pack_source_opens_its_own_files()
    {
        using var stream = new DirectoryRulePackSource(Pack).Open("inside.csv");

        new StreamReader(stream).ReadToEnd().ShouldContain("inside");
    }

    [Theory]
    [InlineData("../secret.csv")]
    [InlineData("..\\secret.csv")]
    [InlineData("lists/../../secret.csv")]
    public void Pack_source_refuses_a_parent_path(string relativePath)
    {
        Should.Throw<FileNotFoundException>(() => new DirectoryRulePackSource(Pack).Open(relativePath).Dispose());
    }

    [Fact]
    public void Pack_source_refuses_an_absolute_path()
    {
        Should.Throw<FileNotFoundException>(() => new DirectoryRulePackSource(Pack).Open(Path.Combine(_root.Path, "secret.csv")).Dispose());
    }

    [Theory]
    [InlineData("../pack-evil/secret.csv")]
    [InlineData("..\\pack-evil\\secret.csv")]
    public void Pack_source_refuses_a_sibling_folder_that_shares_its_name_prefix(string relativePath)
    {
        // "pack-evil" starts with "pack", so a plain prefix check on the full path lets it through.
        Should.Throw<FileNotFoundException>(() => new DirectoryRulePackSource(Pack).Open(relativePath).Dispose());
    }

    [Theory]
    [InlineData("../secret.csv")]
    [InlineData("../pack-evil/secret.csv")]
    public void A_pack_list_outside_the_folder_is_refused_and_never_read(string listPath)
    {
        TestPacks.Write(Pack, "local.rules.yaml", TestPacks.MinimalRules("  lists:\n    outside: " + listPath + "\n"));

        var exception = Should.Throw<RulePackException>(() => new L3Lint.Core.LinterBuilder().AddRulePack(new DirectoryRulePackSource(Pack)).Build());

        exception.Message.ShouldNotContain(SecretMarker);
        exception.ToString().ShouldNotContain(SecretMarker);
    }

    [Fact]
    public void A_valid_local_pack_with_a_list_inside_its_folder_loads()
    {
        // Positive control: the same pack with its list inside the folder loads, so the refusals below come from the
        // path, not from a broken pack.
        TestPacks.Write(Pack, "local.rules.yaml", TestPacks.MinimalRules("  lists:\n    inside: inside.csv\n"));

        var linter = new L3Lint.Core.LinterBuilder().AddRulePack(new DirectoryRulePackSource(Pack)).Build();

        linter.RulePacks.ShouldHaveSingleItem().Id.ShouldBe("l3lint.rules.local");
    }

    [Fact]
    public void A_manifest_that_names_a_file_outside_the_folder_is_refused()
    {
        TestPacks.Write(Pack, "local.rules.yaml", TestPacks.MinimalRules(string.Empty), extraManifestEntries: new() { ["../secret.csv"] = Sha256(File.ReadAllBytes(Path.Combine(_root.Path, "secret.csv"))) });

        Should.Throw<RulePackException>(() => new L3Lint.Core.LinterBuilder().AddRulePack(new DirectoryRulePackSource(Pack)).Build());
    }

    [Theory]
    [InlineData("../../schemas/config.schema.json")]
    [InlineData("config.schema.json")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    public void The_MCP_get_schema_tool_resolves_names_only(string name)
    {
        // get_schema looks names up in the embedded catalog (src/L3Lint.Mcp/Tools/LintTools.cs); a path is just an
        // unknown name. Architecture.Tests checks that MCP tool code has no file system API at all.
        SchemaCatalog.TryGetText(name, out _).ShouldBeFalse();
        SchemaCatalog.TryGetText("config", out var text).ShouldBeTrue();
        text.ShouldContain("$schema");
    }

    public void Dispose() => _root.Dispose();

    internal static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}

/// <summary>Writes a local rule pack folder with a manifest whose checksums match its files.</summary>
internal static class TestPacks
{
    /// <summary>A pack header plus one declarative rule, with extra header lines (such as lists) spliced in.</summary>
    public static string MinimalRules(string extraHeader) =>
        "pack:\n  id: l3lint.rules.local\n  version: 1.0.0\n  brand: general\n  publishedOn: 2026-10-25\n  title: Local\n  description: Local test pack.\n"
        + extraHeader
        + "rules:\n"
        + "  - id: GEN-LOCAL-001\n    title: Merchant name is required\n    requirement: Send the merchant name.\n    rationale: Test rule.\n"
        + "    level: general\n    severity: warning\n    confidence: inferred\n    check: { kind: required, field: /merchant/name }\n"
        + "    message: The merchant name is missing.\n    fix: Send the merchant name.\n    impact: hygiene\n"
        + "    sources:\n      - https://example.com/local\n";

    public static void Write(string folder, string rulesFile, string body, Dictionary<string, string>? extraManifestEntries = null)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, rulesFile), body, new UTF8Encoding(false));
        var files = new JsonObject();
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(folder, file).Replace('\\', '/');
            if (relative != "manifest.json")
            {
                files[relative] = PathTraversalTests.Sha256(File.ReadAllBytes(file));
            }
        }

        foreach (var (name, hash) in extraManifestEntries ?? [])
        {
            files[name] = hash;
        }

        var manifest = new JsonObject { ["pack"] = "l3lint.rules.local", ["version"] = "1.0.0", ["algorithm"] = "sha256", ["files"] = files };
        File.WriteAllText(Path.Combine(folder, "manifest.json"), manifest.ToJsonString(), new UTF8Encoding(false));
    }
}
