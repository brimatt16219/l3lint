using System.Text;
using L3Lint.Adapters.Tests.Support;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Adapters.Tests;

/// <summary>The CSV adapter driven by a YAML column map (FR-006, US-14).</summary>
public sealed class CsvAdapterTests
{
    private const string SimpleMap = """
        format: csv
        groupBy: invoice
        transaction:
          amount: total
          currency: { const: USD }
        lineItems:
          description: item
          productCode: sku
          unitCost: price
        """;

    private static IReadOnlyList<AdapterResult> Map(string csv, string? map = SimpleMap, string? mapName = "test.map.yaml", bool verbose = false) =>
        new CsvAdapter().Map(
            RawDocument.FromCsv(Encoding.UTF8.GetBytes(csv), "test.csv"),
            new AdapterOptions { CsvMap = map, CsvMapName = mapName, Verbose = verbose });

    private static IReadOnlyList<AdapterResult> MapFixture(string id)
    {
        var fixture = Fixture.Load(id);
        return new CsvAdapter().Map(fixture.Document(), fixture.AdapterOptions());
    }

    [Fact]
    public void Rows_group_by_the_group_column_in_first_appearance_order()
    {
        var results = Map("invoice,total,item,sku,price\nB,5.00,Two,S2,5.00\nA,7.00,One,S1,3.00\nB,5.00,Three,S3,0.00\nA,7.00,Four,S4,4.00\n");

        results.Count.ShouldBe(2);
        results.Select(r => r.FirstLine).ShouldBe([2, 3]);
        results[0].Transaction.LineItems.Select(l => l.Description).ShouldBe(["Two", "Three"]);
        results[1].Transaction.LineItems.Select(l => l.Description).ShouldBe(["One", "Four"]);
        results[1].Transaction.LineItems.Select(l => l.Sequence).ShouldBe([1, 2]);
    }

    [Fact]
    public void Rows_that_disagree_on_a_transaction_value_give_an_invalid_note_at_the_disagreeing_cell()
    {
        var result = MapFixture("csv/02-rows-disagree").ShouldHaveSingleItem();

        result.Transaction.Amount.ShouldBeNull();
        result.InvalidFields.ShouldContain("/amount");
        var note = result.Notes.Single(n => n.Kind == AdapterNoteKind.Invalid);
        note.CanonicalPointer.ShouldBe("/amount");
        note.SourcePointer.ShouldBe("row 3, column B (total)");
        result.Paths.Resolve("/amount")!.Value.Location.ShouldBe(new SourceLocation(3, 11));
    }

    [Fact]
    public void Constants_value_maps_and_defaults_are_applied()
    {
        var results = MapFixture("csv/04-const-map-default");

        results[0].Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Exempt);
        results[0].Transaction.Tax.Amount.ShouldBe(0m);
        results[0].Transaction.Shipping.FromPostalCode.ShouldBe("64105");
        results[1].Transaction.Tax.Indicator.ShouldBe(TaxIndicator.Taxable);
        results[1].Transaction.LineItems[0].UnitOfMeasure.ShouldBe("EA");
        results[1].Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Defaulted);
        results[0].Paths.Resolve("/shipping/fromPostalCode").ShouldBeNull();
    }

    [Fact]
    public void Minor_units_use_each_transactions_currency_exponent()
    {
        var results = MapFixture("csv/03-minor-units-three-currencies");

        results.Select(r => r.Transaction.Amount).ShouldBe([280.36m, 27500m, 19.204m]);
        results.Select(r => r.Transaction.LineItems[0].UnitCost).ShouldBe([12.50m, 1250m, 2.750m]);
        results.Select(r => r.Transaction.LineItems[0].Quantity).ShouldBe([10m, 12m, 5m]);
    }

    [Fact]
    public void Minor_units_with_a_fraction_are_invalid()
    {
        var map = SimpleMap.Replace("format: csv", "format: csv\namountUnits: minor", StringComparison.Ordinal);

        var result = Map("invoice,total,item,sku,price\nA,12.5,One,S1,100\n", map).ShouldHaveSingleItem();

        result.InvalidFields.ShouldContain("/amount");
        result.Transaction.LineItems[0].UnitCost.ShouldBe(1.00m);
    }

    [Fact]
    public void A_card_number_in_a_cell_is_masked_with_its_line_and_column()
    {
        var fixture = Fixture.Load("csv/05-card-number-in-cell");
        var line = File.ReadAllLines(fixture.PathOf(".csv"))[1];
        var column = line.IndexOf("4111", StringComparison.Ordinal) + 1;

        var result = MapFixture("csv/05-card-number-in-cell").ShouldHaveSingleItem();

        var match = result.CardNumberMatches.ShouldHaveSingleItem();
        match.MaskedValue.ShouldBe("************1111");
        match.Brand.ShouldBe(CardBrand.Visa);
        match.CanonicalPointer.ShouldBe("/lineItems/0/description");
        match.IsCodeField.ShouldBeFalse();
        match.SourcePointer.ShouldBe("row 2, column M (item)");
        match.Location.ShouldBe(new SourceLocation(2, column));
        result.Transaction.LineItems[0].Description.ShouldBe("************1111");
    }

    [Fact]
    public void A_card_number_in_a_code_column_is_a_code_field_match()
    {
        var result = Map("invoice,total,item,sku,price\nA,1.00,Widget,5555555555554444,1.00\n").ShouldHaveSingleItem();

        var match = result.CardNumberMatches.ShouldHaveSingleItem();
        match.IsCodeField.ShouldBeTrue();
        match.CanonicalPointer.ShouldBe("/lineItems/0/productCode");
        match.Location.ShouldBe(new SourceLocation(2, 15));
        result.Transaction.LineItems[0].ProductCode.ShouldBe("************4444");
    }

    [Fact]
    public void A_card_number_in_an_unmapped_column_is_still_masked()
    {
        var result = Map("invoice,total,item,sku,price,notes\nA,1.00,Widget,W1,1.00,378282246310005\n").ShouldHaveSingleItem();

        var match = result.CardNumberMatches.ShouldHaveSingleItem();
        match.CanonicalPointer.ShouldBeNull();
        match.SourcePointer.ShouldBe("row 2, column F (notes)");
    }

    [Fact]
    public void Path_map_labels_cells_with_row_column_and_header()
    {
        var result = MapFixture("csv/01-erp-two-invoices")[0];

        var unitCost = result.Paths.Resolve("/lineItems/1/unitCost")!.Value;
        unitCost.Pointer.ShouldBe("row 3, column M (unit_price)");
        unitCost.Location.ShouldNotBeNull().Line.ShouldBe(3);
        result.Paths.Resolve("/lineItems")!.Value.Pointer.ShouldBe("rows 2 to 3");
        result.Paths.Resolve("/lineItems/0")!.Value.Pointer.ShouldBe("row 2");
        result.Paths.Resolve("/currency").ShouldBeNull();
        result.Paths.Resolve("/lineItems/0/sequence").ShouldBeNull();
        result.Paths.Resolve("/shipping/toPostalCode").ShouldBeNull();
    }

    [Fact]
    public void Cell_positions_follow_quoted_fields_and_line_breaks_inside_them()
    {
        var csv = "invoice,total,item,sku,price\r\nA,1.00,\"Line one\nline two\",S1,1.00\r\nA,1.00,Plain,S2,2.00\r\n";

        var result = Map(csv).ShouldHaveSingleItem();

        result.Transaction.LineItems[0].Description.ShouldBe("Line one\nline two");
        result.Paths.Resolve("/lineItems/0/productCode")!.Value.Location.ShouldBe(new SourceLocation(3, 11));
        result.Paths.Resolve("/lineItems/1/productCode")!.Value.Location.ShouldBe(new SourceLocation(4, 14));
        result.FirstLine.ShouldBe(2);
    }

    [Fact]
    public void Sequence_comes_from_row_order_unless_the_map_names_a_column()
    {
        MapFixture("csv/01-erp-two-invoices")[0].Transaction.LineItems.Select(l => l.Sequence).ShouldBe([1, 2]);
        MapFixture("csv/08-sequence-and-credit")[0].Transaction.LineItems.Select(l => l.DebitCreditIndicator)
            .ShouldBe([DebitCredit.Debit, DebitCredit.Debit, DebitCredit.Credit]);
    }

    [Fact]
    public void Missing_map_is_an_input_error()
    {
        var error = Should.Throw<InputException>(() => Map("invoice,total\nA,1\n", map: null));

        error.Kind.ShouldBe(InputErrorKind.Unsupported);
    }

    [Fact]
    public void Map_that_is_not_yaml_is_an_input_error_naming_the_map()
    {
        var error = Should.Throw<InputException>(() => Map("invoice,total\nA,1\n", map: "format: [csv", mapName: "erp-map.yaml"));

        error.Kind.ShouldBe(InputErrorKind.Unsupported);
        error.Message.ShouldContain("erp-map.yaml");
    }

    [Fact]
    public void Map_that_breaks_the_schema_is_an_input_error_naming_the_map_and_pointer()
    {
        var map = SimpleMap.Replace("unitCost: price", "unitCost: price\n  colour: shade", StringComparison.Ordinal);

        var error = Should.Throw<InputException>(() => Map("invoice,total,item,sku,price\nA,1,x,y,1\n", map, "erp-map.yaml"));

        error.Kind.ShouldBe(InputErrorKind.Unsupported);
        error.Message.ShouldContain("erp-map.yaml");
        error.Message.ShouldContain("/lineItems");
    }

    [Fact]
    public void Map_naming_a_column_the_file_lacks_is_an_input_error()
    {
        var error = Should.Throw<InputException>(() => Map("invoice,total,item,sku\nA,1,x,y\n"));

        error.Kind.ShouldBe(InputErrorKind.Unsupported);
        error.Message.ShouldContain("\"price\"");
        error.Message.ShouldContain("/lineItems/unitCost");
    }

    [Fact]
    public void Empty_file_is_an_input_error()
    {
        Should.Throw<InputException>(() => Map(string.Empty)).Kind.ShouldBe(InputErrorKind.Malformed);
    }

    [Fact]
    public void Header_only_file_has_no_transactions() =>
        Map("invoice,total,item,sku,price\n").ShouldBeEmpty();

    [Fact]
    public void Unmapped_columns_are_reported_only_with_verbose()
    {
        const string Csv = "invoice,total,item,sku,price,notes\nA,1.00,Widget,W1,1.00,hello\n";

        Map(Csv).ShouldHaveSingleItem().Notes.ShouldNotContain(n => n.Kind == AdapterNoteKind.Ignored);
        Map(Csv, verbose: true).ShouldHaveSingleItem().Notes.ShouldContain(n => n.Kind == AdapterNoteKind.Ignored && n.Message.Contains("notes", StringComparison.Ordinal));
    }

    [Fact]
    public void Json_input_is_refused()
    {
        Should.Throw<InputException>(() => new CsvAdapter().Map(RawDocument.ParseJson("{}", "x.json"), new AdapterOptions { CsvMap = SimpleMap }))
            .Kind.ShouldBe(InputErrorKind.Unsupported);
    }

    [Fact]
    public void Values_that_do_not_convert_become_invalid_notes()
    {
        var results = MapFixture("csv/06-invalid-cells");

        results[0].InvalidFields.ShouldBe(["/lineItems/0/quantity", "/lineItems/1/total", "/tax/indicator"], ignoreOrder: true);
        results[0].Notes.Where(n => n.Kind == AdapterNoteKind.Invalid).Select(n => n.SourcePointer)
            .ShouldBe(["row 2, column F (tax_flag)", "row 2, column P (qty)", "row 3, column U (line_total)"], ignoreOrder: true);
    }
}
