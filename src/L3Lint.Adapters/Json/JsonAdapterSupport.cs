using System.Collections.Frozen;
using System.Text.Json;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Json;

/// <summary>Steps every JSON gateway adapter shares: input checks, currency, derived values and the result.</summary>
internal static class JsonAdapterSupport
{
    /// <summary>The exponent assumed when the currency is missing or unknown (FR-008).</summary>
    public const int AssumedExponent = 2;

    /// <summary>Returns the document root, or throws when the document is not JSON.</summary>
    /// <exception cref="InputException">The document is CSV.</exception>
    public static JsonElement RequireJson(RawDocument document, string adapterName)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Kind != InputKind.Json)
        {
            throw new InputException(InputErrorKind.Unsupported, Messages.Format(Resources.Strings.AdapterNeedsJson, adapterName), document.SourceName);
        }

        return document.Json;
    }

    /// <summary>
    /// Sets the currency used for minor units. A missing or unknown code assumes 2 decimal places and adds an
    /// Ambiguous note, because a wrong exponent would scale every amount.
    /// </summary>
    public static void UseCurrencyForMinorUnits(PayloadReader reader, string? code, string sourcePointer)
    {
        if (Iso4217.TryGetCurrency(code, out var currency))
        {
            reader.MinorUnitCurrency = currency;
            return;
        }

        reader.MinorUnitCurrency = new Currency(code ?? string.Empty, AssumedExponent);
        var message = code is null
            ? Messages.Format(Resources.Strings.NoteCurrencyMissingAssumed, AssumedExponent)
            : Messages.Format(Resources.Strings.NoteCurrencyUnknownAssumed, code, AssumedExponent);
        reader.Log.Add(AdapterNoteKind.Ambiguous, sourcePointer, "/currency", message);
    }

    /// <summary>Uses the default currency from the options when the payload has none (Braintree, sometimes Authorize.net).</summary>
    public static string? DefaultCurrency(string? sent, AdapterOptions options, MappingLog log, string gateway, string? sourcePointer)
    {
        if (sent is not null || log.IsInvalid("/currency") || options.DefaultCurrency is not { } fallback)
        {
            return sent;
        }

        log.Add(AdapterNoteKind.Defaulted, sourcePointer, "/currency", Messages.Format(Resources.Strings.NoteCurrencyDefaulted, gateway, fallback));
        return fallback;
    }

    /// <summary>
    /// Sums line tax amounts into the header tax when the header tax was not sent but lines carry tax (Stripe
    /// sends tax at one level only; Cybersource sends header tax for few processors).
    /// </summary>
    public static Transaction TaxFromLines(Transaction transaction, MappingLog log, string sourcePointer)
    {
        if (transaction.Tax.Amount is not null || log.IsInvalid("/tax/amount") || !transaction.LineItems.Any(l => l.TaxAmount is not null))
        {
            return transaction;
        }

        for (var i = 0; i < transaction.LineItems.Count; i++)
        {
            if (log.IsInvalid(CanonicalAssembler.LinePointer(i, "taxAmount")))
            {
                return transaction;
            }
        }

        var sum = transaction.LineItems.Sum(l => l.TaxAmount ?? 0m);
        log.AddDerived("/tax/amount", sourcePointer, Resources.Strings.NoteDerivedTaxFromLines);
        return transaction with { Tax = transaction.Tax with { Amount = sum } };
    }

    /// <summary>
    /// For formats with no tax indicator field: a tax amount above 0 means taxable. Zero or absent tax leaves the
    /// indicator absent, because an exemption is never guessed.
    /// </summary>
    public static Transaction TaxIndicatorFromAmount(Transaction transaction, MappingLog log, string gateway)
    {
        if (transaction.Tax.Indicator is not null || transaction.Tax.Amount is not > 0m)
        {
            return transaction;
        }

        log.AddDerived("/tax/indicator", null, Messages.Format(Resources.Strings.NoteDerivedTaxIndicator, gateway));
        return transaction with { Tax = transaction.Tax with { Indicator = TaxIndicator.Taxable } };
    }

    /// <summary>Adds an Ambiguous note when an explicit tax flag disagrees with the tax amount.</summary>
    public static void CheckTaxFlag(Transaction transaction, MappingLog log, string flagPointer, string gateway)
    {
        var tax = transaction.Tax;
        if (tax.Indicator == TaxIndicator.Exempt && tax.Amount is > 0m)
        {
            log.Add(AdapterNoteKind.Ambiguous, flagPointer, "/tax/indicator", Messages.Format(Resources.Strings.NoteExemptWithTax, gateway, flagPointer));
        }
        else if (tax.Indicator == TaxIndicator.Taxable && tax.Amount is not > 0m)
        {
            log.Add(AdapterNoteKind.Ambiguous, flagPointer, "/tax/indicator", Messages.Format(Resources.Strings.NoteTaxableWithoutTax, gateway, flagPointer));
        }
    }

    /// <summary>Builds the result from the transaction, path map and log.</summary>
    public static AdapterResult Result(Transaction transaction, PathMap paths, MappingLog log) => new(transaction, paths)
    {
        DerivedFields = log.Derived.ToFrozenSet(StringComparer.Ordinal),
        Notes = [.. log.Notes],
        InvalidFields = log.Invalid.ToFrozenSet(StringComparer.Ordinal),
    };
}
