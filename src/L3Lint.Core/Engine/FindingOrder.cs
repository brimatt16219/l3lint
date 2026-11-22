namespace L3Lint.Core.Engine;

/// <summary>
/// The deterministic finding order (NFR-001): transaction index, canonical path (segment by segment, numbers
/// compared as numbers so /lineItems/2 sorts before /lineItems/10), rule ID, then message.
/// </summary>
internal sealed class FindingOrder : IComparer<Finding>
{
    public static FindingOrder Instance { get; } = new();

    public int Compare(Finding? x, Finding? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var result = x.TransactionIndex.CompareTo(y.TransactionIndex);
        if (result != 0)
        {
            return result;
        }

        result = ComparePointers(x.Path.Canonical, y.Path.Canonical);
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(x.RuleId, y.RuleId);
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(x.Path.Source, y.Path.Source);
        return result != 0 ? result : string.CompareOrdinal(x.Message, y.Message);
    }

    public static int ComparePointers(string a, string b) => Fields.PointerComparer.Instance.Compare(a, b);
}
