using L3Lint.Adapters.Tests.Support;
using L3Lint.Core;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;

namespace L3Lint.Adapters.Tests;

/// <summary>
/// Path maps (FR-007): every canonical field resolves, source pointers translate back to the same canonical pointer,
/// and findings carry the source path.
/// </summary>
public sealed class PathMapTests
{
    // The legacy line-gap fixture numbers its lines 1 and 3; the inverse map cannot know the gap, so it is left out here.
    public static TheoryData<string> JsonFixtures() =>
    [
        .. Fixture.AllIds().Where(id => !id.StartsWith("csv/", StringComparison.Ordinal) && id != "adyen/06-legacy-line-gap" && !Fixture.Load(id).IsTie),
    ];

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void Every_source_pointer_translates_back_to_its_canonical_pointer(string id)
    {
        var fixture = Fixture.Load(id);
        var adapter = Linters.Adapter(fixture.Format);
        var result = Mapped.Fixture(id);
        var lines = Math.Max(result.Transaction.LineItems.Count, 2);

        var checkedCount = 0;
        foreach (var field in CanonicalFields.All)
        {
            var pointers = field.IsLineField
                ? Enumerable.Range(0, lines).Select(i => Pointers.Line(i, field.Name))
                : [field.Path];
            foreach (var pointer in pointers)
            {
                if (result.Paths.Resolve(pointer) is not { } source)
                {
                    continue;
                }

                adapter.ToCanonicalPointer(source.Pointer).ShouldBe(pointer, $"{id}: {source.Pointer}");
                checkedCount++;
            }
        }

        checkedCount.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void Source_pointers_outside_any_mapped_field_translate_to_null()
    {
        Linters.Adapter("stripe").ToCanonicalPointer("/automatic_payment_methods/enabled").ShouldBeNull();
        Linters.Adapter("adyen").ToCanonicalPointer("/paymentMethod/number").ShouldBeNull();
        Linters.Adapter("authorizenet").ToCanonicalPointer("/createTransactionRequest/merchantAuthentication/name").ShouldBeNull();
        Linters.Adapter("checkoutcom").ToCanonicalPointer("/source/token").ShouldBeNull();
        Linters.Adapter("cybersource").ToCanonicalPointer("not a pointer").ShouldBeNull();
        Linters.Adapter("csv").ToCanonicalPointer("row 2, column A (invoice)").ShouldBeNull();
    }

    [Theory]
    [InlineData("adyen", "/additionalData/enhancedSchemeData.itemDetailLine3.productCode", "/lineItems/2/productCode")]
    [InlineData("adyen", "/additionalData/enhancedSchemeData.itemDetailLine0.productCode", null)]
    [InlineData("authorizenet", "/transactionRequest/lineItems/lineItem/name", "/lineItems/0/description")]
    [InlineData("authorizenet", "/createTransactionRequest/transactionRequest/lineItems/lineItem/2/productSKU", "/lineItems/2/productCode")]
    [InlineData("cybersource", "/orderInformation/lineItems/1/productSKU", "/lineItems/1/productCode")]
    [InlineData("braintree", "/shipping/countryCodeAlpha3", "/shipping/toCountry")]
    [InlineData("stripe", "/amount_details/line_items/12/payment_method_options/card/commodity_code", "/lineItems/12/commodityCode")]
    public void Alternate_source_shapes_translate_to_canonical_pointers(string adapter, string source, string? canonical) =>
        Linters.Adapter(adapter).ToCanonicalPointer(source).ShouldBe(canonical);

    [Fact]
    public void A_finding_on_a_missing_field_carries_the_source_path_where_the_gateway_would_carry_it()
    {
        var fixture = Fixture.Load("stripe/03-missing-line-fields");

        var result = Linters.All.Lint(fixture.Document(), fixture.LintOptions());

        var finding = result.Findings.Single(f => f.RuleId == "VISA-L3-004");
        finding.Path.ShouldBe(new FieldPath("/lineItems/1/commodityCode", "/amount_details/line_items/1/payment_method_options/card/commodity_code"));
        finding.Location.ShouldNotBeNull();
    }

    [Fact]
    public void A_mechanical_fix_patch_is_written_in_source_units()
    {
        var text = File.ReadAllText(Fixture.Load("checkoutcom/01-complete-level3").PathOf(".json"))
            .Replace("\"total_amount\": 12000", "\"total_amount\": 12500", StringComparison.Ordinal);

        var result = Linters.All.Lint(RawDocument.ParseJson(text, "patch.json"), Fixture.Load("checkoutcom/01-complete-level3").LintOptions());

        var finding = result.Findings.Single(f => f.RuleId == "GEN-ARITH-001");
        finding.Path.Source.ShouldBe("/items/0/total_amount");
        var operation = finding.Patch.ShouldNotBeNull().Operations.ShouldHaveSingleItem();
        operation.Path.ShouldBe("/items/0/total_amount");
        operation.Value!.ToJsonString().ShouldBe("12000");
    }
}
