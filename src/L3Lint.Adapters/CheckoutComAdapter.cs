using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;

namespace L3Lint.Adapters;

/// <summary>
/// Reads a Checkout.com payment (or capture) request with Level 2 and Level 3 data in <c>processing</c>,
/// <c>shipping</c> and <c>items</c> (FR-004).
/// </summary>
/// <remarks>
/// <para>
/// Every amount is an integer in minor units, converted with the currency exponent (FR-008). Checkout.com has no
/// order-level tax indicator, so a tax amount above 0 marks the sale taxable; the line flag
/// <c>items[].tax_exempt</c> maps to the opposite of the canonical line <c>taxable</c> flag. Checkout.com leaves
/// duty and shipping tax out of the payment amount; when either is above 0 an Ambiguous note says the total
/// check may report it.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class CheckoutComAdapter : IPayloadAdapter
{
    private const string Gateway = "Checkout.com";
    private const string Lines = "/items";

    private static readonly RouteSet Routes = new(
        Lines,
        new SourceTemplate(Lines + "/#"),
        [
            Route.Header("/amount", "/amount", Shape.MinorUnits),
            Route.Header("/currency", "/currency", Shape.Currency),
            Route.Header("/tax/amount", "/processing/tax_amount", Shape.MinorUnits),
            Route.Header("/customer/reference", "/processing/order_id", Shape.Text),
            Route.Header("/order/invoiceNumber", "/reference", Shape.Text),
            Route.Header("/amounts/freight", "/processing/shipping_amount", Shape.MinorUnits),
            Route.Header("/amounts/duty", "/processing/duty_amount", Shape.MinorUnits),
            Route.Header("/amounts/discount", "/processing/discount_amount", Shape.MinorUnits),
            Route.Header("/amounts/shippingTax", "/processing/shipping_tax_amount", Shape.MinorUnits),
            Route.Header("/shipping/fromPostalCode", "/shipping/from_address_zip", Shape.Text),
            Route.Header("/shipping/toPostalCode", "/shipping/address/zip", Shape.Text),
            Route.Header("/shipping/toCountry", "/shipping/address/country", Shape.Country),
        ],
        [
            Route.Line("description", Lines + "/#/name", Shape.Text),
            Route.Line("productCode", Lines + "/#/reference", Shape.Text),
            Route.Line("commodityCode", Lines + "/#/commodity_code", Shape.Text),
            Route.Line("quantity", Lines + "/#/quantity", Shape.WholeNumber),
            Route.Line("unitOfMeasure", Lines + "/#/unit_of_measure", Shape.Text),
            Route.Line("unitCost", Lines + "/#/unit_price", Shape.MinorUnits),
            Route.Line("discountAmount", Lines + "/#/discount_amount", Shape.MinorUnits),
            Route.Line("taxAmount", Lines + "/#/tax_amount", Shape.MinorUnits),
            Route.Line("total", Lines + "/#/total_amount", Shape.MinorUnits),
            Route.Line("taxable", Lines + "/#/tax_exempt", Shape.InverseFlag),
        ]);

    private static readonly PointerTable Pointers = new(Routes);

    /// <inheritdoc/>
    public string Name => "checkoutcom";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>Looks for a <c>processing</c> object and an <c>items</c> array with snake_case keys, plus <c>source</c> or <c>processing_channel_id</c>.</remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root))
        {
            return 0.0;
        }

        var processing = Probe.Has(root, "processing", JsonValueKind.Object);
        var items = Probe.FirstItemHasAny(root, "items", "unit_price", "total_amount", "commodity_code", "unit_of_measure", "tax_amount");
        double score;
        if (processing && items)
        {
            score = 0.9;
        }
        else if (processing)
        {
            score = 0.85;
        }
        else if (items)
        {
            score = 0.8;
        }
        else
        {
            return 0.0;
        }

        if (Probe.Has(root, "source", JsonValueKind.Object) || Probe.Has(root, "processing_channel_id", JsonValueKind.String))
        {
            score += 0.05;
        }

        if (Probe.IsInteger(root, "amount"))
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
        JsonAdapterSupport.UseCurrencyForMinorUnits(reader, currency, "/currency");
        var lines = LinePlan.FromArray(reader, Routes);

        var transaction = CanonicalAssembler.Assemble(reader, Routes, lines, currency);
        transaction = JsonAdapterSupport.TaxIndicatorFromAmount(transaction, log, Gateway);
        if (transaction.Amounts.Duty is > 0m)
        {
            log.Add(AdapterNoteKind.Ambiguous, "/processing/duty_amount", "/amounts/duty", Messages.Format(Resources.Strings.NoteDutyOutsideAmount, Gateway));
        }

        if (transaction.Amounts.ShippingTax is > 0m)
        {
            log.Add(AdapterNoteKind.Ambiguous, "/processing/shipping_tax_amount", "/amounts/shippingTax", Messages.Format(Resources.Strings.NoteShippingTaxOutsideAmount, Gateway));
        }

        var paths = PathPlan.Build(Routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries));
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);
}
