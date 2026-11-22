using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Rules;

namespace L3Lint.Reporting;

/// <summary>
/// Lookups that the SARIF, JUnit and Markdown writers share: transactions by run index, the input each transaction
/// came from, its position in that input, rule metadata by ID, and the transactions grouped by input in run order.
/// </summary>
/// <remarks>
/// The index keeps row numbers, not transactions, and reads transactions back from the report when asked, so it
/// stays small when a batch keeps its results compressed (NFR-003).
/// </remarks>
internal sealed class ReportIndex
{
    private readonly Dictionary<int, int> _rows = [];
    private readonly Dictionary<int, int> _positions = [];
    private readonly Dictionary<string, RuleMetadata> _rules = new(StringComparer.Ordinal);

    public ReportIndex(LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Report = report;
        var counts = new Dictionary<int, int>();
        var byInput = new Dictionary<int, List<int>>();
        var row = 0;
        foreach (var transaction in report.Transactions)
        {
            if (!byInput.TryGetValue(transaction.Input, out var rows))
            {
                rows = [];
                byInput[transaction.Input] = rows;
            }

            rows.Add(row);
            if (_rows.TryAdd(transaction.Index, row))
            {
                var position = counts.GetValueOrDefault(transaction.Input) + 1;
                counts[transaction.Input] = position;
                _positions[transaction.Index] = position;
            }

            row++;
        }

        foreach (var rule in report.Rules)
        {
            _rules.TryAdd(rule.Id, rule);
        }

        Groups = BuildGroups(report, byInput);
    }

    /// <summary>Gets the report.</summary>
    public LintReport Report { get; }

    /// <summary>Gets the inputs in order, each with its transactions in run order, then any transactions whose input is not listed.</summary>
    public IReadOnlyList<InputGroup> Groups { get; }

    /// <summary>Finds a transaction by its run index.</summary>
    public TransactionResult? Transaction(int index) => _rows.TryGetValue(index, out var row) ? Report.Transactions[row] : null;

    /// <summary>Finds a rule's metadata by ID.</summary>
    public RuleMetadata? Rule(string ruleId) => _rules.GetValueOrDefault(ruleId);

    /// <summary>Gets the input path of a transaction as the report gives it, or <see langword="null"/> when unknown.</summary>
    public string? InputPath(TransactionResult transaction) =>
        transaction.Input >= 0 && transaction.Input < Report.Inputs.Count ? Report.Inputs[transaction.Input].Path : transaction.InputName;

    /// <summary>Gets the 1-based position of a transaction within its input.</summary>
    public int PositionInInput(TransactionResult transaction) => _positions.GetValueOrDefault(transaction.Index, 1);

    /// <summary>Gets the display name of a transaction: its ID, or "Transaction N" with the 1-based run index, as in text output.</summary>
    public static string TransactionName(TransactionResult transaction) =>
        transaction.Id ?? string.Format(CultureInfo.InvariantCulture, Resources.Strings.TextTransactionNumber, transaction.Index + 1);

    private static List<InputGroup> BuildGroups(LintReport report, Dictionary<int, List<int>> byInput)
    {
        var groups = new List<InputGroup>(report.Inputs.Count);
        for (var input = 0; input < report.Inputs.Count; input++)
        {
            var rows = byInput.GetValueOrDefault(input);
            groups.Add(new InputGroup(input, report.Inputs[input].Path, rows is null ? [] : new RowView(report.Transactions, rows)));
        }

        foreach (var (input, rows) in byInput.Where(p => p.Key < 0 || p.Key >= report.Inputs.Count).OrderBy(p => p.Key))
        {
            var transactions = new RowView(report.Transactions, rows);
            var name = transactions[0].InputName ?? string.Format(CultureInfo.InvariantCulture, Resources.Strings.ReportInputNumber, input + 1);
            groups.Add(new InputGroup(input, name, transactions));
        }

        return groups;
    }

    /// <summary>Some rows of the report's transactions, read on demand.</summary>
    private sealed class RowView(IReadOnlyList<TransactionResult> source, List<int> rows) : IReadOnlyList<TransactionResult>
    {
        public int Count => rows.Count;

        public TransactionResult this[int index] => source[rows[index]];

        public IEnumerator<TransactionResult> GetEnumerator()
        {
            foreach (var row in rows)
            {
                yield return source[row];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

/// <summary>One input of a report and the transactions read from it, in run order.</summary>
/// <param name="Input">The 0-based input index.</param>
/// <param name="Name">The input path as the report gives it, or a label.</param>
/// <param name="Transactions">The transactions, read from the report when enumerated.</param>
internal sealed record InputGroup(int Input, string Name, IReadOnlyList<TransactionResult> Transactions);
