using L3Lint.Core.Model;

namespace L3Lint.Core.Rules;

/// <summary>
/// Summary of a loaded rule pack, for <c>rules list</c>, <c>version</c> and result headers (US-11).
/// </summary>
/// <param name="Id">The pack ID, for example l3lint.rules.visa.</param>
/// <param name="Version">The pack's semantic version.</param>
/// <param name="Brand">The pack's brand, or <see langword="null"/> for the general pack.</param>
/// <param name="PublishedOn">The pack's publication date.</param>
/// <param name="RuleCount">How many rules the pack holds.</param>
/// <param name="SourceCount">How many distinct source URLs its rules cite.</param>
/// <param name="EarliestEffective">The earliest rule start date, or <see langword="null"/> when every rule applies always.</param>
/// <param name="LatestEnd">The latest rule end date, or <see langword="null"/> when no rule ends.</param>
/// <param name="Origin">"embedded", or the folder of a local pack.</param>
public sealed record RulePackInfo(
    string Id,
    string Version,
    CardBrand? Brand,
    DateOnly PublishedOn,
    int RuleCount,
    int SourceCount,
    DateOnly? EarliestEffective,
    DateOnly? LatestEnd,
    string Origin);
