using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Corpus.Tests;

/// <summary>The outcome of linting one fixture against its expectations.</summary>
public sealed record FixtureOutcome(Fixture Fixture, LintResult Result, IReadOnlyList<Expected> Missing, IReadOnlyList<string> Unexpected, string? ExitMismatch, string? TierMismatch)
{
    public int ExitCode => Result.HasInternalError ? 3 : Result.InputErrors.Count > 0 ? 2 : Result.Findings.Any(f => f.Severity == Severity.Error) ? 1 : 0;

    public bool Passed => Missing.Count == 0 && Unexpected.Count == 0 && ExitMismatch is null && TierMismatch is null;

    public IEnumerable<Finding> Defects => Result.Findings.Where(f => f.Severity >= Severity.Warning);

    public string Explain() =>
        $"{Fixture.Name}: missing [{string.Join(", ", Missing.Select(m => m.RuleId + " " + m.Path + " " + m.Severity))}], "
        + $"unexpected [{string.Join(", ", Unexpected)}], {ExitMismatch} {TierMismatch} "
        + $"actual [{string.Join(", ", Result.Findings.Select(f => f.RuleId + " " + f.Path.Canonical + " " + WireNames.Of(f.Severity)))}]";
}

/// <summary>
/// Lints fixtures with the context each one states and compares the result with its expectations. The clock is
/// fixed at 2026-10-19, the date every fixture is written for.
/// </summary>
public static class CorpusRunner
{
    private static readonly Lazy<ILinter> Shared = new(() =>
        L3LintFactory.Create(new ResolvedSettings { Options = LintOptions.Default }, timeProvider: new FakeTimeProvider(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero))));

    public static ILinter Linter => Shared.Value;

    public static LintOptions OptionsFor(Fixture fixture)
    {
        var context = fixture.Context;
        var overrides = new LintOverrides
        {
            Brand = context.GetValueOrDefault("brand"),
            CardProduct = context.GetValueOrDefault("cardProduct"),
            Mcc = context.GetValueOrDefault("mcc"),
            Processor = context.GetValueOrDefault("processor"),
            AsOf = context.TryGetValue("asOf", out var asOf) ? DateOnly.ParseExact(asOf, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null,
            Rates = context.GetValueOrDefault("rates"),
            Only = context.TryGetValue("only", out var only) ? [only] : [],
        };
        return SettingsResolver.Resolve(L3LintConfiguration.Default, overrides, Path.GetDirectoryName(fixture.PayloadPath)!).Options;
    }

    public static FixtureOutcome Run(Fixture fixture)
    {
        var options = OptionsFor(fixture);
        var document = RawDocument.ParseJson(File.ReadAllBytes(fixture.PayloadPath), fixture.Name);
        var result = fixture.Context.TryGetValue("copies", out var copies) ? LintBatch(document, options, int.Parse(copies, System.Globalization.CultureInfo.InvariantCulture)) : Linter.Lint(document, options);
        var missing = fixture.Findings.Where(expected => !result.Findings.Any(f =>
            f.RuleId == expected.RuleId
            && (expected.Path == "*" || f.Path.Canonical == expected.Path)
            && (expected.Severity is null || WireNames.Of(f.Severity) == expected.Severity))).ToList();
        var unexpected = fixture.Absent.Where(id => result.Findings.Any(f => f.RuleId == id)).ToList();
        var outcome = new FixtureOutcome(fixture, result, missing, unexpected, null, null);
        string? exitMismatch = fixture.ExitCode is { } code && code != outcome.ExitCode ? $"exit {outcome.ExitCode} instead of {code}" : null;
        string? tierMismatch = null;
        if (fixture.TierStatus is { } status)
        {
            var tiers = result.Transactions.SelectMany(t => t.Tiers).ToList();
            var tier = tiers.FirstOrDefault(t => Core.Model.CanonicalNames.ToName(t.Brand) == fixture.TierBrand);
            var actual = tier is null ? "none" : WireNames.Of(tier.Status);
            tierMismatch = actual == status ? null : $"tier {actual} instead of {status}";
        }

        return outcome with { ExitMismatch = exitMismatch, TierMismatch = tierMismatch };
    }

    // Lints the payload as a batch of identical transactions and returns the batch result's findings.
    private static LintResult LintBatch(RawDocument document, LintOptions options, int copies)
    {
        var session = Linter.CreateSession(options);
        for (var i = 0; i < copies; i++)
        {
            session.Lint(document, i, i);
        }

        var batch = session.Complete(copies);
        var single = Linter.Lint(document, options);
        return single with { Findings = [.. single.Findings, .. batch?.Findings ?? []] };
    }
}
