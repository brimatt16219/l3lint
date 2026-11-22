using System.ComponentModel;

namespace L3Lint.Mcp.Tools;

/// <summary>
/// The context a caller can set for <c>lint_payload</c> and <c>suggest_fixes</c>. Values beat the payload, as
/// command-line flags do (Interfaces tab, "Precedence").
/// </summary>
internal sealed record LintContext
{
    [Description("Card brand: auto, visa, mastercard, amex, discover or all. Default: auto (reads card.brand).")]
    public string? Brand { get; init; }

    [Description("Card product: consumer, business, corporate, purchasing, fleet or commercialPrepaid.")]
    public string? CardProduct { get; init; }

    [Description("Merchant category code, 4 digits.")]
    public string? Mcc { get; init; }

    [Description("Merchant country, ISO alpha-2. Default: US.")]
    public string? MerchantCountry { get; init; }

    [Description("ISO 4217 currency for payloads that carry none, such as Braintree requests.")]
    public string? Currency { get; init; }

    [Description("Processor profile name, for example generic, stripe or adyen.")]
    public string? Processor { get; init; }

    [Description("Lint against the rules in force on this date, YYYY-MM-DD. Default: the transaction date, else today.")]
    public string? AsOf { get; init; }
}
