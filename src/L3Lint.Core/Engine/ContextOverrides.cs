using System.Globalization;
using L3Lint.Core.Model;

namespace L3Lint.Core.Engine;

/// <summary>
/// Builds the effective transaction: options beat the payload, which beats defaults (ADR-0042). Every override
/// that changes a value the payload sent adds a note, so a reviewer can see why a rule ran.
/// </summary>
internal static class ContextOverrides
{
    public const string DefaultMerchantCountry = "US";

    public static Transaction Apply(Transaction transaction, LintOptions options, List<string> notes)
    {
        var card = transaction.Card;
        var merchant = transaction.Merchant;
        var defaults = options.Defaults;

        // Config defaults fill only what the payload left out; flags below override what the payload sent.
        if (card.Brand is null or CardBrand.Unknown && defaults.Brand is { } defaultBrand)
        {
            card = card with { Brand = defaultBrand };
        }

        if (card.Product is null or CardProduct.Unknown && defaults.CardProduct is { } defaultProduct)
        {
            card = card with { Product = defaultProduct };
        }

        if (merchant.Mcc is null && defaults.Mcc is { } defaultMcc)
        {
            merchant = merchant with { Mcc = defaultMcc };
        }

        if (merchant.Country is null && defaults.MerchantCountry is { } defaultCountry)
        {
            merchant = merchant with { Country = defaultCountry };
        }

        if (!options.AllBrands && options.Brand is { } brand && brand != card.Brand)
        {
            if (transaction.Card.Brand is { } sent && sent != CardBrand.Unknown)
            {
                notes.Add(Note(Resources.Strings.NoteBrandOverride, CanonicalNames.ToName(brand), CanonicalNames.ToName(sent)));
            }

            card = card with { Brand = brand };
        }

        if (options.CardProduct is { } product && product != card.Product)
        {
            if (transaction.Card.Product is { } sent && sent != CardProduct.Unknown)
            {
                notes.Add(Note(Resources.Strings.NoteProductOverride, CanonicalNames.ToName(product), CanonicalNames.ToName(sent)));
            }

            card = card with { Product = product };
        }

        if (options.Mcc is { } mcc && mcc != merchant.Mcc)
        {
            if (transaction.Merchant.Mcc is { } sent)
            {
                notes.Add(Note(Resources.Strings.NoteMccOverride, mcc, sent));
            }

            merchant = merchant with { Mcc = mcc };
        }

        if (options.MerchantCountry is { } country && country != merchant.Country)
        {
            if (transaction.Merchant.Country is { } sent)
            {
                notes.Add(Note(Resources.Strings.NoteCountryOverride, country, sent));
            }

            merchant = merchant with { Country = country };
        }
        else if (merchant.Country is null)
        {
            merchant = merchant with { Country = DefaultMerchantCountry };
        }

        return ReferenceEquals(card, transaction.Card) && ReferenceEquals(merchant, transaction.Merchant)
            ? transaction
            : transaction with { Card = card, Merchant = merchant };
    }

    private static string Note(string template, params object[] args) => string.Format(CultureInfo.InvariantCulture, template, args);
}
