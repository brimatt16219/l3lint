using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Rules.Amex;
using L3Lint.Rules.Discover;
using L3Lint.Rules.General;
using L3Lint.Rules.Mastercard;
using L3Lint.Rules.Visa;
using Microsoft.Extensions.Time.Testing;

namespace L3Lint.Adapters.Tests.Support;

/// <summary>A linter with the five shipped packs, the seven standard adapters and a fixed clock.</summary>
internal static class Linters
{
    public static FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public static ILinter All { get; } = Build();

    public static AdapterRegistry Registry { get; } = new(StandardAdapters.All);

    public static IPayloadAdapter Adapter(string name) => StandardAdapters.All.Single(a => a.Name == name);

    private static ILinter Build()
    {
        var builder = new LinterBuilder()
            .AddRulePack(new GeneralRulePack())
            .AddRulePack(new VisaRulePack())
            .AddRulePack(new MastercardRulePack())
            .AddRulePack(new AmexRulePack())
            .AddRulePack(new DiscoverRulePack())
            .UseTimeProvider(Clock);
        foreach (var adapter in StandardAdapters.All)
        {
            builder.AddAdapter(adapter);
        }

        return builder.Build();
    }
}
