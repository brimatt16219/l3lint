namespace L3Lint.Core.Profiles;

/// <summary>
/// Limits and rule toggles for one processor or gateway (FR-011, ADR-0017).
/// </summary>
/// <remarks>Every limit cites the public page it comes from. A limit a profile does not set falls back to the rule's default.</remarks>
public sealed record ProcessorProfile
{
    /// <summary>Gets the profile ID, for example "stripe".</summary>
    public required string Id { get; init; }

    /// <summary>Gets the display title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets a description of what the profile covers.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the public pages the profile's values come from.</summary>
    public IReadOnlyList<Uri> Sources { get; init; } = [];

    /// <summary>Gets the limits keyed by profile key, for example maxLineItems.</summary>
    public IReadOnlyDictionary<string, ProfileLimit> Limits { get; init; } = new Dictionary<string, ProfileLimit>();

    /// <summary>Gets rule IDs or globs this profile turns off.</summary>
    public IReadOnlyList<string> DisabledRules { get; init; } = [];

    /// <summary>Gets severity changes keyed by rule ID or glob.</summary>
    public IReadOnlyDictionary<string, Severity> SeverityOverrides { get; init; } = new Dictionary<string, Severity>();

    /// <summary>
    /// Gets a limit value.
    /// </summary>
    /// <param name="key">The profile key.</param>
    /// <returns>The value, or <see langword="null"/> when the profile does not set it.</returns>
    public int? GetLimit(string key) => Limits.TryGetValue(key, out var limit) ? limit.Value : null;
}
