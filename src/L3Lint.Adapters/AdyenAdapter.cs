using System.Globalization;
using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters;

/// <summary>
/// Reads an Adyen Checkout <c>/payments</c> (or capture) request with Level 2 and Level 3 data (FR-003).
/// </summary>
/// <remarks>
/// <para>
/// Two request forms are accepted: the structured <c>enhancedSchemeData.levelTwoThree</c> object with JSON
/// integers and an <c>itemDetailLines</c> array, and the legacy flat <c>additionalData</c> keys
/// <c>enhancedSchemeData.*</c> with string values and lines numbered from 1 in the key
/// (<c>enhancedSchemeData.itemDetailLine1.unitPrice</c>). When both are present the structured object is used
/// and an Ambiguous note says so. Amounts are minor units, converted with the currency exponent (FR-008).
/// Adyen has no tax indicator and no header discount: a tax amount above 0 marks the sale taxable.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class AdyenAdapter : IPayloadAdapter
{
    private const string Gateway = "Adyen";
    private const string Structured = "/enhancedSchemeData/levelTwoThree";
    private const string StructuredLines = Structured + "/itemDetailLines";
    private const string LegacyPrefix = "enhancedSchemeData.";
    private const string LegacyLinePrefix = "enhancedSchemeData.itemDetailLine";
    private const string Legacy = "/additionalData/" + LegacyPrefix;
    private const string LegacyLine = "/additionalData/" + LegacyLinePrefix + "#.";

    private static readonly Route[] Common =
    [
        Route.Header("/amount", "/amount/value", Shape.MinorUnits),
        Route.Header("/currency", "/amount/currency", Shape.Currency),
        Route.Header("/order/invoiceNumber", "/reference", Shape.Text),
    ];

    private static readonly RouteSet StructuredRoutes = new(
        StructuredLines,
        new SourceTemplate(StructuredLines + "/#"),
        [
            .. Common,
            Route.Header("/customer/reference", Structured + "/customerReferenceNumber", Shape.Text),
            Route.Header("/tax/amount", Structured + "/totalTaxAmount", Shape.MinorUnits),
            Route.Header("/amounts/freight", Structured + "/freightAmount", Shape.MinorUnits),
            Route.Header("/amounts/duty", Structured + "/dutyAmount", Shape.MinorUnits),
            Route.Header("/shipping/fromPostalCode", Structured + "/shipFromPostalCode", Shape.Text),
            Route.Header("/shipping/toPostalCode", Structured + "/destination/postalCode", Shape.Text),
            Route.Header("/shipping/toCountry", Structured + "/destination/countryCode", Shape.Country),
            Route.Header("/order/orderDate", Structured + "/orderDate", Shape.Date),
        ],
        [
            Route.Line("description", StructuredLines + "/#/description", Shape.Text),
            Route.Line("productCode", StructuredLines + "/#/productCode", Shape.Text),
            Route.Line("commodityCode", StructuredLines + "/#/commodityCode", Shape.Text),
            Route.Line("quantity", StructuredLines + "/#/quantity", Shape.WholeNumber),
            Route.Line("unitOfMeasure", StructuredLines + "/#/unitOfMeasure", Shape.Text),
            Route.Line("unitCost", StructuredLines + "/#/unitPrice", Shape.MinorUnits),
            Route.Line("discountAmount", StructuredLines + "/#/discountAmount", Shape.MinorUnits),
            Route.Line("total", StructuredLines + "/#/totalAmount", Shape.MinorUnits),
        ]);

    // Legacy line keys number lines from 1 (the only literal example uses itemDetailLine1; AD4).
    private static readonly RouteSet LegacyRoutes = new(
        null,
        null,
        [
            .. Common,
            Route.Header("/customer/reference", Legacy + "customerReference", Shape.Text),
            Route.Header("/tax/amount", Legacy + "totalTaxAmount", Shape.MinorUnitsText),
            Route.Header("/amounts/freight", Legacy + "freightAmount", Shape.MinorUnitsText),
            Route.Header("/amounts/duty", Legacy + "dutyAmount", Shape.MinorUnitsText),
            Route.Header("/shipping/fromPostalCode", Legacy + "shipFromPostalCode", Shape.Text),
            Route.Header("/shipping/toPostalCode", Legacy + "destinationPostalCode", Shape.Text),
            Route.Header("/shipping/toCountry", Legacy + "destinationCountryCode", Shape.Country),
            Route.Header("/order/orderDate", Legacy + "orderDate", Shape.DateDayMonthYear),
        ],
        [
            Route.Line("description", LegacyLine + "description", Shape.Text, keyBase: 1),
            Route.Line("productCode", LegacyLine + "productCode", Shape.Text, keyBase: 1),
            Route.Line("commodityCode", LegacyLine + "commodityCode", Shape.Text, keyBase: 1),
            Route.Line("quantity", LegacyLine + "quantity", Shape.WholeNumberText, keyBase: 1),
            Route.Line("unitOfMeasure", LegacyLine + "unitOfMeasure", Shape.Text, keyBase: 1),
            Route.Line("unitCost", LegacyLine + "unitPrice", Shape.MinorUnitsText, keyBase: 1),
            Route.Line("discountAmount", LegacyLine + "discountAmount", Shape.MinorUnitsText, keyBase: 1),
            Route.Line("total", LegacyLine + "totalAmount", Shape.MinorUnitsText, keyBase: 1),
        ]);

    private static readonly PointerTable Pointers = new(StructuredRoutes, LegacyRoutes);

    /// <inheritdoc/>
    public string Name => "adyen";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>Needs a top-level <c>merchantAccount</c>; Level 2 and 3 data in either form raises the score.</remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root) || !Probe.Has(root, "merchantAccount", JsonValueKind.String))
        {
            return 0.0;
        }

        var score = 0.8;
        if (Probe.Has(Probe.Child(root, "amount"), "value"))
        {
            score += 0.1;
        }

        if (Probe.Has(Probe.Child(root, "enhancedSchemeData"), "levelTwoThree", JsonValueKind.Object) || HasLegacyKeys(root))
        {
            score += 0.1;
        }

        return Math.Min(score, 1.0);
    }

    /// <inheritdoc/>
    /// <exception cref="InputException">The document is not JSON.</exception>
    public IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options)
    {
        var root = JsonAdapterSupport.RequireJson(document, Name);
        ArgumentNullException.ThrowIfNull(options);

        var log = new MappingLog();
        var reader = new PayloadReader(root, log);
        var structured = reader.Element(Structured) is not null;
        var legacy = HasLegacyKeys(root);
        if (structured && legacy)
        {
            log.Add(AdapterNoteKind.Ambiguous, "/additionalData", null, Resources.Strings.NoteAdyenBothForms);
        }

        var routes = legacy && !structured ? LegacyRoutes : StructuredRoutes;
        var currency = reader.ReadText(routes.Header("/currency"), 0, "/currency");
        JsonAdapterSupport.UseCurrencyForMinorUnits(reader, currency, "/amount/currency");
        var lines = routes == LegacyRoutes ? LegacyLines(root, log) : LinePlan.FromArray(reader, routes);

        var transaction = CanonicalAssembler.Assemble(reader, routes, lines, currency);
        transaction = JsonAdapterSupport.TaxIndicatorFromAmount(transaction, log, Gateway);
        if (transaction.Amounts.Duty is > 0m)
        {
            log.Add(AdapterNoteKind.Ambiguous, routes.Header("/amounts/duty")!.Source.Text, "/amounts/duty", Messages.Format(Resources.Strings.NoteDutyOutsideAmount, Gateway));
        }

        var paths = PathPlan.Build(routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries));
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);

    private static bool HasLegacyKeys(JsonElement root)
    {
        var data = Probe.Child(root, "additionalData");
        if (data.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in data.EnumerateObject())
        {
            if (property.Name.StartsWith(LegacyPrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // Lines are the distinct itemDetailLine numbers in ascending order; a gap is noted, not filled with empty lines.
    private static LinePlan LegacyLines(JsonElement root, MappingLog log)
    {
        var numbers = new SortedSet<int>();
        foreach (var property in Probe.Child(root, "additionalData").EnumerateObject())
        {
            var name = property.Name;
            if (!name.StartsWith(LegacyLinePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var dot = name.IndexOf('.', LegacyLinePrefix.Length);
            if (dot > LegacyLinePrefix.Length
                && int.TryParse(name.AsSpan(LegacyLinePrefix.Length, dot - LegacyLinePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                numbers.Add(number);
            }
        }

        if (numbers.Count == 0)
        {
            return new LinePlan(0, static i => i + 1);
        }

        var keys = numbers.ToArray();
        if (keys[0] != 1 || keys[^1] != keys.Length)
        {
            log.Add(AdapterNoteKind.Ambiguous, "/additionalData", "/lineItems", Resources.Strings.NoteAdyenLineGap);
        }

        return new LinePlan(keys.Length, i => i < keys.Length ? keys[i] : keys[^1] + (i - keys.Length + 1));
    }
}
