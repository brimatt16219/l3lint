using System.Collections.Frozen;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>Named lists from all loaded packs. Immutable after load.</summary>
internal sealed class ListLookup : IListLookup
{
    private static readonly FrozenSet<string> Empty = FrozenSet<string>.Empty;

    private readonly FrozenDictionary<string, FrozenSet<string>> _exact;
    private readonly FrozenDictionary<string, FrozenSet<string>> _ignoreCase;
    private readonly FrozenSet<string> _partial;

    public ListLookup(IReadOnlyDictionary<string, IReadOnlyList<string>> lists, IEnumerable<string> partial)
    {
        _exact = lists.ToFrozenDictionary(l => l.Key, l => l.Value.ToFrozenSet(StringComparer.Ordinal), StringComparer.Ordinal);
        _ignoreCase = lists.ToFrozenDictionary(l => l.Key, l => l.Value.ToFrozenSet(StringComparer.OrdinalIgnoreCase), StringComparer.Ordinal);
        _partial = partial.ToFrozenSet(StringComparer.Ordinal);
        Names = [.. _exact.Keys.Order(StringComparer.Ordinal)];
    }

    public IReadOnlyCollection<string> Names { get; }

    public bool Contains(string listName, string value, bool ignoreCase = false) =>
        (ignoreCase ? _ignoreCase : _exact).TryGetValue(listName, out var set) && set.Contains(value);

    public IReadOnlySet<string> Values(string listName) => _exact.TryGetValue(listName, out var set) ? set : Empty;

    public bool IsPartial(string listName) => _partial.Contains(listName);

    public bool Exists(string listName) => _exact.ContainsKey(listName);
}
