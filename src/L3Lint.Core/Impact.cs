namespace L3Lint.Core;

/// <summary>
/// What a finding means for the interchange tier (Architecture tab, "Impact classes").
/// </summary>
public enum Impact
{
    /// <summary>The payload cannot reach Level 3, Product 3 or Data Rate III.</summary>
    BlocksLevel3,

    /// <summary>The payload cannot reach Level 2 or Data Rate II either.</summary>
    BlocksLevel2,

    /// <summary>Fields are present but likely to fail Visa's quality review.</summary>
    RiskVisaValidation,

    /// <summary>Data will be cut or altered in transit; no rate effect expected.</summary>
    Truncation,

    /// <summary>No enhanced-data rate exists for this card, brand or MCC.</summary>
    NoBenefit,

    /// <summary>An input problem or safety issue.</summary>
    Hygiene,
}
