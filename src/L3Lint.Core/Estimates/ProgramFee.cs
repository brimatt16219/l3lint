using L3Lint.Core.Model;

namespace L3Lint.Core.Estimates;

/// <summary>
/// A brand program fee, such as Visa's 0.05% CEDP fee charged whether or not the data validates.
/// </summary>
/// <param name="Brand">The brand.</param>
/// <param name="When">"enhancedDataSent" (the payload carries Level 2 or 3 data) or "always".</param>
/// <param name="Percent">The fee in percent.</param>
/// <param name="Fixed">A fixed fee per transaction.</param>
public sealed record ProgramFee(CardBrand Brand, string When, decimal Percent, decimal Fixed);
