using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core.Tests.Properties;

/// <summary>
/// Canonical JSON round trip for generated transactions (T-06, "Model round-trips every canonical fixture";
/// ADR-0011, "The C# model mirrors the canonical JSON"): writing a transaction and reading it back gives an equal
/// transaction, and writing that again gives the same bytes, decimal scale included.
/// </summary>
public sealed class CanonicalJsonProperties
{
    [Property(MaxTest = 1000)]
    public Property Serialize_then_read_gives_an_equal_transaction() =>
        Prop.ForAll(Transactions.All().ToArbitrary(), transaction =>
        {
            var json = CanonicalJson.Serialize(transaction);

            var result = new CanonicalAdapter().Map(RawDocument.ParseJson(json, "generated"), AdapterOptions.Default).ShouldHaveSingleItem();

            result.InvalidFields.ShouldBeEmpty(json);
            Transactions.Same(result.Transaction, transaction).ShouldBeTrue(json);
        });

    [Property(MaxTest = 1000)]
    public Property Writing_is_byte_identical_after_a_round_trip() =>
        Prop.ForAll(Transactions.All().ToArbitrary(), transaction =>
        {
            var first = CanonicalJson.SerializeToUtf8Bytes(transaction);

            var read = new CanonicalAdapter().Map(RawDocument.ParseJson(first, "generated"), AdapterOptions.Default)[0].Transaction;

            CanonicalJson.SerializeToUtf8Bytes(read).ShouldBe(first);
            CanonicalJson.Serialize(read).ShouldBe(CanonicalJson.Serialize(transaction));
        });
}

/// <summary>Generators for canonical transactions with every field optional, as the model allows.</summary>
internal static class Transactions
{
    // Letters, spaces, punctuation and a few non-ASCII letters. Digits appear only in short runs, so no value can
    // look like a card number (13 to 19 digits).
    private static readonly string TextChars = "abcxyzABCXYZ -_.,'" + '"' + "/&<>#" + new string([(char)0x5C, (char)0xE9, (char)0xDF, (char)0x65E5, (char)0xF1]);

    public static Gen<Transaction> All() =>
        from header in Header()
        from card in Card()
        from merchant in Merchant()
        from customer in Optional(Text()).Select(r => new CustomerInfo { Reference = r })
        from order in Order()
        from tax in Tax()
        from amounts in Amounts()
        from shipping in Shipping()
        from billing in Optional(Code()).Select(p => new BillingInfo { PostalCode = p })
        from amex in Amex()
        from count in Gen.Choose(0, 5)
        from lines in Gen.ListOf(Line(), count)
        select header with
        {
            Card = card,
            Merchant = merchant,
            Customer = customer,
            Order = order,
            Tax = tax,
            Amounts = amounts,
            Shipping = shipping,
            Billing = billing,
            Amex = amex,
            LineItems = [.. lines],
        };

    /// <summary>Value equality, comparing the two lists in the model element by element.</summary>
    public static bool Same(Transaction actual, Transaction expected) =>
        actual.LineItems.SequenceEqual(expected.LineItems)
        && (actual.Amex.AdviceText ?? []).SequenceEqual(expected.Amex.AdviceText ?? [])
        && (actual.Amex.AdviceText is null) == (expected.Amex.AdviceText is null)
        && actual with { LineItems = expected.LineItems, Amex = expected.Amex } == expected
        && actual.Amex.RequestorName == expected.Amex.RequestorName;

    private static Gen<Transaction> Header() =>
        from schemaVersion in Gen.Elements<string?>("1.0", "1.0", null)
        from id in Optional(Code())
        from amount in Optional(Amount())
        from currency in Optional(Gen.Elements("USD", "JPY", "BHD", "EUR", "CLF"))
        from date in Optional(Date())
        from channel in Optional(Gen.Elements(Channel.CardPresent, Channel.CardNotPresent))
        select new Transaction
        {
            SchemaVersion = schemaVersion,
            Id = id,
            Amount = amount,
            Currency = currency,
            TransactionDate = date,
            Channel = channel,
        };

    private static Gen<CardInfo> Card() =>
        from brand in Optional(Gen.Elements(Enum.GetValues<CardBrand>()))
        from product in Optional(Gen.Elements(Enum.GetValues<CardProduct>()))
        from productCode in Optional(Code())
        from federal in Optional(Gen.Elements(true, false))
        from last4 in Optional(Gen.Choose(0, 9999).Select(i => i.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)))
        select new CardInfo { Brand = brand, Product = product, ProductCode = productCode, IsFederalGovernment = federal, Last4 = last4 };

    private static Gen<MerchantInfo> Merchant() =>
        from mcc in Optional(Gen.Choose(0, 9999).Select(i => i.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)))
        from name in Optional(Text())
        from country in Optional(Gen.Elements("US", "CA", "GB", "DE"))
        from taxId in Optional(Code())
        select new MerchantInfo { Mcc = mcc, Name = name, Country = country, TaxId = taxId };

    private static Gen<OrderInfo> Order() =>
        from po in Optional(Code())
        from invoice in Optional(Code())
        from date in Optional(Date())
        select new OrderInfo { PurchaseOrderNumber = po, InvoiceNumber = invoice, OrderDate = date };

    private static Gen<TaxInfo> Tax() =>
        from amount in Optional(Amount())
        from indicator in Optional(Gen.Elements(Enum.GetValues<TaxIndicator>()))
        from rate in Optional(Rate())
        from exemption in Optional(Code())
        select new TaxInfo { Amount = amount, Indicator = indicator, Rate = rate, ExemptionId = exemption };

    private static Gen<SummaryAmounts> Amounts() =>
        from discount in Optional(Amount())
        from freight in Optional(Amount())
        from duty in Optional(Amount())
        from shippingTax in Optional(Amount())
        from handling in Optional(Amount())
        select new SummaryAmounts { Discount = discount, Freight = freight, Duty = duty, ShippingTax = shippingTax, Handling = handling };

    private static Gen<ShippingInfo> Shipping() =>
        from origin in Optional(Code())
        from to in Optional(Code())
        from country in Optional(Gen.Elements("US", "MX", "FR"))
        select new ShippingInfo { FromPostalCode = origin, ToPostalCode = to, ToCountry = country };

    private static Gen<AmexInfo> Amex() =>
        from requestor in Optional(Text())
        from hasAdvice in Gen.Elements(true, false)
        from count in Gen.Choose(0, 3)
        from advice in Gen.ListOf(Text(), count)
        select new AmexInfo { RequestorName = requestor, AdviceText = hasAdvice ? [.. advice] : null };

    private static Gen<LineItem> Line() =>
        from sequence in Optional(Gen.Choose(-5, 1000))
        from description in Optional(Text())
        from productCode in Optional(Code())
        from commodityCode in Optional(Gen.Choose(0, 99_999_999).Select(i => i.ToString("D8", System.Globalization.CultureInfo.InvariantCulture)))
        from scheme in Optional(Gen.Elements(Enum.GetValues<CommodityScheme>()))
        from quantity in Optional(Amount())
        from unit in Optional(Gen.Elements("EA", "BX", "CA", "KGM", "PCE"))
        from unitCost in Optional(Amount())
        from discount in Optional(Amount())
        from discountIndicator in Optional(Gen.Elements(true, false))
        from taxAmount in Optional(Amount())
        from taxRate in Optional(Rate())
        from taxable in Optional(Gen.Elements(true, false))
        from taxType in Optional(Code())
        from includesTax in Optional(Gen.Elements(true, false))
        from total in Optional(Amount())
        from debitCredit in Optional(Gen.Elements(DebitCredit.Debit, DebitCredit.Credit))
        from detail in Optional(Gen.Elements("Y", "N", "D"))
        select new LineItem
        {
            Sequence = sequence,
            Description = description,
            ProductCode = productCode,
            CommodityCode = commodityCode,
            CommodityScheme = scheme,
            Quantity = quantity,
            UnitOfMeasure = unit,
            UnitCost = unitCost,
            DiscountAmount = discount,
            DiscountIndicator = discountIndicator,
            TaxAmount = taxAmount,
            TaxRate = taxRate,
            Taxable = taxable,
            TaxTypeCode = taxType,
            AmountIncludesTax = includesTax,
            Total = total,
            DebitCreditIndicator = debitCredit,
            DetailIndicator = detail,
        };

    /// <summary>Decimals with 0 to 4 places, scale kept (so 12.50 stays 12.50), positive or negative.</summary>
    private static Gen<decimal> Amount() =>
        from units in Gen.Choose(-2_000_000, 2_000_000)
        from scale in Gen.Choose(0, 4)
        select new decimal(Math.Abs(units), 0, 0, units < 0, (byte)scale);

    private static Gen<decimal> Rate() => Gen.Choose(0, 3000).Select(i => new decimal(i, 0, 0, false, 4));

    private static Gen<DateOnly> Date() => Gen.Choose(0, 36_500).Select(d => new DateOnly(2000, 1, 1).AddDays(d));

    private static Gen<string> Text() =>
        from length in Gen.Choose(0, 24)
        from chars in Gen.ListOf(Gen.Elements(TextChars.ToCharArray()), length)
        select new string([.. chars]);

    private static Gen<string> Code() =>
        from letters in Gen.Choose(1, 4).SelectMany(n => Gen.ListOf(Gen.Elements("ABCXYZ".ToCharArray()), n))
        from number in Gen.Choose(0, 999_999)
        select new string([.. letters]) + "-" + number.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static Gen<T?> Optional<T>(Gen<T> gen)
        where T : struct =>
        Gen.Frequency((1, Gen.Constant<T?>(null)), (3, gen.Select(v => (T?)v)));

    private static Gen<string?> Optional(Gen<string> gen) =>
        Gen.Frequency((1, Gen.Constant<string?>(null)), (3, gen.Select(v => (string?)v)));
}
