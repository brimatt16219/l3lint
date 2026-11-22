using L3Lint.Core.Engine;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;
using L3Lint.Core.Profiles;

namespace L3Lint.Core.Rules;

/// <summary>
/// Everything a rule may read about one transaction: the effective transaction, derived facts, options, the
/// processor profile, the as-of date and the brand being evaluated.
/// </summary>
/// <remarks>
/// The context is a small value passed by reference; it holds no mutable state. The transaction it carries
/// is the effective one: context overrides are applied and card numbers are masked (ADR-0042, ADR-0004).
/// </remarks>
public readonly struct RuleContext : IEquatable<RuleContext>
{
    private readonly TransactionState _state;

    internal RuleContext(TransactionState state, CardBrand brand)
    {
        _state = state;
        Brand = brand;
    }

    /// <summary>Gets the effective transaction.</summary>
    public Transaction Transaction => _state.Transaction;

    /// <summary>Gets values computed once per transaction: expected totals, tax ratio, normalized text.</summary>
    public DerivedFacts Facts => _state.Facts;

    /// <summary>Gets the run's options.</summary>
    public LintOptions Options => _state.Run.Options;

    /// <summary>Gets the selected processor profile.</summary>
    public ProcessorProfile Profile => _state.Run.Profile;

    /// <summary>Gets the date whose rules apply.</summary>
    public DateOnly AsOf => _state.AsOf;

    /// <summary>Gets the brand this rule runs for; for general rules, the payload's brand.</summary>
    public CardBrand Brand { get; }

    /// <summary>Gets every brand evaluated for this transaction.</summary>
    public IReadOnlyList<CardBrand> EvaluatedBrands => _state.Brands;

    /// <summary>Gets the 0-based index of the transaction in the run.</summary>
    public int TransactionIndex => _state.TransactionIndex;

    /// <summary>Gets the named lists loaded from rule packs, such as MCC exclusion lists.</summary>
    public IListLookup Lists => _state.Run.Lists;

    /// <summary>Gets the card numbers the scrub stage masked in this transaction's document.</summary>
    public IReadOnlyList<CardNumberMatch> CardNumberMatches => _state.CardNumberMatches;

    internal TransactionState State => _state;

    /// <summary>Tests two contexts for equality.</summary>
    /// <param name="left">The first context.</param>
    /// <param name="right">The second context.</param>
    /// <returns><see langword="true"/> when both describe the same transaction state and brand.</returns>
    public static bool operator ==(RuleContext left, RuleContext right) => left.Equals(right);

    /// <summary>Tests two contexts for inequality.</summary>
    /// <param name="left">The first context.</param>
    /// <param name="right">The second context.</param>
    /// <returns><see langword="true"/> when they differ.</returns>
    public static bool operator !=(RuleContext left, RuleContext right) => !left.Equals(right);

    /// <summary>
    /// Tells whether a canonical value was present with the wrong type. Checks skip such values (ADR-0036).
    /// </summary>
    /// <param name="canonicalPointer">A concrete canonical pointer, for example /lineItems/2/quantity.</param>
    /// <returns><see langword="true"/> when the value, or an object containing it, was invalid.</returns>
    public bool IsInvalid(string canonicalPointer) => _state.IsInvalid(canonicalPointer);

    /// <inheritdoc/>
    public bool Equals(RuleContext other) => ReferenceEquals(_state, other._state) && Brand == other.Brand;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RuleContext other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_state, Brand);
}
