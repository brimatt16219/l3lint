using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters;

/// <summary>
/// Reads an Authorize.net <c>createTransactionRequest</c> body, wrapped (with <c>merchantAuthentication</c>) or as
/// a bare <c>transactionRequest</c> object, with Level 2 and Level 3 data (FR-004).
/// </summary>
/// <remarks>
/// <para>
/// Amounts, quantities and flags may be JSON numbers and booleans or quoted strings, as in the published example;
/// both are read, and fixes are written back as strings. The currency is <c>currencyCode</c>, else
/// <see cref="AdapterOptions.DefaultCurrency"/> with a Defaulted note. <c>lineItems.lineItem</c> may be one
/// object or an array. The line description is <c>name</c>, else <c>description</c>; the product code is
/// <c>productCode</c>, else <c>productSKU</c>; both fallbacks add an Ambiguous note.
/// <c>order.purchaseOrderDateUTC</c> is not mapped: the spec field mapping lists the order date as not supported.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class AuthorizeNetAdapter : IPayloadAdapter
{
    private const string Gateway = "Authorize.net";
    private const string Wrapped = "/createTransactionRequest/transactionRequest";
    private const string Bare = "/transactionRequest";

    private static readonly RouteSet WrappedArray = Build(Wrapped, array: true);
    private static readonly RouteSet WrappedObject = Build(Wrapped, array: false);
    private static readonly RouteSet BareArray = Build(Bare, array: true);
    private static readonly RouteSet BareObject = Build(Bare, array: false);

    private static readonly PointerTable Pointers = new(
        WrappedArray,
        WrappedObject,
        BareArray,
        BareObject,
        Alternates(Wrapped, array: true),
        Alternates(Wrapped, array: false),
        Alternates(Bare, array: true),
        Alternates(Bare, array: false));

    /// <inheritdoc/>
    public string Name => "authorizenet";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>Looks for the root <c>createTransactionRequest</c>, or a root <c>transactionRequest</c> object.</remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root))
        {
            return 0.0;
        }

        var envelope = Probe.Child(root, "createTransactionRequest");
        if (envelope.ValueKind == JsonValueKind.Object)
        {
            var score = 0.9;
            if (Probe.Has(envelope, "transactionRequest", JsonValueKind.Object))
            {
                score += 0.05;
            }

            if (Probe.Has(envelope, "merchantAuthentication", JsonValueKind.Object))
            {
                score += 0.05;
            }

            return Math.Min(score, 1.0);
        }

        var request = Probe.Child(root, "transactionRequest");
        if (request.ValueKind != JsonValueKind.Object)
        {
            return 0.0;
        }

        var bare = 0.85;
        if (Probe.Has(request, "transactionType") || Probe.Has(request, "amount") || Probe.Has(request, "lineItems"))
        {
            bare += 0.05;
        }

        if (Probe.Has(root, "merchantAuthentication", JsonValueKind.Object))
        {
            bare += 0.05;
        }

        return bare;
    }

    /// <inheritdoc/>
    /// <exception cref="InputException">The document is not JSON.</exception>
    public IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options)
    {
        var root = JsonAdapterSupport.RequireJson(document, Name);
        ArgumentNullException.ThrowIfNull(options);

        var log = new MappingLog();
        var reader = new PayloadReader(root, log);
        var prefix = Probe.Has(root, "createTransactionRequest", JsonValueKind.Object) ? Wrapped : Bare;
        var lineItem = reader.Element(prefix + "/lineItems/lineItem");
        var single = lineItem is { ValueKind: JsonValueKind.Object };
        var routes = (prefix, single) switch
        {
            (Wrapped, true) => WrappedObject,
            (Wrapped, false) => WrappedArray,
            (_, true) => BareObject,
            _ => BareArray,
        };
        var arrayRoutes = prefix == Wrapped ? WrappedArray : BareArray;

        var currency = reader.ReadText(routes.Header("/currency"), 0, "/currency");
        currency = JsonAdapterSupport.DefaultCurrency(currency, options, log, Gateway, prefix + "/currencyCode");

        // The published JSON example sends one line as an object (research item 10); several lines are an array.
        var lines = single ? new LinePlan(1, static i => i) : LinePlan.FromArray(reader, routes);

        // One object covers line 0 only; any later index points into the array form.
        RouteSet LineRoutes(int index) => single && index > 0 ? arrayRoutes : routes;
        string? Item(int index) => LineRoutes(index).Item?.Format(index);

        var transaction = CanonicalAssembler.Assemble(reader, routes, lines, currency, LineRoutes, Item);
        var overrides = new Dictionary<(int Index, string Field), Route>();
        transaction = Fallbacks(transaction, reader, routes, Alternates(prefix, !single), overrides);
        JsonAdapterSupport.CheckTaxFlag(transaction, log, prefix + "/taxExempt", Gateway);

        var paths = PathPlan.Build(routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries), LineRoutes, overrides);
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);

    private static RouteSet Build(string prefix, bool array)
    {
        var collection = prefix + "/lineItems/lineItem";
        var line = array ? collection + "/#/" : collection + "/";
        return new RouteSet(
            array ? collection : null,
            new SourceTemplate(array ? collection + "/#" : collection),
            [
                Route.Header("/amount", prefix + "/amount", Shape.DecimalText),
                Route.Header("/currency", prefix + "/currencyCode", Shape.Currency),
                Route.Header("/tax/amount", prefix + "/tax/amount", Shape.DecimalText),
                Route.Header("/tax/indicator", prefix + "/taxExempt", Shape.TaxExemptText),
                Route.Header("/customer/reference", prefix + "/poNumber", Shape.Text),
                Route.Header("/order/invoiceNumber", prefix + "/order/invoiceNumber", Shape.Text),
                Route.Header("/amounts/freight", prefix + "/shipping/amount", Shape.DecimalText),
                Route.Header("/amounts/duty", prefix + "/duty/amount", Shape.DecimalText),
                Route.Header("/amounts/discount", prefix + "/discount/amount", Shape.DecimalText),
                Route.Header("/shipping/fromPostalCode", prefix + "/shipFrom/zip", Shape.Text),
                Route.Header("/shipping/toPostalCode", prefix + "/shipTo/zip", Shape.Text),
                Route.Header("/shipping/toCountry", prefix + "/shipTo/country", Shape.Country),
                Route.Header("/billing/postalCode", prefix + "/billTo/zip", Shape.Text),
            ],
            [
                Route.Line("description", line + "name", Shape.Text),
                Route.Line("productCode", line + "productCode", Shape.Text),
                Route.Line("commodityCode", line + "commodityCode", Shape.Text),
                Route.Line("quantity", line + "quantity", Shape.DecimalText),
                Route.Line("unitOfMeasure", line + "unitOfMeasure", Shape.Text),
                Route.Line("unitCost", line + "unitPrice", Shape.DecimalText),
                Route.Line("discountAmount", line + "discountAmount", Shape.DecimalText),
                Route.Line("taxAmount", line + "taxAmount", Shape.DecimalText),
                Route.Line("total", line + "totalAmount", Shape.DecimalText),
                Route.Line("taxable", line + "taxable", Shape.FlagText),
                Route.Line("amountIncludesTax", line + "taxIncludedInTotal", Shape.FlagText),
            ]);
    }

    // Second-choice sources: the item description when the name is missing, the SKU when the product code is missing.
    private static RouteSet Alternates(string prefix, bool array)
    {
        var line = prefix + "/lineItems/lineItem" + (array ? "/#/" : "/");
        return new RouteSet(
            null,
            null,
            [],
            [
                Route.Line("description", line + "description", Shape.Text),
                Route.Line("productCode", line + "productSKU", Shape.Text),
            ]);
    }

    private static Transaction Fallbacks(Transaction transaction, PayloadReader reader, RouteSet primary, RouteSet alternates, Dictionary<(int Index, string Field), Route> overrides)
    {
        var lines = transaction.LineItems.ToList();
        var changed = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var description = Fallback(reader, primary, alternates, overrides, (i, "description"), line.Description, Resources.Strings.NoteItemDescriptionUsed);
            var productCode = Fallback(reader, primary, alternates, overrides, (i, "productCode"), line.ProductCode, Resources.Strings.NoteSkuUsed);
            if (!ReferenceEquals(description, line.Description) || !ReferenceEquals(productCode, line.ProductCode))
            {
                lines[i] = line with { Description = description, ProductCode = productCode };
                changed = true;
            }
        }

        return changed ? transaction with { LineItems = lines } : transaction;
    }

    private static string? Fallback(PayloadReader reader, RouteSet primary, RouteSet alternates, Dictionary<(int Index, string Field), Route> overrides, (int Index, string Field) at, string? current, string note)
    {
        var (index, field) = at;
        var canonical = CanonicalAssembler.LinePointer(index, field);
        var route = alternates.Line(field)!;
        var source = route.Source.Format(index);
        if (current is not null || reader.Log.IsInvalid(canonical) || reader.Element(source) is null)
        {
            return current;
        }

        overrides[at] = route;
        var value = reader.ReadText(route, index, canonical);
        if (value is not null)
        {
            reader.Log.Add(AdapterNoteKind.Ambiguous, source, canonical, Messages.Format(note, primary.Line(field)!.Source.Format(index), source));
        }

        return value;
    }
}
