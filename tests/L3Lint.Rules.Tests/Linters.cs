using L3Lint.Core;
using L3Lint.Rules.Amex;
using L3Lint.Rules.Discover;
using L3Lint.Rules.General;
using L3Lint.Rules.Mastercard;
using L3Lint.Rules.Visa;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Rules.Tests;

/// <summary>A linter with the five shipped packs and a fixed clock (2026-10-19, the date of the spec's example).</summary>
internal static class Linters
{
    public static FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public static ILinter Packs { get; } = new LinterBuilder()
        .AddRulePack(new GeneralRulePack())
        .AddRulePack(new VisaRulePack())
        .AddRulePack(new MastercardRulePack())
        .AddRulePack(new AmexRulePack())
        .AddRulePack(new DiscoverRulePack())
        .UseTimeProvider(Clock)
        .Build();
}
