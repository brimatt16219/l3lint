using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Core;

/// <summary>
/// The exact strings l3lint uses for its enumerations in YAML packs, JSON output and SARIF properties.
/// </summary>
public static class WireNames
{
    /// <summary>Gets the name of a severity: "error", "warning" or "information".</summary>
    /// <param name="value">The severity.</param>
    /// <returns>The wire name.</returns>
    public static string Of(Severity value) => value switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "information",
    };

    /// <summary>Gets the name of a confidence label, for example "processor-doc".</summary>
    /// <param name="value">The label.</param>
    /// <returns>The wire name.</returns>
    public static string Of(Confidence value) => value switch
    {
        Confidence.PublicBrand => "public-brand",
        Confidence.ProcessorDoc => "processor-doc",
        Confidence.Inferred => "inferred",
        _ => "policy",
    };

    /// <summary>Gets the name of an adapter note kind, for example "defaulted".</summary>
    /// <param name="value">The note kind.</param>
    /// <returns>The wire name.</returns>
    public static string Of(Adapters.AdapterNoteKind value) => value switch
    {
        Adapters.AdapterNoteKind.Ignored => "ignored",
        Adapters.AdapterNoteKind.Ambiguous => "ambiguous",
        Adapters.AdapterNoteKind.Derived => "derived",
        Adapters.AdapterNoteKind.Invalid => "invalid",
        _ => "defaulted",
    };

    /// <summary>Gets the name of an impact class, for example "blocksLevel3".</summary>
    /// <param name="value">The impact.</param>
    /// <returns>The wire name.</returns>
    public static string Of(Impact value) => value switch
    {
        Impact.BlocksLevel3 => "blocksLevel3",
        Impact.BlocksLevel2 => "blocksLevel2",
        Impact.RiskVisaValidation => "riskVisaValidation",
        Impact.Truncation => "truncation",
        Impact.NoBenefit => "noBenefit",
        _ => "hygiene",
    };

    /// <summary>Gets the name of a rule level: "general", "L2" or "L3".</summary>
    /// <param name="value">The level.</param>
    /// <returns>The wire name.</returns>
    public static string Of(RuleLevel value) => value switch
    {
        RuleLevel.L2 => "L2",
        RuleLevel.L3 => "L3",
        _ => "general",
    };

    /// <summary>Gets the name of a tier status, for example "atRisk".</summary>
    /// <param name="value">The status.</param>
    /// <returns>The wire name.</returns>
    public static string Of(TierStatus value) => value switch
    {
        TierStatus.Meets => "meets",
        TierStatus.AtRisk => "atRisk",
        TierStatus.Blocked => "blocked",
        _ => "noBenefit",
    };

    /// <summary>Parses a severity name.</summary>
    /// <param name="text">"error", "warning" or "information" (case-insensitive).</param>
    /// <param name="severity">The severity when recognized.</param>
    /// <returns><see langword="true"/> when recognized.</returns>
    public static bool TryParseSeverity(string? text, out Severity severity)
    {
        (var ok, severity) = text?.ToUpperInvariant() switch
        {
            "ERROR" => (true, Severity.Error),
            "WARNING" or "WARN" => (true, Severity.Warning),
            "INFORMATION" or "INFO" => (true, Severity.Information),
            _ => (false, Severity.Information),
        };
        return ok;
    }
}
