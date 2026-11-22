using System.Globalization;
using L3Lint.Core.Adapters;
using L3Lint.Core.Engine;
using L3Lint.Core.Packs;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace L3Lint.Core;

/// <summary>
/// Builds an <see cref="ILinter"/> from rule packs, adapters and checks, validating everything up front.
/// </summary>
/// <remarks>
/// <see cref="Build"/> loads every pack once: it verifies manifests, validates YAML against the rule pack
/// schema, compiles checks and resolves custom check names. Any problem throws
/// <see cref="RulePackException"/> (exit code 3, FR-019). A later pack with the same ID replaces an earlier
/// one, so a local pack loaded with <c>--rule-pack</c> overrides the embedded copy. Not thread-safe; build once.
/// </remarks>
/// <example>
/// <code>
/// ILinter linter = new LinterBuilder()
///     .AddRulePack(new GeneralRulePack())
///     .AddRulePack(new VisaRulePack())
///     .AddAdapter(new StripeAdapter())
///     .Build();
/// </code>
/// </example>
public sealed class LinterBuilder
{
    private readonly List<IRulePackSource> _packs = [];
    private readonly List<IPayloadAdapter> _adapters = [];
    private readonly List<IRule> _rules = [];
    private readonly List<ICustomCheck> _customChecks = [];
    private readonly List<IBatchCheck> _batchChecks = [];
    private TimeProvider _time = TimeProvider.System;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private LintOptions _defaults = LintOptions.Default;
    private IReadOnlyDictionary<string, string>? _lockedVersions;

    /// <summary>Adds a rule pack.</summary>
    /// <param name="source">The pack.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder AddRulePack(IRulePackSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _packs.Add(source);
        return this;
    }

    /// <summary>Adds an input adapter.</summary>
    /// <param name="adapter">The adapter.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder AddAdapter(IPayloadAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapters.Add(adapter);
        return this;
    }

    /// <summary>Adds a rule implemented in code.</summary>
    /// <param name="rule">The rule.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder AddRule(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Adds a custom check that pack rules can name.</summary>
    /// <param name="check">The check.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder AddCustomCheck(ICustomCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _customChecks.Add(check);
        return this;
    }

    /// <summary>Adds a batch check that pack rules can name.</summary>
    /// <param name="check">The check.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder AddBatchCheck(IBatchCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _batchChecks.Add(check);
        return this;
    }

    /// <summary>Sets the clock used for "today" when neither an as-of date nor a transaction date is given.</summary>
    /// <param name="time">The time provider.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder UseTimeProvider(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
        return this;
    }

    /// <summary>Sets the logger factory. Logs never carry payload values.</summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
        return this;
    }

    /// <summary>Sets the options used when a call passes none.</summary>
    /// <param name="defaults">The default options.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder UseDefaults(LintOptions defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        _defaults = defaults;
        return this;
    }

    /// <summary>Requires the loaded packs to match a lock file (<c>.l3lint.lock</c>).</summary>
    /// <param name="packVersions">Pack ID to required version.</param>
    /// <returns>This builder.</returns>
    public LinterBuilder RequirePackVersions(IReadOnlyDictionary<string, string> packVersions)
    {
        ArgumentNullException.ThrowIfNull(packVersions);
        _lockedVersions = packVersions;
        return this;
    }

    /// <summary>
    /// Loads and validates everything and creates the linter.
    /// </summary>
    /// <returns>A thread-safe linter.</returns>
    /// <exception cref="RulePackException">A pack, check or lock requirement is invalid.</exception>
    public ILinter Build()
    {
        var logger = _loggerFactory.CreateLogger("L3Lint.Engine");
        var registry = new CheckRegistry();
        registry.Add(new NoRateProgramCheck());
        foreach (var check in _packs.SelectMany(p => p.CustomChecks).Concat(_customChecks))
        {
            registry.Add(check);
        }

        foreach (var check in _packs.SelectMany(p => p.BatchChecks).Concat(_batchChecks))
        {
            registry.Add(check);
        }

        // Each pack is read, hashed, parsed and schema-validated on its own, so the packs load in parallel (NFR-002,
        // cold start). The results are used in the order the packs were added, and GetResult rethrows the first
        // failure in that order, so the outcome never depends on thread timing (NFR-001).
        var loads = _packs.Select(source => Task.Run(() => RulePackLoader.Load(source))).ToList();
        var loaded = new Dictionary<string, LoadedPack>(StringComparer.Ordinal);
        var policies = new Dictionary<string, ITierPolicy?>(StringComparer.Ordinal);
        foreach (var (source, load) in _packs.Zip(loads))
        {
            var pack = load.GetAwaiter().GetResult();
            loaded[pack.Id] = pack;
            policies[pack.Id] = source.TierPolicy ?? (policies.TryGetValue(pack.Id, out var existing) ? existing : null);
        }

        var lists = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var partial = new List<string>();
        foreach (var pack in loaded.Values.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            foreach (var (name, values) in pack.Lists)
            {
                if (!lists.TryAdd(name, values))
                {
                    throw new RulePackException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackDuplicateList, name), pack.Id, pack.RulesFile, "/pack/lists/" + name);
                }
            }

            partial.AddRange(pack.PartialLists);
        }

        var lookup = new ListLookup(lists, partial);
        var compiled = new List<CompiledRule>();
        var infos = new List<RulePackInfo>();
        foreach (var pack in loaded.Values.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            var rules = RulePackLoader.Compile(pack, registry, lookup);
            compiled.AddRange(rules);
            infos.Add(RulePackLoader.Describe(pack, rules));
        }

        var duplicate = compiled.Select(r => r.Metadata.Id).Concat(_rules.Select(r => r.Metadata.Id))
            .GroupBy(id => id, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new RulePackException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackDuplicateRule, duplicate.Key));
        }

        CheckLock(infos);
        var engine = new LintEngine(
            compiled,
            _rules,
            [.. policies.Values.OfType<ITierPolicy>()],
            lookup,
            new AdapterRegistry(_adapters),
            infos,
            _defaults,
            _time,
            logger);
        return new Linter(engine);
    }

    private void CheckLock(IReadOnlyList<RulePackInfo> packs)
    {
        if (_lockedVersions is null)
        {
            return;
        }

        foreach (var (id, version) in _lockedVersions.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var loaded = packs.FirstOrDefault(p => p.Id == id);
            if (loaded is null || loaded.Version != version)
            {
                throw new RulePackException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.LockMismatch, id, version, loaded?.Version ?? Resources.Strings.LockNotLoaded), id, ".l3lint.lock", null);
            }
        }
    }
}
