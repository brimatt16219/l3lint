using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Hosting;

/// <summary>
/// Options for <see cref="L3LintServiceCollectionExtensions.AddL3Lint"/>: default lint context plus extensions
/// such as custom rules and adapters (Interfaces tab, "Library API").
/// </summary>
/// <remarks>Configure once at startup; the registered <see cref="ILinter"/> is built from a snapshot.</remarks>
public sealed class L3LintOptions
{
    private readonly List<IRule> _rules = [];
    private readonly List<IPayloadAdapter> _adapters = [];
    private readonly List<ICustomCheck> _customChecks = [];
    private readonly List<IRulePackSource> _packs = [];

    /// <summary>Gets or sets the brand override; <see langword="null"/> reads the payload.</summary>
    public CardBrand? Brand { get; set; }

    /// <summary>Gets or sets a value indicating whether to lint against every brand.</summary>
    public bool AllBrands { get; set; }

    /// <summary>Gets or sets the card product override.</summary>
    public CardProduct? CardProduct { get; set; }

    /// <summary>Gets or sets the MCC override.</summary>
    public string? Mcc { get; set; }

    /// <summary>Gets or sets the merchant country override.</summary>
    public string? MerchantCountry { get; set; }

    /// <summary>Gets or sets the processor profile name; default "generic".</summary>
    public string? Processor { get; set; }

    /// <summary>Gets or sets the as-of date.</summary>
    public DateOnly? AsOf { get; set; }

    /// <summary>Gets or sets the lowest failing severity; <see langword="null"/> never fails.</summary>
    public Severity? FailOn { get; set; } = Severity.Error;

    /// <summary>Gets or sets the arithmetic settings.</summary>
    public ArithmeticOptions Arithmetic { get; set; } = new();

    /// <summary>Gets the rule IDs or globs to skip.</summary>
    public ISet<string> DisabledRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets severity overrides by rule ID or glob.</summary>
    public IDictionary<string, Severity> SeverityOverrides { get; } = new Dictionary<string, Severity>(StringComparer.Ordinal);

    /// <summary>Gets or sets a value indicating whether single-processor rules keep their catalog severity.</summary>
    public bool StrictProfiles { get; set; }

    /// <summary>Gets or sets a value indicating whether a card number stops linting.</summary>
    public bool StrictPci { get; set; }

    /// <summary>Gets or sets the default currency for formats that carry none (Braintree).</summary>
    public string? DefaultCurrency { get; set; }

    /// <summary>
    /// Adds a custom rule with a parameterless constructor.
    /// </summary>
    /// <typeparam name="T">The rule type.</typeparam>
    /// <returns>These options.</returns>
    public L3LintOptions AddRule<T>()
        where T : IRule, new() => AddRule(new T());

    /// <summary>
    /// Adds a custom rule.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <returns>These options.</returns>
    public L3LintOptions AddRule(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a custom adapter.
    /// </summary>
    /// <param name="adapter">The adapter.</param>
    /// <returns>These options.</returns>
    public L3LintOptions AddAdapter(IPayloadAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapters.Add(adapter);
        return this;
    }

    /// <summary>
    /// Adds a custom check that rule packs can name.
    /// </summary>
    /// <param name="check">The check.</param>
    /// <returns>These options.</returns>
    public L3LintOptions AddCustomCheck(ICustomCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        _customChecks.Add(check);
        return this;
    }

    /// <summary>
    /// Adds a rule pack; a pack with the same ID as a shipped pack replaces it.
    /// </summary>
    /// <param name="pack">The pack.</param>
    /// <returns>These options.</returns>
    public L3LintOptions AddRulePack(IRulePackSource pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        _packs.Add(pack);
        return this;
    }

    /// <summary>
    /// Creates the default lint options from these settings.
    /// </summary>
    /// <returns>The lint options.</returns>
    public LintOptions ToLintOptions() => new()
    {
        Brand = Brand,
        AllBrands = AllBrands,
        CardProduct = CardProduct,
        Mcc = Mcc,
        MerchantCountry = MerchantCountry,
        Processor = Processor,
        AsOf = AsOf,
        FailOn = FailOn,
        Arithmetic = Arithmetic,
        DisabledRules = new HashSet<string>(DisabledRules, StringComparer.Ordinal),
        SeverityOverrides = new Dictionary<string, Severity>(SeverityOverrides, StringComparer.Ordinal),
        StrictProfiles = StrictProfiles,
        StrictPci = StrictPci,
        Adapter = new AdapterOptions { DefaultCurrency = DefaultCurrency },
    };

    internal LinterBuilder Configure(LinterBuilder builder)
    {
        foreach (var pack in _packs)
        {
            builder.AddRulePack(pack);
        }

        foreach (var rule in _rules)
        {
            builder.AddRule(rule);
        }

        foreach (var adapter in _adapters)
        {
            builder.AddAdapter(adapter);
        }

        foreach (var check in _customChecks)
        {
            builder.AddCustomCheck(check);
        }

        return builder.UseDefaults(ToLintOptions());
    }
}
