using System.Text.Json;
using L3Lint.Adapters.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters;

/// <summary>
/// Reads the JSON form of a Stripe PaymentIntent create request with Level 2 and Level 3 data in
/// <c>amount_details</c> and <c>payment_details</c> (FR-003).
/// </summary>
/// <remarks>
/// <para>
/// Stripe sends every amount as an integer in minor units, so values are converted with the currency exponent
/// from the embedded ISO 4217 table, and the lowercase currency becomes uppercase (FR-008). Stripe has no line
/// total: it is computed as round(quantity x unit cost) minus the line discount and listed in
/// <see cref="AdapterResult.DerivedFields"/>. When only lines carry tax, the header tax is their sum. Stripe has no
/// tax indicator, so a tax amount above 0 marks the sale taxable; zero tax leaves it absent.
/// </para>
/// <para>Stateless and thread-safe.</para>
/// </remarks>
public sealed class StripeAdapter : IPayloadAdapter
{
    private const string Gateway = "Stripe";
    private const string Lines = "/amount_details/line_items";
    private const string HeaderTax = "/amount_details/tax/total_tax_amount";

    private static readonly RouteSet Routes = new(
        Lines,
        new SourceTemplate(Lines + "/#"),
        [
            Route.Header("/amount", "/amount", Shape.MinorUnits),
            Route.Header("/currency", "/currency", Shape.LowercaseCurrency),
            Route.Header("/tax/amount", HeaderTax, Shape.MinorUnits),
            Route.Header("/customer/reference", "/payment_details/customer_reference", Shape.Text),
            Route.Header("/order/invoiceNumber", "/payment_details/order_reference", Shape.Text),
            Route.Header("/amounts/freight", "/amount_details/shipping/amount", Shape.MinorUnits),
            Route.Header("/amounts/discount", "/amount_details/discount_amount", Shape.MinorUnits),
            Route.Header("/shipping/fromPostalCode", "/amount_details/shipping/from_postal_code", Shape.Text),
            Route.Header("/shipping/toPostalCode", "/amount_details/shipping/to_postal_code", Shape.Text),
        ],
        [
            Route.Line("description", Lines + "/#/product_name", Shape.Text),
            Route.Line("productCode", Lines + "/#/product_code", Shape.Text),
            Route.Line("commodityCode", Lines + "/#/payment_method_options/card/commodity_code", Shape.Text),
            Route.Line("quantity", Lines + "/#/quantity", Shape.WholeNumber),
            Route.Line("unitOfMeasure", Lines + "/#/unit_of_measure", Shape.Text),
            Route.Line("unitCost", Lines + "/#/unit_cost", Shape.MinorUnits),
            Route.Line("discountAmount", Lines + "/#/discount_amount", Shape.MinorUnits),
            Route.Line("taxAmount", Lines + "/#/tax/total_tax_amount", Shape.MinorUnits),
        ]);

    private static readonly PointerTable Pointers = new(Routes);

    /// <inheritdoc/>
    public string Name => "stripe";

    /// <inheritdoc/>
    public InputKind Kind => InputKind.Json;

    /// <inheritdoc/>
    /// <remarks>Looks for <c>amount_details</c> or <c>payment_details</c>, an integer amount and a lowercase currency.</remarks>
    public double Detect(RawDocument document)
    {
        if (!Probe.TryRoot(document, out var root))
        {
            return 0.0;
        }

        var amountDetails = Probe.Has(root, "amount_details", JsonValueKind.Object);
        var paymentDetails = Probe.Has(root, "payment_details", JsonValueKind.Object);
        if (!amountDetails && !paymentDetails)
        {
            return 0.0;
        }

        var score = 0.85;
        if (amountDetails && paymentDetails)
        {
            score += 0.05;
        }

        if (Probe.IsInteger(root, "amount"))
        {
            score += 0.05;
        }

        if (Probe.Has(root, "currency", JsonValueKind.String) && root.GetProperty("currency").GetString() is { Length: > 0 } code && code.All(c => !char.IsAsciiLetterUpper(c)))
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
        transaction = DeriveLineTotals(transaction, reader.MinorUnitCurrency, log);
        transaction = JsonAdapterSupport.TaxFromLines(transaction, log, HeaderTax);
        transaction = JsonAdapterSupport.TaxIndicatorFromAmount(transaction, log, Gateway);

        var paths = PathPlan.Build(Routes, lines, new SourceConverters(reader.MinorUnitCurrency, reader.Alpha3Countries));
        if (options.Verbose)
        {
            IgnoredFields.Report(root, Pointers.ToCanonical, log);
        }

        return [JsonAdapterSupport.Result(transaction, paths, log)];
    }

    /// <inheritdoc/>
    public string? ToCanonicalPointer(string sourcePointer) => Pointers.ToCanonical(sourcePointer);

    // Stripe has no line total field (ST2); the total is round(quantity x unit cost) - line discount in major units.
    private static Transaction DeriveLineTotals(Transaction transaction, Currency currency, MappingLog log)
    {
        if (transaction.LineItems.Count == 0)
        {
            return transaction;
        }

        var lines = new List<LineItem>(transaction.LineItems.Count);
        for (var i = 0; i < transaction.LineItems.Count; i++)
        {
            var line = transaction.LineItems[i];
            if (log.IsInvalid(CanonicalAssembler.LinePointer(i, "quantity"))
                || log.IsInvalid(CanonicalAssembler.LinePointer(i, "unitCost"))
                || log.IsInvalid(CanonicalAssembler.LinePointer(i, "discountAmount")))
            {
                // A total computed from a bad input would be a second finding for the same mistake (ADR-0036).
                log.MarkInvalid(CanonicalAssembler.LinePointer(i, "total"));
            }
            else if (line.Quantity is { } quantity && line.UnitCost is { } unitCost)
            {
                var total = Math.Round(quantity * unitCost, currency.Exponent, MidpointRounding.AwayFromZero) - (line.DiscountAmount ?? 0m);
                line = line with { Total = total };
            }

            lines.Add(line);
        }

        log.AddDerived("/lineItems/*/total", null, Resources.Strings.NoteDerivedLineTotals);
        return transaction with { LineItems = lines };
    }
}
