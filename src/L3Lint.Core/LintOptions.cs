using L3Lint.Core.Adapters;
using L3Lint.Core.Estimates;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Profiles;

namespace L3Lint.Core;

/// <summary>
/// Context and settings for one lint run.
/// </summary>
/// <remarks>
/// Values set here beat values in the payload, which beat defaults (Interfaces tab, "Precedence"). Each
/// override that changes a payload value adds a note to the result (ADR-0042). Instances are immutable;
/// use <c>with</c> expressions to vary them.
/// </remarks>
public sealed record LintOptions
{
    /// <summary>Gets the default options: auto brand, generic profile, fail on errors.</summary>
    public static LintOptions Default { get; } = new();

    /// <summary>Gets the brand to lint against; <see langword="null"/> reads <c>card.brand</c> from the payload.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>Gets a value indicating whether to lint against every brand pack (<c>--brand all</c>).</summary>
    public bool AllBrands { get; init; }

    /// <summary>Gets the card product override; <see langword="null"/> uses the payload.</summary>
    public CardProduct? CardProduct { get; init; }

    /// <summary>Gets the merchant category code override; <see langword="null"/> uses the payload.</summary>
    public string? Mcc { get; init; }

    /// <summary>Gets the merchant country override (<c>--merchant-country</c>); <see langword="null"/> uses the payload, then defaults, then US.</summary>
    public string? MerchantCountry { get; init; }

    /// <summary>Gets context values used only when the payload is silent (the config file's <c>context</c> section).</summary>
    public ContextDefaults Defaults { get; init; } = ContextDefaults.None;

    /// <summary>Gets the processor profile name, for example "stripe"; <see langword="null"/> means "generic".</summary>
    public string? Processor { get; init; }

    /// <summary>Gets a custom profile loaded from a file; it takes precedence over <see cref="Processor"/>.</summary>
    public ProcessorProfile? ProcessorProfile { get; init; }

    /// <summary>Gets the date whose rules apply; <see langword="null"/> uses the transaction date, else today.</summary>
    public DateOnly? AsOf { get; init; }

    /// <summary>Gets the lowest severity that makes a run fail (exit code 1); <see langword="null"/> never fails.</summary>
    public Severity? FailOn { get; init; } = Severity.Error;

    /// <summary>Gets the arithmetic tolerance and rounding settings.</summary>
    public ArithmeticOptions Arithmetic { get; init; } = new();

    /// <summary>Gets rule IDs or globs (such as VISA-DQ-*) to skip.</summary>
    public IReadOnlySet<string> DisabledRules { get; init; } = new HashSet<string>();

    /// <summary>Gets rule IDs or globs to run exclusively; empty runs every rule.</summary>
    public IReadOnlySet<string> OnlyRules { get; init; } = new HashSet<string>();

    /// <summary>Gets severity overrides keyed by rule ID or glob.</summary>
    public IReadOnlyDictionary<string, Severity> SeverityOverrides { get; init; } = new Dictionary<string, Severity>();

    /// <summary>Gets a value indicating whether single-processor rules keep their catalog severity under every profile (ADR-0016).</summary>
    public bool StrictProfiles { get; init; }

    /// <summary>Gets a value indicating whether a card number stops processing instead of being masked.</summary>
    public bool StrictPci { get; init; }

    /// <summary>Gets canonical pointers (with <c>*</c> allowed) exempt from card-number detection after human review.</summary>
    public IReadOnlyList<string> PciAllowFields { get; init; } = [];

    /// <summary>Gets placeholder terms to add to the shipped list.</summary>
    public IReadOnlyList<string> PlaceholdersAdd { get; init; } = [];

    /// <summary>Gets placeholder terms to remove from the shipped list.</summary>
    public IReadOnlyList<string> PlaceholdersRemove { get; init; } = [];

    /// <summary>Gets the batch heuristic thresholds for GEN-DQ-007.</summary>
    public BatchOptions Batch { get; init; } = new();

    /// <summary>Gets the user's rate table; <see langword="null"/> disables cost estimates.</summary>
    public RateTable? Rates { get; init; }

    /// <summary>Gets the input format for raw documents; <see langword="null"/> or "auto" detects it.</summary>
    public string? Format { get; init; }

    /// <summary>Gets options passed to adapters, such as the CSV column map.</summary>
    public AdapterOptions Adapter { get; init; } = AdapterOptions.Default;

    /// <summary>Gets the input limits.</summary>
    public InputLimits Limits { get; init; } = InputLimits.Default;
}
