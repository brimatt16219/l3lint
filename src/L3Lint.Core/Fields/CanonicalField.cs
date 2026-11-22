using L3Lint.Core.Model;

namespace L3Lint.Core.Fields;

/// <summary>The JSON type of a canonical field.</summary>
internal enum FieldKind
{
    Text,
    Decimal,
    Integer,
    Boolean,
    Enum,
    Date,
    TextList,
    Collection,
}

/// <summary>
/// One entry of the canonical field dictionary (Card brand rules tab): where it lives, its type and how to read it.
/// </summary>
/// <param name="Path">Pointer template, for example "/tax/amount" or "/lineItems/*/total".</param>
/// <param name="Kind">The JSON type.</param>
/// <param name="ReadHeader">Reads a transaction-level value; null for line fields.</param>
/// <param name="ReadLine">Reads a line-level value; null for transaction fields.</param>
/// <param name="MissingSentinel">An enum value that required-field rules treat as missing (ADR-0013).</param>
/// <param name="IsMoney">Whether the value is an amount in major units subject to the currency exponent.</param>
/// <param name="IsCode">Whether the field holds a code, where a card-number match is only a warning (ADR-0023).</param>
/// <param name="EnumValues">Allowed JSON values for enum fields.</param>
/// <param name="Constraint">Resource key describing the schema constraint, for schema finding messages.</param>
internal sealed record CanonicalField(
    string Path,
    FieldKind Kind,
    Func<Transaction, FieldValue>? ReadHeader,
    Func<LineItem, FieldValue>? ReadLine,
    string? MissingSentinel = null,
    bool IsMoney = false,
    bool IsCode = false,
    IReadOnlyList<string>? EnumValues = null,
    string? Constraint = null)
{
    public bool IsLineField => ReadLine is not null;

    /// <summary>Gets the last pointer segment, for example "total".</summary>
    public string Name => Path[(Path.LastIndexOf('/') + 1)..];
}
