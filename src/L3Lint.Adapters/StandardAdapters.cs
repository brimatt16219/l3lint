using L3Lint.Core.Adapters;

namespace L3Lint.Adapters;

/// <summary>
/// The adapters that ship with l3lint, ready to add to a linter with <c>LinterBuilder.AddAdapter</c>.
/// </summary>
/// <example>
/// <code>
/// var builder = new LinterBuilder();
/// foreach (var adapter in StandardAdapters.All)
/// {
///     builder.AddAdapter(adapter);
/// }
/// </code>
/// </example>
public static class StandardAdapters
{
    /// <summary>
    /// Gets the seven standard adapters: Stripe, Adyen, Braintree, Cybersource, Authorize.net, Checkout.com and CSV,
    /// in that order. The instances are stateless and safe to share.
    /// </summary>
    public static IReadOnlyList<IPayloadAdapter> All { get; } =
    [
        new StripeAdapter(),
        new AdyenAdapter(),
        new BraintreeAdapter(),
        new CybersourceAdapter(),
        new AuthorizeNetAdapter(),
        new CheckoutComAdapter(),
        new CsvAdapter(),
    ];
}
