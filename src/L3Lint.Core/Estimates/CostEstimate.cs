namespace L3Lint.Core.Estimates;

/// <summary>
/// Estimated interchange cost for one transaction, from the user's rate table. Always an estimate: real
/// interchange also depends on factors l3lint does not see.
/// </summary>
/// <param name="RateTableName">The rate table's name.</param>
/// <param name="Effective">The rate table's effective date.</param>
/// <param name="Currency">The currency of the amounts.</param>
/// <param name="Lines">One estimate per evaluated brand that has categories in the table.</param>
public sealed record CostEstimate(string RateTableName, DateOnly Effective, string Currency, IReadOnlyList<CostEstimateLine> Lines);
