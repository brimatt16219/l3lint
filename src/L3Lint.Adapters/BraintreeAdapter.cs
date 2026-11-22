using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters;

/// <summary>
/// Reads a Braintree transaction sale request in the server SDK's camelCase shape with Level 2 and Level 3
/// fields (FR-003).
/// </summary>
/// <remarks>
/// <para>
/// Amounts and quantities are decimal strings in major units. Braintree has no currency field (the currency comes
/// from the merchant account), so <see cref="AdapterOptions.DefaultCurrency"/> is used with a Defaulted note, or
/// the currency stays absent. <c>taxExempt</c> maps to the tax indicator: true is exempt, false is taxable.
/// <c>lineItems[].kind</c> maps to the debit or credit indicator. An alpha-3 ship-to country
/// (<c>shipping.countryCodeAlpha3</c>, used in the published examples) is converted to alpha-2. The GraphQL API
/// nests the same data differently and is not read.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class BraintreeAdapter : IPayloadAdapter
{
    private const string Gateway = "Braintree";
    private const string Lines = "/lineItems";

    private static readonly RouteSet Alpha2Routes = Build("/shipping/countryCodeAlpha2");
    private static readonly RouteSet Alpha3Routes = Build("/shipping/countryCodeAlpha3");
    private static readonly PointerTable Pointers = new(Alpha2Routes, Alpha3Routes);

    // Keys that only other gateways use; any of them rules Braintree out.
    private static readonly string[] OtherGatewayKeys =
        ["merchantAccount", "amount_details", "payment_details", "processing", "orderInformation", "transactionRequest", "createTransactionRequest", "enhancedSchemeData"];

    private static readonly string[] BraintreeKeys =
        ["taxAmount", "taxExempt", "purchaseOrderNumber", "shipsFromPostalCode", "shippingAmount", "shippingTaxAmount", "paymentMethodNonce", "paymentMethodToken", "merchantAccountId"];

    /// <inheritdoc/>
    public string Name => "braintree";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>
    /// Looks for line items carrying <c>unitAmount</c> or <c>kind</c>, or Braintree's Level 2 keys, with no key that
    /// belongs to another gateway (Adyen also has a <c>lineItems</c> array, but always with <c>merchantAccount</c>).
    /// </remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root) || OtherGatewayKeys.Any(k => Probe.Has(root, k)))
        {
            return 0.0;
        }

        var keys = BraintreeKeys.Count(k => Probe.Has(root, k));
        double score;
        if (Probe.FirstItemHasAny(root, "lineItems", "unitAmount", "kind"))
        {
            score = 0.9;
        }
        else if (keys >= 2)
        {
            score = 0.8;
        }
        else
        {
            return 0.0;
        }

        if (Probe.Has(root, "amount", JsonValueKind.String))
        {
            score += 0.05;
        }

        if (keys >= 2)
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
        var routes = reader.Element("/shipping/countryCodeAlpha2") is null && reader.Element("/shipping/countryCodeAlpha3") is not null
            ? Alpha3Routes
            : Alpha2Routes;
        var currency = JsonAdapterSupport.DefaultCurrency(null, options, log, Gateway, null);
        var lines = LinePlan.FromArray(reader, routes);

        var transaction = CanonicalAssembler.Assemble(reader, routes, lines, currency);
        JsonAdapterSupport.CheckTaxFlag(transaction, log, "/taxExempt", Gateway);

        var paths = PathPlan.Build(routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries));
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);

    private static RouteSet Build(string country) => new(
        Lines,
        new SourceTemplate(Lines + "/#"),
        [
            Route.Header("/amount", "/amount", Shape.DecimalText),
            Route.Header("/tax/amount", "/taxAmount", Shape.DecimalText),
            Route.Header("/tax/indicator", "/taxExempt", Shape.TaxExempt),
            Route.Header("/customer/reference", "/purchaseOrderNumber", Shape.Text),
            Route.Header("/order/invoiceNumber", "/orderId", Shape.Text),
            Route.Header("/amounts/freight", "/shippingAmount", Shape.DecimalText),
            Route.Header("/amounts/discount", "/discountAmount", Shape.DecimalText),
            Route.Header("/amounts/shippingTax", "/shippingTaxAmount", Shape.DecimalText),
            Route.Header("/shipping/fromPostalCode", "/shipsFromPostalCode", Shape.Text),
            Route.Header("/shipping/toPostalCode", "/shipping/postalCode", Shape.Text),
            Route.Header("/shipping/toCountry", country, Shape.Country),
        ],
        [
            Route.Line("description", Lines + "/#/name", Shape.Text),
            Route.Line("productCode", Lines + "/#/productCode", Shape.Text),
            Route.Line("commodityCode", Lines + "/#/commodityCode", Shape.Text),
            Route.Line("quantity", Lines + "/#/quantity", Shape.DecimalText),
            Route.Line("unitOfMeasure", Lines + "/#/unitOfMeasure", Shape.Text),
            Route.Line("unitCost", Lines + "/#/unitAmount", Shape.DecimalText),
            Route.Line("discountAmount", Lines + "/#/discountAmount", Shape.DecimalText),
            Route.Line("taxAmount", Lines + "/#/taxAmount", Shape.DecimalText),
            Route.Line("total", Lines + "/#/totalAmount", Shape.DecimalText),
            Route.Line("debitCreditIndicator", Lines + "/#/kind", Shape.DebitCredit),
        ]);
}
