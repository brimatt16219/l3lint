using System.Collections.Frozen;
using System.Globalization;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Maps canonical JSON Pointers to the matching location in the source format (FR-007).
/// </summary>
/// <remarks>
/// <para>
/// Line fields are mapped with functions of the 0-based line index, so the map covers fields the payload
/// did not send: a missing Stripe commodity code still points at
/// <c>/amount_details/line_items/1/payment_method_options/card/commodity_code</c>. A field the format cannot
/// carry resolves to <see langword="null"/>.
/// </para>
/// <para>Instances are immutable; build them with <see cref="PathMapBuilder"/>.</para>
/// </remarks>
public sealed class PathMap
{
    private readonly FrozenDictionary<string, SourceField?> _fields;
    private readonly FrozenDictionary<string, Func<int, SourceField?>> _lineFields;
    private readonly SourceField? _lineItems;
    private readonly Func<int, SourceField?>? _lineItem;
    private readonly SourceField? _adviceText;
    private readonly Func<int, SourceField?>? _adviceLine;
    private readonly bool _identity;

    internal PathMap(
        IDictionary<string, SourceField?> fields,
        IDictionary<string, Func<int, SourceField?>> lineFields,
        SourceField? lineItems,
        Func<int, SourceField?>? lineItem,
        SourceField? adviceText,
        Func<int, SourceField?>? adviceLine,
        bool identity)
    {
        _fields = fields.ToFrozenDictionary(StringComparer.Ordinal);
        _lineFields = lineFields.ToFrozenDictionary(StringComparer.Ordinal);
        _lineItems = lineItems;
        _lineItem = lineItem;
        _adviceText = adviceText;
        _adviceLine = adviceLine;
        _identity = identity;
    }

    /// <summary>Gets a map where every canonical pointer maps to itself (canonical input).</summary>
    public static PathMap Identity { get; } = new(
        new Dictionary<string, SourceField?>(),
        new Dictionary<string, Func<int, SourceField?>>(),
        null,
        null,
        null,
        null,
        identity: true);

    /// <summary>Gets a value indicating whether this map is the identity map.</summary>
    public bool IsIdentity => _identity;

    /// <summary>
    /// Resolves a concrete canonical pointer, such as <c>/lineItems/1/total</c>, to its source location.
    /// </summary>
    /// <param name="canonicalPointer">The canonical pointer.</param>
    /// <returns>The source field, or <see langword="null"/> when the format has no equivalent.</returns>
    public SourceField? Resolve(string canonicalPointer)
    {
        ArgumentNullException.ThrowIfNull(canonicalPointer);
        if (_identity)
        {
            return new SourceField(canonicalPointer);
        }

        if (_fields.TryGetValue(canonicalPointer, out var direct))
        {
            return direct;
        }

        if (TrySplitIndexed(canonicalPointer, "/lineItems", out var index, out var rest))
        {
            if (rest.Length == 0)
            {
                return _lineItem?.Invoke(index);
            }

            return _lineFields.TryGetValue(rest, out var line) ? line(index) : null;
        }

        if (canonicalPointer == "/lineItems")
        {
            return _lineItems;
        }

        if (TrySplitIndexed(canonicalPointer, "/amex/adviceText", out var adviceIndex, out var adviceRest) && adviceRest.Length == 0)
        {
            return _adviceLine?.Invoke(adviceIndex);
        }

        return canonicalPointer == "/amex/adviceText" ? _adviceText : null;
    }

    private static bool TrySplitIndexed(string pointer, string collection, out int index, out string rest)
    {
        index = -1;
        rest = string.Empty;
        if (!pointer.StartsWith(collection + "/", StringComparison.Ordinal))
        {
            return false;
        }

        var tail = pointer.AsSpan(collection.Length + 1);
        var slash = tail.IndexOf('/');
        var number = slash < 0 ? tail : tail[..slash];
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            return false;
        }

        rest = slash < 0 ? string.Empty : tail[(slash + 1)..].ToString();
        return true;
    }
}
