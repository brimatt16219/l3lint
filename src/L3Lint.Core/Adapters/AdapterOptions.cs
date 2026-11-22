using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Options passed to adapters.
/// </summary>
public sealed record AdapterOptions
{
    /// <summary>Gets the default options.</summary>
    public static AdapterOptions Default { get; } = new();

    /// <summary>
    /// Gets the ISO 4217 currency to use when the format does not carry one (Braintree, sometimes
    /// Authorize.net). Absent means the transaction has no currency and GEN-FMT-002 reports it.
    /// </summary>
    public string? DefaultCurrency { get; init; }

    /// <summary>Gets the YAML text of a CSV column map (required for CSV input).</summary>
    public string? CsvMap { get; init; }

    /// <summary>Gets the name of the CSV map file, used in messages.</summary>
    public string? CsvMapName { get; init; }

    /// <summary>Gets the input limits.</summary>
    public InputLimits Limits { get; init; } = InputLimits.Default;

    /// <summary>Gets a value indicating whether adapters should report ignored source fields as notes.</summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Gets canonical pointer patterns exempt from card-number masking (<c>pci.allowFields</c>), for adapters that
    /// mask their own input (CSV). A <c>*</c> segment matches any line index.
    /// </summary>
    public IReadOnlyList<string> PciAllowFields { get; init; } = [];
}
