using L3Lint.Core.Adapters;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>Everything the engine knows about one transaction while its rules run.</summary>
internal sealed class TransactionState
{
    public required RunContext Run { get; init; }

    public required Transaction Transaction { get; init; }

    public required DerivedFacts Facts { get; init; }

    public required DateOnly AsOf { get; init; }

    public required IReadOnlyList<CardBrand> Brands { get; init; }

    public required int TransactionIndex { get; init; }

    public required PathMap Paths { get; init; }

    /// <summary>Sortable position in the run: the run index the caller reserved for the transaction.</summary>
    public long OrderKey { get; init; }

    /// <summary>The 1-based line where the transaction starts in its file.</summary>
    public int FirstLine { get; init; } = 1;

    public IReadOnlyList<CardNumberMatch> CardNumberMatches { get; init; } = [];

    public IReadOnlyList<SchemaIssue> SchemaIssues { get; init; } = [];

    public IReadOnlySet<string> InvalidFields { get; init; } = new HashSet<string>();

    public bool HasInvalidFields => InvalidFields.Count > 0;

    /// <summary>True when the pointer, or an object or array that contains it, held a value of the wrong type.</summary>
    public bool IsInvalid(string pointer)
    {
        if (InvalidFields.Count == 0)
        {
            return false;
        }

        var current = pointer;
        while (current.Length > 0)
        {
            if (InvalidFields.Contains(current))
            {
                return true;
            }

            var slash = current.LastIndexOf('/');
            current = slash <= 0 ? string.Empty : current[..slash];
        }

        return false;
    }
}
