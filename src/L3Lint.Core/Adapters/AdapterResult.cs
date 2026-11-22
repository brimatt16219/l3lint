using L3Lint.Core.Model;
using L3Lint.Core.Pci;

namespace L3Lint.Core.Adapters;

/// <summary>
/// One transaction produced by an adapter, with everything needed to report findings in the source format.
/// </summary>
/// <remarks>Immutable. Adapters build one result per transaction.</remarks>
public sealed record AdapterResult
{
    /// <summary>Initializes a new result.</summary>
    /// <param name="transaction">The canonical transaction.</param>
    /// <param name="paths">Canonical pointer to source pointer map.</param>
    public AdapterResult(Transaction transaction, PathMap paths)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(paths);
        Transaction = transaction;
        Paths = paths;
    }

    /// <summary>Gets the canonical transaction.</summary>
    public Transaction Transaction { get; }

    /// <summary>Gets the map from canonical pointers to source pointers (FR-007).</summary>
    public PathMap Paths { get; }

    /// <summary>
    /// Gets canonical pointer templates whose values the adapter computed rather than read, for example
    /// "/lineItems/*/total" for Stripe. Arithmetic rules skip derived values instead of trivially passing them.
    /// </summary>
    public IReadOnlySet<string> DerivedFields { get; init; } = new HashSet<string>();

    /// <summary>Gets notes about ignored, ambiguous, defaulted or invalid source values.</summary>
    public IReadOnlyList<AdapterNote> Notes { get; init; } = [];

    /// <summary>
    /// Gets concrete canonical pointers whose source values were present but had the wrong type. Checks skip
    /// these fields so one bad value produces one finding (ADR-0036).
    /// </summary>
    public IReadOnlySet<string> InvalidFields { get; init; } = new HashSet<string>();

    /// <summary>Gets card numbers masked by the adapter itself (CSV only; JSON documents are scrubbed before mapping).</summary>
    public IReadOnlyList<CardNumberMatch> CardNumberMatches { get; init; } = [];

    /// <summary>Gets the 1-based line in the input file where this transaction starts, when the adapter knows it (CSV).</summary>
    public int? FirstLine { get; init; }
}
