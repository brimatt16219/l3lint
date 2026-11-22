using System.Collections.Frozen;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;

namespace L3Lint.Adapters.Csv;

/// <summary>
/// Maps the rows of one transaction group (rows sharing the <c>groupBy</c> value) to a canonical transaction:
/// one line item per row, transaction-level values that must agree across rows (FR-006, US-14).
/// </summary>
/// <remarks>Not thread-safe; one mapper per group.</remarks>
internal sealed class CsvGroupMapper
{
    private readonly CsvMap _map;
    private readonly CsvTable _table;
    private readonly IReadOnlyDictionary<string, int> _columns;
    private readonly IReadOnlyList<CsvRow> _rows;
    private readonly string _key;
    private readonly MappingLog _log = new();
    private readonly Dictionary<string, object> _header = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SourceField?> _headerSources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _defaulted = new(StringComparer.Ordinal);

    /// <summary>Initializes a mapper for one group.</summary>
    public CsvGroupMapper(CsvMap map, CsvTable table, IReadOnlyDictionary<string, int> columns, IReadOnlyList<CsvRow> rows, string key)
    {
        _map = map;
        _table = table;
        _columns = columns;
        _rows = rows;
        _key = key;
    }

    /// <summary>Gets the log, so the adapter can add notes that belong to the first transaction.</summary>
    public MappingLog Log => _log;

    /// <summary>Builds the transaction, its path map, notes and card number matches.</summary>
    /// <param name="masked">Cells that held card numbers: (row, column) to the brand they matched; the cells are already masked.</param>
    /// <returns>The adapter result.</returns>
    public AdapterResult Build(IReadOnlyDictionary<(CsvRow Row, int Column), CardBrand> masked)
    {
        Currency? minorUnits = null;
        var currencyEntry = _map.Transaction.FirstOrDefault(f => f.Target == "/currency");
        if (currencyEntry is not null)
        {
            MapHeader(currencyEntry, null);
        }

        if (_map.MinorUnits)
        {
            var code = _header.GetValueOrDefault("/currency") as string;
            if (Iso4217.TryGetCurrency(code, out var currency))
            {
                minorUnits = currency;
            }
            else
            {
                minorUnits = new Currency(code ?? string.Empty, JsonAdapterSupport.AssumedExponent);
                _log.Add(AdapterNoteKind.Ambiguous, null, "/currency", Messages.Format(Resources.Strings.NoteCsvCurrencyAssumed, _key, JsonAdapterSupport.AssumedExponent));
            }
        }

        foreach (var entry in _map.Transaction)
        {
            if (entry != currencyEntry)
            {
                MapHeader(entry, minorUnits);
            }
        }

        var lines = new List<LineItem>(_rows.Count);
        for (var i = 0; i < _rows.Count; i++)
        {
            lines.Add(MapLine(i, minorUnits));
        }

        var transaction = Assemble(lines);
        return new AdapterResult(transaction, BuildPaths())
        {
            DerivedFields = FrozenSet<string>.Empty,
            Notes = [.. _log.Notes],
            InvalidFields = _log.Invalid.ToFrozenSet(StringComparer.Ordinal),
            CardNumberMatches = Matches(masked),
            FirstLine = _rows[0].Line,
        };
    }

    /// <summary>Builds the "row 14, column F (unit_price)" label of a cell.</summary>
    public static string Label(CsvTable table, CsvRow row, int column) =>
        Messages.Format(Resources.Strings.CsvCellLabel, row.Line, CsvTable.ColumnLetters(column + 1), table.Header[column]);

    private static bool IsAmount(CanonicalField field) => field.IsMoney || field.Path == "/lineItems/*/unitCost";

    private void MapHeader(CsvFieldMap entry, Currency? minorUnits)
    {
        var target = entry.Target;
        if (!CanonicalFields.TryGet(target, out var field))
        {
            return;
        }

        // Rows must agree on values they send; an empty cell (or one filled from the map's default) does not
        // contradict a row that sends the value.
        var column = Column(entry.Source);
        CsvRow? chosen = null;
        string? value = null;
        var usedDefault = false;
        foreach (var row in _rows)
        {
            var (text, defaulted) = Resolve(entry.Source, row);
            if (text is null || (defaulted && chosen is not null))
            {
                continue;
            }

            if (chosen is null || (usedDefault && !defaulted))
            {
                (chosen, value, usedDefault) = (row, text, defaulted);
                continue;
            }

            if (!string.Equals(text, value, StringComparison.Ordinal))
            {
                var name = column is { } index ? _table.Header[index] : target;
                var source = column is { } at ? Label(_table, row, at) : null;
                _log.AddInvalid(source, target, Messages.Format(Resources.Strings.CsvRowsDisagree, chosen.Line, row.Line, _key, name));
                _headerSources[target] = Cell(row, column);
                return;
            }
        }

        _headerSources[target] = Cell(chosen ?? _rows[0], column);
        if (value is null)
        {
            return;
        }

        var label = column is { } c ? Label(_table, chosen!, c) : entry.MapPointer;
        if (usedDefault)
        {
            Defaulted(label, entry);
        }

        Convert(field, value, IsAmount(field) ? minorUnits : null, label, target, converted => _header[target] = converted);
    }

    private LineItem MapLine(int index, Currency? minorUnits)
    {
        var row = _rows[index];
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var entry in _map.Lines)
        {
            if (!CanonicalFields.TryGet("/lineItems/*/" + entry.Target, out var field))
            {
                continue;
            }

            var (text, defaulted) = Resolve(entry.Source, row);
            if (text is null)
            {
                continue;
            }

            var column = Column(entry.Source);
            var label = column is { } c ? Label(_table, row, c) : entry.MapPointer;
            if (defaulted)
            {
                Defaulted(label, entry);
            }

            var pointer = CanonicalAssembler.LinePointer(index, entry.Target);
            Convert(field, text, IsAmount(field) ? minorUnits : null, label, pointer, converted => values[entry.Target] = converted);
        }

        T? Get<T>(string name)
            where T : struct => values.TryGetValue(name, out var value) ? (T)value : null;
        string? Text(string name) => values.TryGetValue(name, out var value) ? (string)value : null;

        return new LineItem
        {
            // Without a mapped sequence column, lines are numbered by row order within the group (ADR-0008).
            Sequence = _map.Lines.Any(f => f.Target == "sequence") ? Get<int>("sequence") : index + 1,
            Description = Text("description"),
            ProductCode = Text("productCode"),
            CommodityCode = Text("commodityCode"),
            CommodityScheme = Get<CommodityScheme>("commodityScheme"),
            Quantity = Get<decimal>("quantity"),
            UnitOfMeasure = Text("unitOfMeasure"),
            UnitCost = Get<decimal>("unitCost"),
            DiscountAmount = Get<decimal>("discountAmount"),
            DiscountIndicator = Get<bool>("discountIndicator"),
            TaxAmount = Get<decimal>("taxAmount"),
            TaxRate = Get<decimal>("taxRate"),
            Taxable = Get<bool>("taxable"),
            TaxTypeCode = Text("taxTypeCode"),
            AmountIncludesTax = Get<bool>("amountIncludesTax"),
            Total = Get<decimal>("total"),
            DebitCreditIndicator = Get<DebitCredit>("debitCreditIndicator"),
            DetailIndicator = Text("detailIndicator"),
        };
    }

    private Transaction Assemble(List<LineItem> lines)
    {
        T? Get<T>(string pointer)
            where T : struct => _header.TryGetValue(pointer, out var value) ? (T)value : null;
        string? Text(string pointer) => _header.TryGetValue(pointer, out var value) ? (string)value : null;

        return new Transaction
        {
            Id = Text("/id"),
            Amount = Get<decimal>("/amount"),
            Currency = Text("/currency"),
            TransactionDate = Get<DateOnly>("/transactionDate"),
            Channel = Get<Channel>("/channel"),
            Card = new CardInfo
            {
                Brand = Get<CardBrand>("/card/brand"),
                Product = Get<CardProduct>("/card/product"),
                ProductCode = Text("/card/productCode"),
                IsFederalGovernment = Get<bool>("/card/isFederalGovernment"),
                Last4 = Text("/card/last4"),
            },
            Merchant = new MerchantInfo
            {
                Mcc = Text("/merchant/mcc"),
                Name = Text("/merchant/name"),
                Country = Text("/merchant/country"),
                TaxId = Text("/merchant/taxId"),
            },
            Customer = new CustomerInfo { Reference = Text("/customer/reference") },
            Order = new OrderInfo
            {
                PurchaseOrderNumber = Text("/order/purchaseOrderNumber"),
                InvoiceNumber = Text("/order/invoiceNumber"),
                OrderDate = Get<DateOnly>("/order/orderDate"),
            },
            Tax = new TaxInfo
            {
                Amount = Get<decimal>("/tax/amount"),
                Indicator = Get<TaxIndicator>("/tax/indicator"),
                Rate = Get<decimal>("/tax/rate"),
                ExemptionId = Text("/tax/exemptionId"),
            },
            Amounts = new SummaryAmounts
            {
                Discount = Get<decimal>("/amounts/discount"),
                Freight = Get<decimal>("/amounts/freight"),
                Duty = Get<decimal>("/amounts/duty"),
                ShippingTax = Get<decimal>("/amounts/shippingTax"),
                Handling = Get<decimal>("/amounts/handling"),
            },
            Shipping = new ShippingInfo
            {
                FromPostalCode = Text("/shipping/fromPostalCode"),
                ToPostalCode = Text("/shipping/toPostalCode"),
                ToCountry = Text("/shipping/toCountry"),
            },
            Billing = new BillingInfo { PostalCode = Text("/billing/postalCode") },
            Amex = new AmexInfo
            {
                RequestorName = Text("/amex/requestorName"),
                AdviceText = _header.TryGetValue("/amex/adviceText", out var advice) ? (List<string>)advice : null,
            },
            LineItems = lines,
        };
    }

    private PathMap BuildPaths()
    {
        var builder = new PathMapBuilder();
        foreach (var field in CanonicalFields.All)
        {
            if (!field.IsLineField)
            {
                builder.Field(field.Path, _headerSources.GetValueOrDefault(field.Path));
                continue;
            }

            var entry = _map.Lines.FirstOrDefault(f => f.Target == field.Name);
            var column = entry is null ? null : Column(entry.Source);
            builder.LineField(field.Name, index => index < _rows.Count ? Cell(_rows[index], column) : null);
        }

        var first = _rows[0].Line;
        var last = _rows[^1].Line;
        var collection = first == last ? Messages.Format(Resources.Strings.CsvRowLabel, first) : Messages.Format(Resources.Strings.CsvRowsLabel, first, last);
        builder.LineItems(
            new SourceField(collection, null, new SourceLocation(first, 1)),
            index => index < _rows.Count ? new SourceField(Messages.Format(Resources.Strings.CsvRowLabel, _rows[index].Line), null, new SourceLocation(_rows[index].Line, 1)) : null);
        var advice = _headerSources.GetValueOrDefault("/amex/adviceText");
        builder.AdviceText(advice, _ => advice);
        return builder.Build();
    }

    private List<CardNumberMatch> Matches(IReadOnlyDictionary<(CsvRow Row, int Column), CardBrand> masked)
    {
        var matches = new List<CardNumberMatch>();
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            for (var column = 0; column < row.Cells.Length; column++)
            {
                if (!masked.TryGetValue((row, column), out var brand))
                {
                    continue;
                }

                var canonical = CanonicalFor(column, i);
                var isCode = canonical is not null && CanonicalFields.FindByPointer(canonical) is { IsCode: true };
                matches.Add(new CardNumberMatch(Label(_table, row, column), canonical, row.Cells[column], brand, isCode, Position(row, column)));
            }
        }

        return matches;
    }

    // The canonical field a column feeds, transaction fields first; the scrub needs it to tell code fields from text.
    private string? CanonicalFor(int column, int lineIndex)
    {
        var name = _table.Header[column];
        if (_map.Transaction.FirstOrDefault(f => string.Equals(f.Source.Column, name, StringComparison.Ordinal)) is { } header)
        {
            return header.Target;
        }

        return _map.Lines.FirstOrDefault(f => string.Equals(f.Source.Column, name, StringComparison.Ordinal)) is { } line
            ? CanonicalAssembler.LinePointer(lineIndex, line.Target)
            : null;
    }

    private void Convert(CanonicalField field, string text, Currency? minorUnits, string label, string pointer, Action<object> set)
    {
        if (CsvValues.TryConvert(field, text, minorUnits, out var value, out var expectation))
        {
            set(value);
        }
        else
        {
            _log.AddInvalid(label, pointer, Messages.Format(Resources.Strings.InvalidValue, label, expectation));
        }
    }

    private void Defaulted(string label, CsvFieldMap entry)
    {
        if (_defaulted.Add(entry.MapPointer))
        {
            _log.Add(AdapterNoteKind.Defaulted, label, null, Messages.Format(Resources.Strings.CsvDefaultUsed, label, entry.MapPointer));
        }
    }

    private (string? Text, bool Defaulted) Resolve(CsvSource source, CsvRow row)
    {
        if (source.Column is null)
        {
            return (source.Constant, false);
        }

        var index = _columns[source.Column];
        var cell = index < row.Cells.Length ? row.Cells[index] : string.Empty;
        if (string.IsNullOrWhiteSpace(cell))
        {
            return source.Default is null ? (null, false) : (source.Default, true);
        }

        if (source.Values is { } values && (values.TryGetValue(cell, out var mapped) || values.TryGetValue(cell.Trim(), out mapped)))
        {
            return (mapped, false);
        }

        return (cell, false);
    }

    private int? Column(CsvSource source) => source.Column is { } name ? _columns[name] : null;

    private SourceField? Cell(CsvRow row, int? column) =>
        column is { } index ? new SourceField(Label(_table, row, index), null, Position(row, index)) : null;

    private static SourceLocation? Position(CsvRow row, int column) =>
        column < row.Positions.Length ? row.Positions[column] : null;
}
