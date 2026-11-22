using L3Lint.Core.Model;

namespace L3Lint.Core;

/// <summary>
/// Context values used only when the payload is silent, such as the <c>context</c> section of <c>.l3lint.yaml</c>.
/// </summary>
/// <remarks>
/// Precedence (Interfaces tab): command-line flags (<see cref="LintOptions"/> overrides) beat payload values, which
/// beat these defaults, which beat built-in defaults (ADR-0042).
/// </remarks>
public sealed record ContextDefaults
{
    /// <summary>Gets the default with nothing set.</summary>
    public static ContextDefaults None { get; } = new();

    /// <summary>Gets the brand to use when the payload has none.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>Gets the card product to use when the payload has none.</summary>
    public CardProduct? CardProduct { get; init; }

    /// <summary>Gets the MCC to use when the payload has none.</summary>
    public string? Mcc { get; init; }

    /// <summary>Gets the merchant country to use when the payload has none; the built-in default is US.</summary>
    public string? MerchantCountry { get; init; }
}
