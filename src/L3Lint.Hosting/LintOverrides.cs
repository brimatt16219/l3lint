namespace L3Lint.Hosting;

/// <summary>
/// Values given on the command line, in an MCP call or as GitHub Action inputs. They beat the payload and the
/// config file (Interfaces tab, "Precedence").
/// </summary>
public sealed record LintOverrides
{
    /// <summary>Gets the input format: auto, canonical, a gateway name or csv.</summary>
    public string? Format { get; init; }

    /// <summary>Gets the CSV column map path.</summary>
    public string? CsvMap { get; init; }

    /// <summary>Gets the currency for payloads that carry none (<c>--currency</c>).</summary>
    public string? Currency { get; init; }

    /// <summary>Gets the brand: auto, visa, mastercard, amex, discover or all.</summary>
    public string? Brand { get; init; }

    /// <summary>Gets the card product.</summary>
    public string? CardProduct { get; init; }

    /// <summary>Gets the MCC (4 digits).</summary>
    public string? Mcc { get; init; }

    /// <summary>Gets the merchant country (ISO alpha-2).</summary>
    public string? MerchantCountry { get; init; }

    /// <summary>Gets the processor profile name or file path.</summary>
    public string? Processor { get; init; }

    /// <summary>Gets the as-of date.</summary>
    public DateOnly? AsOf { get; init; }

    /// <summary>Gets local rule pack folders.</summary>
    public IReadOnlyList<string> RulePacks { get; init; } = [];

    /// <summary>Gets rules to run exclusively (IDs or globs).</summary>
    public IReadOnlyList<string> Only { get; init; } = [];

    /// <summary>Gets rules to skip (IDs or globs).</summary>
    public IReadOnlyList<string> Disable { get; init; } = [];

    /// <summary>Gets severity overrides as "RULE=level".</summary>
    public IReadOnlyList<string> Severity { get; init; } = [];

    /// <summary>Gets the fail-on level: error, warning, information or none.</summary>
    public string? FailOn { get; init; }

    /// <summary>Gets output targets, each "kind" or "kind=path".</summary>
    public IReadOnlyList<string> Outputs { get; init; } = [];

    /// <summary>Gets the most findings to print.</summary>
    public int? MaxFindings { get; init; }

    /// <summary>Gets the rate table path.</summary>
    public string? Rates { get; init; }

    /// <summary>Gets a value indicating whether a card number stops processing.</summary>
    public bool StrictPci { get; init; }

    /// <summary>Gets a value indicating whether single-processor rules keep their catalog severity.</summary>
    public bool StrictProfiles { get; init; }

    /// <summary>Gets a value indicating whether adapter notes are collected.</summary>
    public bool Verbose { get; init; }
}
