using System.Text;
using System.Text.RegularExpressions;

namespace L3Lint.Architecture.Tests;

/// <summary>
/// No shortcuts in shipped source (build directive): no open-work markers, no unimplemented members, no empty catch
/// blocks, and no em-dash or en-dash characters in source, docs, schemas or rule pack YAML.
/// </summary>
/// <remarks>
/// The patterns use a one-letter character class (for example <c>TO[D]O</c>) so that this file never matches its own
/// search in a repository-wide scan.
/// </remarks>
public sealed partial class ShippedSourceTests
{
    private const char EmDash = (char)0x2014;
    private const char EnDash = (char)0x2013;

    private static readonly string[] TextExtensions =
        [".cs", ".yaml", ".yml", ".json", ".csv", ".md", ".txt", ".resx", ".csproj", ".props", ".targets", ".xml", ".xsd", ".sh"];

    [Fact]
    public void Shipped_source_has_no_open_work_markers()
    {
        var hits = Scan(Repository.Files("src", ".cs"), MarkerPattern());

        hits.ShouldBeEmpty("Open-work markers in shipped source:\n  " + string.Join("\n  ", hits));
    }

    [Fact]
    public void Shipped_source_has_no_unimplemented_members()
    {
        var hits = Scan(Repository.Files("src", ".cs"), UnimplementedPattern());

        hits.ShouldBeEmpty("Unimplemented members in shipped source:\n  " + string.Join("\n  ", hits));
    }

    [Fact]
    public void Shipped_source_has_no_empty_catch_blocks()
    {
        var hits = Repository.Files("src", ".cs")
            .SelectMany(file => EmptyCatches(File.ReadAllText(file)).Select(line => $"{Repository.Relative(file)}:{line}"))
            .ToList();

        hits.ShouldBeEmpty("Empty catch blocks (a catch must handle, log or rethrow):\n  " + string.Join("\n  ", hits));
    }

    [Fact]
    public void Source_docs_schemas_and_packs_use_no_em_or_en_dashes()
    {
        var files = new[] { "src", "docs", "schemas" }.SelectMany(folder => Repository.Files(folder, TextExtensions)).ToList();

        var hits = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(EmDash, StringComparison.Ordinal) || lines[i].Contains(EnDash, StringComparison.Ordinal))
                {
                    hits.Add($"{Repository.Relative(file)}:{i + 1}");
                }
            }
        }

        files.Count.ShouldBeGreaterThan(100);
        files.ShouldContain(f => f.EndsWith(".rules.yaml", StringComparison.Ordinal));
        hits.ShouldBeEmpty("Em-dash or en-dash characters (write plain sentences instead):\n  " + string.Join("\n  ", hits));
    }

    // The samples are split with + so a plain text search of the repository does not report them as real catch blocks.
    [Theory]
    [InlineData("try { Run(); } catch" + " {  }")]
    [InlineData("try { Run(); }\ncatch (IOException)\n{\n    // nothing to do\n}")]
    [InlineData("try { Run(); } catch (Exception ex) when (ex is IOException)" + " { /* ignored */ }")]
    public void Empty_catch_detector_finds_empty_blocks(string code)
    {
        EmptyCatches(code).ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("try { Run(); } catch (IOException ex) { Log(ex); }")]
    [InlineData("try { Run(); } catch { throw; }")]
    [InlineData("var catcher = \"catch\";")]
    public void Empty_catch_detector_ignores_handled_blocks(string code)
    {
        EmptyCatches(code).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("// TO" + "DO: finish")]
    [InlineData("/* FIX" + "ME */")]
    public void Marker_detector_finds_markers(string code)
    {
        MarkerPattern().IsMatch(code).ShouldBeTrue();
    }

    private static List<string> Scan(IEnumerable<string> files, Regex pattern)
    {
        var hits = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    hits.Add($"{Repository.Relative(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        return hits;
    }

    /// <summary>Returns the line number of each catch block whose body holds only whitespace or comments.</summary>
    private static List<int> EmptyCatches(string code) =>
        [.. EmptyCatchPattern().Matches(code).Select(m => code.AsSpan(0, m.Index).Count('\n') + 1)];

    [GeneratedRegex(@"\bTO[D]O\b|\bFI[X]ME\b", RegexOptions.CultureInvariant)]
    private static partial Regex MarkerPattern();

    [GeneratedRegex(@"NotImplemented[E]xception", RegexOptions.CultureInvariant)]
    private static partial Regex UnimplementedPattern();

    // catch, an optional (Type name), an optional when (...) filter with one level of nested parentheses, then a
    // block that holds only whitespace, line comments or block comments. Atomic groups keep the match linear.
    [GeneratedRegex(
        @"\bcatch\b\s*(?>\([^()]*\))?\s*(?>when\s*\((?>[^()]+|\([^()]*\))*\))?\s*\{(?>\s+|//[^\n]*|/\*[\s\S]*?\*/)*\}",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmptyCatchPattern();
}
