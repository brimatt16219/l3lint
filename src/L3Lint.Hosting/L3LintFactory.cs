using System.Globalization;
using L3Lint.Adapters;
using L3Lint.Core;
using L3Lint.Core.Rules;
using L3Lint.Rules.Amex;
using L3Lint.Rules.Discover;
using L3Lint.Rules.General;
using L3Lint.Rules.Mastercard;
using L3Lint.Rules.Visa;
using Microsoft.Extensions.Logging;

namespace L3Lint.Hosting;

/// <summary>
/// Builds linters with the five shipped rule packs and the seven shipped adapters, as the CLI, MCP server and
/// <see cref="L3LintServiceCollectionExtensions.AddL3Lint"/> do.
/// </summary>
public static class L3LintFactory
{
    /// <summary>
    /// Creates new instances of the five embedded rule packs.
    /// </summary>
    /// <returns>The general, Visa, Mastercard, Amex and Discover packs.</returns>
    public static IReadOnlyList<IRulePackSource> EmbeddedPacks() =>
        [new GeneralRulePack(), new VisaRulePack(), new MastercardRulePack(), new AmexRulePack(), new DiscoverRulePack()];

    /// <summary>
    /// Creates a builder with every shipped pack and adapter.
    /// </summary>
    /// <returns>A builder ready for more packs, rules, adapters or options.</returns>
    public static LinterBuilder CreateBuilder()
    {
        var builder = new LinterBuilder();
        foreach (var pack in EmbeddedPacks())
        {
            builder.AddRulePack(pack);
        }

        foreach (var adapter in StandardAdapters.All)
        {
            builder.AddAdapter(adapter);
        }

        return builder;
    }

    /// <summary>
    /// Creates a linter for resolved settings: shipped packs, local packs (which replace a shipped pack with the
    /// same ID), the lock file check and the default options.
    /// </summary>
    /// <param name="settings">The resolved settings.</param>
    /// <param name="loggerFactory">The logger factory, or <see langword="null"/> for none.</param>
    /// <param name="timeProvider">The clock for "today", or <see langword="null"/> for the system clock.</param>
    /// <returns>The linter.</returns>
    /// <exception cref="UsageException">A local pack folder does not exist.</exception>
    /// <exception cref="RulePackException">A pack is invalid or the lock file does not match.</exception>
    /// <exception cref="ConfigurationException">The lock file is invalid.</exception>
    public static ILinter Create(ResolvedSettings settings, ILoggerFactory? loggerFactory = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var builder = CreateBuilder().UseDefaults(settings.Options);
        foreach (var folder in settings.LocalPacks)
        {
            try
            {
                builder.AddRulePack(new DirectoryRulePackSource(folder));
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.UsagePackFolder, folder), ex);
            }
        }

        if (settings.LockFile is { } lockFile)
        {
            builder.RequirePackVersions(LockFile.Read(lockFile));
        }

        if (loggerFactory is not null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        if (timeProvider is not null)
        {
            builder.UseTimeProvider(timeProvider);
        }

        return builder.Build();
    }
}
