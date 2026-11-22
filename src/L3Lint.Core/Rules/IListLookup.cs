namespace L3Lint.Core.Rules;

/// <summary>
/// Named lists loaded from rule packs (MCC lists, unit-of-measure lists, placeholder terms).
/// </summary>
/// <remarks>List names are unique across loaded packs. MCC ranges such as 3000 to 3999 are expanded at load.</remarks>
public interface IListLookup
{
    /// <summary>Gets the names of all loaded lists.</summary>
    IReadOnlyCollection<string> Names { get; }

    /// <summary>Tests membership.</summary>
    /// <param name="listName">The list name, for example visaProduct3ExcludedMcc.</param>
    /// <param name="value">The value to look up.</param>
    /// <param name="ignoreCase">Whether to compare ignoring ASCII case.</param>
    /// <returns><see langword="true"/> when the list exists and holds the value.</returns>
    bool Contains(string listName, string value, bool ignoreCase = false);

    /// <summary>Gets a list's values.</summary>
    /// <param name="listName">The list name.</param>
    /// <returns>The values, or an empty set when the list does not exist.</returns>
    IReadOnlySet<string> Values(string listName);

    /// <summary>Tells whether a list is marked partial (not fully confirmed from public sources, ADR-0025).</summary>
    /// <param name="listName">The list name.</param>
    /// <returns><see langword="true"/> when the pack marks the list as partial.</returns>
    bool IsPartial(string listName);
}
