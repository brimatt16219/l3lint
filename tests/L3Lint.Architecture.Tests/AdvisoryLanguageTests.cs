using System.Text.RegularExpressions;

namespace L3Lint.Architecture.Tests;

/// <summary>
/// Advisory language only (decision D5, ADR-0005): output says "likely to downgrade" or "meets the documented
/// requirement" and never claims that a payload qualifies for a rate. Scans every text that reaches a user: resource
/// strings, rule packs and their lists, processor profiles, CLI templates, MCP content, the Action, the samples and
/// the docs site.
/// </summary>
/// <remarks>
/// Source URLs are removed before the scan, because a cited page's address is not l3lint's wording. The pattern
/// spells the word with a one-letter character class so that this file never matches its own search.
/// </remarks>
public sealed partial class AdvisoryLanguageTests
{
    [Fact]
    public void No_shipped_text_says_a_payload_qualifies()
    {
        var files = new[]
        {
            Repository.Files("src", ".resx", ".yaml", ".yml", ".csv", ".json", ".md"),
            Repository.Files("action"),
            Repository.Files("samples"),
            Repository.Files("docs/articles", ".md"),
            Repository.Files("docs/rules", ".md"),
            [Path.Combine(Repository.Root, "README.md"), Path.Combine(Repository.Root, "docs", "index.md")],
        }.SelectMany(f => f).ToList();

        // 78 rule pages, 18 articles, the packs, lists, profiles and resources: an empty scan would prove nothing.
        files.Count.ShouldBeGreaterThan(150);
        var hits = new List<string>();
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var prose = UrlPattern().Replace(lines[i], string.Empty);
                if (BannedPattern().IsMatch(prose))
                {
                    hits.Add($"{Repository.Relative(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        hits.ShouldBeEmpty("D5: never say a payload qualifies; say it is likely to downgrade or meets the documented requirement.\n  " + string.Join("\n  ", hits));
    }

    [Fact]
    public void The_scan_finds_the_banned_wording_and_ignores_urls()
    {
        // Positive control: the patterns must catch the claim in prose and skip it inside a cited address.
        BannedPattern().IsMatch("This payload qualifies for Level 3.").ShouldBeTrue();
        BannedPattern().IsMatch("It will qualify for the rate.").ShouldBeTrue();
        BannedPattern().IsMatch(UrlPattern().Replace("See https://example.org/how-to-qualify-cedp-rates/ for details.", string.Empty)).ShouldBeFalse();
        BannedPattern().IsMatch("\"fullyQualifiedName\": \"/lineItems/0\"").ShouldBeFalse();
    }

    [GeneratedRegex(@"\b[q]ualif(y|ies|ied|ying)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BannedPattern();

    [GeneratedRegex(@"https?://\S+", RegexOptions.CultureInvariant)]
    private static partial Regex UrlPattern();
}
