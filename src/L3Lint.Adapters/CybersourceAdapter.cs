using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters;

/// <summary>
/// Reads a Cybersource REST payments request (<c>/pts/v2/payments</c> shape) with Level 2 and Level 3 data
/// (FR-004).
/// </summary>
/// <remarks>
/// <para>
/// Amounts are decimal strings in major units. <c>orderInformation.invoiceDetails.taxable</c> maps to the tax
/// indicator (false means exempt). When <c>amountDetails.taxAmount</c> is absent, the header tax is the sum of the
/// line tax amounts, because most processors take tax per line. The line product code is
/// <c>productCode</c>; when it is absent, <c>productSku</c> (or <c>productSKU</c>) is used with an Ambiguous note,
/// because which one reaches the network is processor specific. <c>invoiceDetails.purchaseOrderDate</c> is not
/// mapped: the spec field mapping lists the order date as not supported.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class CybersourceAdapter : IPayloadAdapter
{
    private const string Gateway = "Cybersource";
    private const string Order = "/orderInformation";
    private const string Amounts = Order + "/amountDetails";
    private const string Lines = Order + "/lineItems";

    private static readonly RouteSet Routes = new(
        Lines,
        new SourceTemplate(Lines + "/#"),
        [
            Route.Header("/amount", Amounts + "/totalAmount", Shape.DecimalText),
            Route.Header("/currency", Amounts + "/currency", Shape.Currency),
            Route.Header("/tax/amount", Amounts + "/taxAmount", Shape.DecimalText),
            Route.Header("/tax/indicator", Order + "/invoiceDetails/taxable", Shape.Taxable),
            Route.Header("/customer/reference", Order + "/invoiceDetails/purchaseOrderNumber", Shape.Text),
            Route.Header("/order/invoiceNumber", "/clientReferenceInformation/code", Shape.Text),
            Route.Header("/amounts/freight", Amounts + "/freightAmount", Shape.DecimalText),
            Route.Header("/amounts/duty", Amounts + "/dutyAmount", Shape.DecimalText),
            Route.Header("/amounts/discount", Amounts + "/discountAmount", Shape.DecimalText),
            Route.Header("/shipping/fromPostalCode", Order + "/shippingDetails/shipFromPostalCode", Shape.Text),
            Route.Header("/shipping/toPostalCode", Order + "/shipTo/postalCode", Shape.Text),
            Route.Header("/shipping/toCountry", Order + "/shipTo/country", Shape.Country),
            Route.Header("/billing/postalCode", Order + "/billTo/postalCode", Shape.Text),
        ],
        [
            Route.Line("description", Lines + "/#/productName", Shape.Text),
            Route.Line("productCode", Lines + "/#/productCode", Shape.Text),
            Route.Line("commodityCode", Lines + "/#/commodityCode", Shape.Text),
            Route.Line("quantity", Lines + "/#/quantity", Shape.DecimalText),
            Route.Line("unitOfMeasure", Lines + "/#/unitOfMeasure", Shape.Text),
            Route.Line("unitCost", Lines + "/#/unitPrice", Shape.DecimalText),
            Route.Line("discountAmount", Lines + "/#/discountAmount", Shape.DecimalText),
            Route.Line("taxAmount", Lines + "/#/taxAmount", Shape.DecimalText),
            Route.Line("total", Lines + "/#/totalAmount", Shape.DecimalText),
            Route.Line("amountIncludesTax", Lines + "/#/amountIncludesTax", Shape.YesNoFlag),
        ]);

    // The SDK and examples write productSku; the field reference writes productSKU (research item 4).
    private static readonly Route[] SkuRoutes =
    [
        Route.Line("productCode", Lines + "/#/productSku", Shape.Text),
        Route.Line("productCode", Lines + "/#/productSKU", Shape.Text),
    ];

    private static readonly PointerTable Pointers = new(
        Routes,
        new RouteSet(null, null, [], [SkuRoutes[0]]),
        new RouteSet(null, null, [], [SkuRoutes[1]]));

    /// <inheritdoc/>
    public string Name => "cybersource";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>Needs <c>orderInformation</c>; <c>clientReferenceInformation</c>, <c>processingInformation</c> or <c>paymentInformation</c> raise the score.</remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root) || !Probe.Has(root, "orderInformation", JsonValueKind.Object))
        {
            return 0.0;
        }

        var score = 0.85;
        if (Probe.Has(root, "clientReferenceInformation", JsonValueKind.Object)
            || Probe.Has(root, "processingInformation", JsonValueKind.Object)
            || Probe.Has(root, "paymentInformation", JsonValueKind.Object))
        {
            score += 0.1;
        }

        var order = Probe.Child(root, "orderInformation");
        if (Probe.Has(order, "amountDetails", JsonValueKind.Object) || Probe.Has(order, "lineItems", JsonValueKind.Array))
        {
            score += 0.05;
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
        var currency = reader.ReadText(Routes.Header("/currency"), 0, "/currency");
        var lines = LinePlan.FromArray(reader, Routes);

        var transaction = CanonicalAssembler.Assemble(reader, Routes, lines, currency);
        var overrides = new Dictionary<(int Index, string Field), Route>();
        transaction = ProductCodes(transaction, reader, overrides);
        transaction = JsonAdapterSupport.TaxFromLines(transaction, log, Amounts + "/taxAmount");
        JsonAdapterSupport.CheckTaxFlag(transaction, log, Order + "/invoiceDetails/taxable", Gateway);

        var paths = PathPlan.Build(Routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries), lineOverrides: overrides);
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);

    private static Transaction ProductCodes(Transaction transaction, PayloadReader reader, Dictionary<(int Index, string Field), Route> overrides)
    {
        var lines = transaction.LineItems.ToList();
        var changed = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var codePointer = CanonicalAssembler.LinePointer(i, "productCode");
            if (reader.Log.IsInvalid(codePointer))
            {
                continue;
            }

            foreach (var route in SkuRoutes)
            {
                if (reader.Element(route.Source.Format(i)) is not { } element)
                {
                    continue;
                }

                var codeSource = Routes.Line("productCode")!.Source.Format(i);
                if (lines[i].ProductCode is { } code)
                {
                    if (element.ValueKind == JsonValueKind.String && !string.Equals(element.GetString(), code, StringComparison.Ordinal))
                    {
                        reader.Log.Add(AdapterNoteKind.Ambiguous, codeSource, codePointer, Messages.Format(Resources.Strings.NoteCodeOverSku, codeSource, route.Source.Format(i)));
                    }

                    break;
                }

                var value = reader.ReadText(route, i, codePointer);
                overrides[(i, "productCode")] = route;
                if (value is not null)
                {
                    reader.Log.Add(AdapterNoteKind.Ambiguous, route.Source.Format(i), codePointer, Messages.Format(Resources.Strings.NoteSkuUsed, codeSource, route.Source.Format(i)));
                    lines[i] = lines[i] with { ProductCode = value };
                    changed = true;
                }

                break;
            }
        }

        return changed ? transaction with { LineItems = lines } : transaction;
    }
}
