using System.Xml.Linq;

namespace L3Lint.Architecture.Tests;

/// <summary>Every library package locks its public surface with the PublicApiAnalyzers files (src/Directory.Build.targets).</summary>
public sealed class PublicApiFilesTests
{
    public static TheoryData<string> Libraries()
    {
        var data = new TheoryData<string>();
        foreach (var project in Repository.Files("src", ".csproj"))
        {
            var outputType = XDocument.Load(project).Descendants("OutputType").Select(e => e.Value.Trim()).LastOrDefault();
            if (!string.Equals(outputType, "Exe", StringComparison.OrdinalIgnoreCase))
            {
                data.Add(Repository.Relative(project));
            }
        }

        return data;
    }

    [Fact]
    public void Every_library_is_found()
    {
        // Core, five packs, Adapters, Reporting and Hosting; the CLI and MCP server are executables.
        Libraries().Count.ShouldBe(9);
    }

    [Theory]
    [MemberData(nameof(Libraries))]
    public void Library_has_both_public_API_files(string project)
    {
        var folder = Path.GetDirectoryName(Path.Combine(Repository.Root, project))!;

        File.Exists(Path.Combine(folder, "PublicAPI.Shipped.txt")).ShouldBeTrue($"{project} has no PublicAPI.Shipped.txt");
        File.Exists(Path.Combine(folder, "PublicAPI.Unshipped.txt")).ShouldBeTrue($"{project} has no PublicAPI.Unshipped.txt");
    }
}
