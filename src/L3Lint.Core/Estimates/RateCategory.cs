using L3Lint.Core.Model;

namespace L3Lint.Core.Estimates;

/// <summary>
/// One interchange category in a rate table.
/// </summary>
/// <param name="Brand">The brand.</param>
/// <param name="Products">Card products the category covers; empty means any.</param>
/// <param name="Tier">The tier: product3, product2, cardNotPresent, cardPresent, largeTicket, dataRate3, dataRate2, dataRate1 or standard.</param>
/// <param name="Percent">The rate in percent.</param>
/// <param name="Fixed">The fixed fee per transaction.</param>
/// <param name="Name">A display name.</param>
public sealed record RateCategory(CardBrand Brand, IReadOnlyList<CardProduct> Products, string Tier, decimal Percent, decimal Fixed, string? Name);
