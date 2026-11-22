namespace L3Lint.Adapters.Json;

/// <summary>
/// How a gateway writes one value: the JSON type it uses, its units, and how a canonical value is written
/// back for fix patches (FR-007, FR-008).
/// </summary>
internal enum Shape
{
    /// <summary>A JSON string, copied as is.</summary>
    Text,

    /// <summary>A JSON integer in minor units (Stripe, Checkout.com, Adyen structured object).</summary>
    MinorUnits,

    /// <summary>A JSON string holding an integer in minor units (Adyen legacy additionalData keys).</summary>
    MinorUnitsText,

    /// <summary>A decimal in major units, sent as a JSON number or a decimal string; written back as a string.</summary>
    DecimalText,

    /// <summary>A JSON integer (Stripe, Checkout.com and Adyen structured quantities).</summary>
    WholeNumber,

    /// <summary>A JSON string holding an integer (Adyen legacy quantity).</summary>
    WholeNumberText,

    /// <summary>A JSON boolean, or the text "true" or "false"; written back as a boolean.</summary>
    Flag,

    /// <summary>A JSON boolean, or the text "true" or "false"; written back as text (Authorize.net).</summary>
    FlagText,

    /// <summary>A JSON boolean, "true", "false", "Y" or "N" (Cybersource amountIncludesTax).</summary>
    YesNoFlag,

    /// <summary>A JSON boolean that means the opposite of the canonical flag (Checkout.com items[].tax_exempt).</summary>
    InverseFlag,

    /// <summary>A date written yyyy-MM-dd.</summary>
    Date,

    /// <summary>A date written ddMMyy (Adyen legacy orderDate).</summary>
    DateDayMonthYear,

    /// <summary>An ISO 3166-1 alpha-2 or alpha-3 country code; alpha-3 is converted to alpha-2.</summary>
    Country,

    /// <summary>An ISO 4217 currency code, copied as is.</summary>
    Currency,

    /// <summary>A lowercase ISO 4217 currency code (Stripe), converted to uppercase.</summary>
    LowercaseCurrency,

    /// <summary>A tax exempt flag (Braintree): true means exempt, false means taxable; written back as a boolean.</summary>
    TaxExempt,

    /// <summary>A tax exempt flag (Authorize.net): true means exempt, false means taxable; written back as text.</summary>
    TaxExemptText,

    /// <summary>A taxable flag (Cybersource invoiceDetails.taxable): true means taxable, false means exempt.</summary>
    Taxable,

    /// <summary>The text "debit" or "credit" (Braintree lineItems[].kind).</summary>
    DebitCredit,
}
