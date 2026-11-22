using System.Text.RegularExpressions;

namespace L3Lint.Architecture.Tests;

/// <summary>
/// Third-party steps in the GitHub Action and the repository workflows are pinned by full commit SHA (Architecture
/// tab, threat "Tampered releases": "Action steps pinned by commit SHA"; NFR-008).
/// </summary>
public sealed partial class SupplyChainTests
{
    [Fact]
    public void Every_third_party_step_is_pinned_by_commit_sha()
    {
        var files = Repository.Files("action", ".yml", ".yaml").Concat(Repository.Files(Path.Combine(".github", "workflows"), ".yml", ".yaml")).ToList();
        var steps = files
            .SelectMany(file => File.ReadAllLines(file).Select((line, index) => (File: file, Line: index + 1, Match: UsesLine().Match(line))))
            .Where(s => s.Match.Success)
            .ToList();

        var unpinned = steps
            .Where(s => !s.Match.Groups["target"].Value.StartsWith("./", StringComparison.Ordinal))
            .Where(s => !PinnedTarget().IsMatch(s.Match.Groups["target"].Value))
            .Select(s => $"{Repository.Relative(s.File)}:{s.Line}: {s.Match.Groups["target"].Value}")
            .ToList();

        files.ShouldContain(f => f.EndsWith("action.yml", StringComparison.Ordinal));
        steps.ShouldNotBeEmpty();
        unpinned.ShouldBeEmpty("Steps must be pinned by a 40-character commit SHA:\n  " + string.Join("\n  ", unpinned));
    }

    [GeneratedRegex(@"^\s*-?\s*uses:\s*(?<target>[^\s#]+)", RegexOptions.CultureInvariant)]
    private static partial Regex UsesLine();

    [GeneratedRegex(@"^[\w.-]+/[\w.-]+(/[\w./-]+)?@[0-9a-f]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex PinnedTarget();
}
