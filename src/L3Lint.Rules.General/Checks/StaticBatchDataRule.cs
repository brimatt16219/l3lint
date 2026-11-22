using System.Globalization;
using L3Lint.Core;
using L3Lint.Core.Checks;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Rules;

namespace L3Lint.Rules.General.Checks;

/// <summary>
/// GEN-DQ-007 for one run: counts, per description and product code, how many transactions carry it, and
/// reports values found on at least the configured share of at least the configured number of transactions.
/// </summary>
/// <remarks>
/// Memory is bounded: at most <see cref="MaxTrackedValues"/> distinct values are tracked (NFR-003). The finding
/// points at the first transaction in run order that carries the value, so the result does not depend on the
/// order workers finish in (ADR-0034).
/// </remarks>
internal sealed class StaticBatchDataRule(RuleMetadata rule, BatchOptions options) : IBatchRule
{
    public const int MaxTrackedValues = 100_000;

    private static readonly string[] Fields = ["description", "productCode"];

    private readonly Dictionary<(string Field, string Value), Entry> _entries = [];
    private int _transactions;

    public RuleMetadata Metadata => rule;

    public void Observe(in RuleContext context)
    {
        _transactions++;
        var seen = new HashSet<(string, string)>();
        var lines = context.Transaction.LineItems;
        for (var i = 0; i < lines.Count; i++)
        {
            foreach (var field in Fields)
            {
                var value = field == "description" ? lines[i].Description : lines[i].ProductCode;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var key = (field, value.Trim().ToUpperInvariant());
                if (!seen.Add(key))
                {
                    continue;
                }

                var state = context.State;
                if (_entries.TryGetValue(key, out var entry))
                {
                    entry.Count++;
                    if (state.OrderKey < entry.FirstKey)
                    {
                        entry.Set(state.OrderKey, state.TransactionIndex, Pointers.Line(i, field), state.Paths.Resolve(Pointers.Line(i, field))?.Pointer, state.FirstLine, value);
                    }
                }
                else if (_entries.Count < MaxTrackedValues)
                {
                    var created = new Entry { Count = 1 };
                    created.Set(state.OrderKey, state.TransactionIndex, Pointers.Line(i, field), state.Paths.Resolve(Pointers.Line(i, field))?.Pointer, state.FirstLine, value);
                    _entries[key] = created;
                }
            }
        }
    }

    public void Complete(IFindingSink sink)
    {
        if (_transactions < options.MinTransactions)
        {
            return;
        }

        var threshold = options.SameValueShare * _transactions;
        foreach (var entry in _entries.Values.Where(e => e.Count >= threshold).OrderBy(e => e.FirstKey).ThenBy(e => e.Pointer, StringComparer.Ordinal))
        {
            var arguments = CheckArguments.For(entry.Pointer, null);
            arguments["count"] = CheckArguments.Number(entry.Count);
            arguments["transactions"] = CheckArguments.Number(_transactions);
            arguments["share"] = CheckArguments.Percent((decimal)entry.Count / _transactions);
            sink.Report(new FindingDraft(rule, entry.Pointer)
            {
                Actual = entry.Value,
                TransactionIndex = entry.FirstIndex,
                SourcePointer = entry.Source,
                Location = new SourceLocation(entry.FirstLine, 1),
                Arguments = arguments,
                Expected = string.Format(CultureInfo.InvariantCulture, Core.Resources.Strings.ExpectedBatchShare, CheckArguments.Percent(options.SameValueShare)),
            });
        }
    }

    private sealed class Entry
    {
        public int Count { get; set; }

        public long FirstKey { get; private set; }

        public int FirstIndex { get; private set; }

        public string Pointer { get; private set; } = string.Empty;

        public string? Source { get; private set; }

        public int FirstLine { get; private set; }

        public string Value { get; private set; } = string.Empty;

        public void Set(long key, int index, string pointer, string? source, int firstLine, string value)
        {
            FirstKey = key;
            FirstIndex = index;
            Pointer = pointer;
            Source = source;
            FirstLine = firstLine;
            Value = value;
        }
    }
}
