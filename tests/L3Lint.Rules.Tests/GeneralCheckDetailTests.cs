using System.Globalization;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Input;

namespace L3Lint.Rules.Tests;

/// <summary>
/// The full finding of each GEN custom check: path, rendered message, actual value and patch. The rule examples
/// prove that a rule fires; these prove what it says. Every expected message is the rule's template from
/// general.rules.yaml filled with the values the mutation put into the spec example (Card brand rules tab), so no
/// expectation comes from running l3lint.
/// </summary>
public sealed class GeneralCheckDetailTests
{
    private static readonly string SpecExample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    [Fact]
    public void A_repeated_line_names_both_lines_and_its_description()
    {
        var finding = Single("GEN-DQ-008", p => p["lineItems"]!.AsArray().Add(p["lineItems"]![0]!.DeepClone()));

        finding.Path.Canonical.ShouldBe("/lineItems/2");
        finding.Message.ShouldBe("Line 3 repeats line 1: same description, product code, quantity and unit cost.");
        finding.Actual.ShouldBe("Nitrile exam gloves L 100");
    }

    [Fact]
    public void A_repeated_line_without_a_description_shows_its_product_code()
    {
        var finding = Single("GEN-DQ-008", p =>
        {
            p["lineItems"]![0]!.AsObject().Remove("description");
            p["lineItems"]!.AsArray().Add(p["lineItems"]![0]!.DeepClone());
        });

        finding.Actual.ShouldBe("GLV-NTR-L100");
    }

    [Fact]
    public void Lines_without_description_or_product_code_are_not_compared()
    {
        var findings = Findings("GEN-DQ-008", p =>
        {
            p["lineItems"]![0]!.AsObject().Remove("description");
            p["lineItems"]![0]!.AsObject().Remove("productCode");
            p["lineItems"]!.AsArray().Add(p["lineItems"]![0]!.DeepClone());
        });

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void A_wrong_sequence_says_what_it_should_be_and_patches_it()
    {
        var finding = Single("GEN-FMT-007", p => p["lineItems"]![1]!["sequence"] = 5);

        finding.Path.Canonical.ShouldBe("/lineItems/1/sequence");
        finding.Message.ShouldBe("Line 2 has sequence 5; it should be 2.");
        finding.Actual.ShouldBe("5");
        finding.Expected.ShouldBe("2");
        var operation = finding.Patch.ShouldNotBeNull().Operations.ShouldHaveSingleItem();
        operation.Op.ShouldBe("add");
        operation.Path.ShouldBe("/lineItems/1/sequence");
        operation.Value!.ToJsonString().ShouldBe("2");
    }

    [Fact]
    public void A_missing_sequence_uses_the_missing_message()
    {
        var finding = Single("GEN-FMT-007", p => p["lineItems"]![1]!.AsObject().Remove("sequence"));

        finding.Message.ShouldBe("Line 2 has no sequence number; it should be 2.");
        finding.Actual.ShouldBeNull();
    }

    [Fact]
    public void A_missing_currency_uses_the_missing_message()
    {
        var finding = Single("GEN-FMT-002", p => p.Remove("currency"));

        finding.Message.ShouldBe("The currency is missing.");
        finding.Patch.ShouldBeNull();
    }

    [Fact]
    public void A_lower_case_currency_is_patched_to_upper_case()
    {
        var finding = Single("GEN-FMT-002", p => p["currency"] = "usd");

        finding.Message.ShouldBe("Currency usd is not an ISO 4217 code.");
        finding.Actual.ShouldBe("usd");
        var operation = finding.Patch.ShouldNotBeNull().Operations.ShouldHaveSingleItem();
        operation.Op.ShouldBe("add");
        operation.Path.ShouldBe("/currency");
        operation.Value!.ToJsonString().ShouldBe("\"USD\"");
    }

    [Fact]
    public void An_unknown_currency_has_no_mechanical_fix()
    {
        var finding = Single("GEN-FMT-002", p => p["currency"] = "ABC");

        finding.Message.ShouldBe("Currency ABC is not an ISO 4217 code.");
        finding.Patch.ShouldBeNull();
    }

    [Fact]
    public void A_currency_of_the_wrong_type_is_left_to_the_schema_finding()
    {
        // ADR-0036: one bad value gives one finding.
        var result = Lint(p => p["currency"] = 840);

        result.Select(f => f.RuleId).ShouldContain("GEN-FMT-001");
        result.Select(f => f.RuleId).ShouldNotContain("GEN-FMT-002");
    }

    [Fact]
    public void A_merchant_name_as_description_names_the_line()
    {
        // On a Visa payload VISA-DQ-001 supersedes GEN-DQ-002 (ADR-0032), so this uses a Mastercard card.
        var finding = Single("GEN-DQ-002", p =>
        {
            p["card"]!["brand"] = "mastercard";
            p["lineItems"]![1]!["description"] = "Gulfside Medical Supply";
        });

        finding.Path.Canonical.ShouldBe("/lineItems/1/description");
        finding.Message.ShouldBe("Line 2 description \"Gulfside Medical Supply\" is the merchant name.");
    }

    [Fact]
    public void A_negative_quantity_on_a_debit_line_names_the_line_and_field()
    {
        var finding = Single("GEN-ARITH-004", p => p["lineItems"]![1]!["quantity"] = -2);

        finding.Path.Canonical.ShouldBe("/lineItems/1/quantity");
        finding.Message.ShouldBe("Line 2 is a debit line with a negative value at /lineItems/1/quantity: -2.");
        finding.Actual.ShouldBe("-2");
    }

    [Fact]
    public void A_negative_quantity_on_a_credit_line_is_allowed()
    {
        var findings = Findings("GEN-ARITH-004", p =>
        {
            p["lineItems"]![1]!["quantity"] = -2;
            p["lineItems"]![1]!["debitCreditIndicator"] = "credit";
        });

        findings.ShouldBeEmpty();
    }

    [Fact]
    public void A_placeholder_product_code_names_the_line_and_value()
    {
        // On a Visa payload VISA-DQ-002 supersedes GEN-DQ-003 (ADR-0032), so this uses a Mastercard card.
        var finding = Single("GEN-DQ-003", p =>
        {
            p["card"]!["brand"] = "mastercard";
            p["lineItems"]![1]!["productCode"] = "SKU";
        });

        finding.Path.Canonical.ShouldBe("/lineItems/1/productCode");
        finding.Message.ShouldBe("Line 2 has a placeholder value at /lineItems/1/productCode: \"SKU\".");
        finding.Actual.ShouldBe("SKU");
    }

    [Fact]
    public void A_filler_customer_reference_shows_the_value()
    {
        var finding = Single("GEN-DQ-004", p => p["customer"]!["reference"] = "123456");

        finding.Path.Canonical.ShouldBe("/customer/reference");
        finding.Message.ShouldBe("\"123456\" at /customer/reference looks like filler, not the buyer's reference.");
        finding.Actual.ShouldBe("123456");
    }

    [Fact]
    public void An_invalid_line_total_is_left_to_the_schema_finding()
    {
        // ADR-0036: a line total of the wrong type stops the transaction total check instead of adding a second finding.
        var result = Lint(p => p["lineItems"]![0]!["total"] = "120,00");

        result.Select(f => f.RuleId).ShouldContain("GEN-FMT-001");
        result.Select(f => f.RuleId).ShouldNotContain("GEN-ARITH-002");
    }

    private static Finding Single(string ruleId, Action<JsonObject> mutation) => Findings(ruleId, mutation).ShouldHaveSingleItem();

    private static List<Finding> Findings(string ruleId, Action<JsonObject> mutation) => [.. Lint(mutation).Where(f => f.RuleId == ruleId)];

    private static IReadOnlyList<Finding> Lint(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(SpecExample)!.AsObject();
        mutation(root);
        return Linters.Packs.Lint(RawDocument.ParseJson(root.ToJsonString(), "p.json")).Findings;
    }
}

/// <summary>
/// GEN-DQ-007 over a run (Card brand rules tab: the same description or product code on at least 80% of at least 20
/// transactions; ADR-0034 for where the finding points). Each test lints copies of the spec example in one session.
/// </summary>
public sealed class StaticBatchDataTests
{
    private const string RuleId = "GEN-DQ-007";

    private static readonly string SpecExample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpecExamples", "canonical-example.json"));

    [Fact]
    public void Every_value_over_the_share_is_reported_in_first_transaction_then_path_order()
    {
        // Transaction 0 has its own description on line 2, so "Isolation gown blue cs 50" first appears in transaction 1.
        var findings = Batch(20, (i, p) =>
        {
            if (i == 0)
            {
                p["lineItems"]![1]!["description"] = "Unique gown A";
            }
        });

        findings.Select(f => (f.Path.Canonical, f.Actual, f.TransactionIndex)).ShouldBe(
        [
            ("/lineItems/0/description", "Nitrile exam gloves L 100", 0),
            ("/lineItems/0/productCode", "GLV-NTR-L100", 0),
            ("/lineItems/1/productCode", "GWN-ISO-BL50", 0),
            ("/lineItems/1/description", "Isolation gown blue cs 50", 1),
        ]);
        findings[0].Message.ShouldBe("\"Nitrile exam gloves L 100\" appears at /lineItems/0/description on 20 of 20 transactions (100%).");
        findings[3].Message.ShouldBe("\"Isolation gown blue cs 50\" appears at /lineItems/1/description on 19 of 20 transactions (95%).");
    }

    [Fact]
    public void Exactly_the_share_is_enough_and_less_is_not()
    {
        // 16 of 20 is exactly 80%; 15 of 20 is below it.
        var atShare = Batch(20, (i, p) => p["lineItems"]![1]!["productCode"] = i < 4 ? "GWN-OTHER-" + Text(i) : "GWN-ISO-BL50");
        var belowShare = Batch(20, (i, p) => p["lineItems"]![1]!["productCode"] = i < 5 ? "GWN-OTHER-" + Text(i) : "GWN-ISO-BL50");

        var finding = atShare.Single(f => f.Path.Canonical == "/lineItems/1/productCode");
        finding.Message.ShouldBe("\"GWN-ISO-BL50\" appears at /lineItems/1/productCode on 16 of 20 transactions (80%).");
        finding.TransactionIndex.ShouldBe(4);
        belowShare.ShouldNotContain(f => f.Path.Canonical == "/lineItems/1/productCode");
    }

    [Fact]
    public void The_finding_points_at_the_first_transaction_in_run_order_whatever_order_they_were_linted_in()
    {
        var findings = Batch(20, (_, _) => { }, order: [5, 0, 1, 2, 3, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]);

        findings.ShouldAllBe(f => f.TransactionIndex == 0);
    }

    [Fact]
    public void A_value_counts_once_per_transaction_and_blank_values_are_ignored()
    {
        var findings = Batch(20, (_, p) =>
        {
            p["lineItems"]![1]!["description"] = "Nitrile exam gloves L 100";
            p["lineItems"]![1]!["productCode"] = "   ";
        });

        findings.Single(f => f.Path.Canonical == "/lineItems/0/description").Message
            .ShouldBe("\"Nitrile exam gloves L 100\" appears at /lineItems/0/description on 20 of 20 transactions (100%).");
        findings.ShouldNotContain(f => string.IsNullOrWhiteSpace(f.Actual));
    }

    [Fact]
    public void Fewer_than_twenty_transactions_are_never_checked()
    {
        Batch(19, (_, _) => { }).ShouldBeEmpty();
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static List<Finding> Batch(int count, Action<int, JsonObject> mutation, int[]? order = null)
    {
        var session = Linters.Packs.CreateSession();
        foreach (var i in order ?? [.. Enumerable.Range(0, count)])
        {
            var root = JsonNode.Parse(SpecExample)!.AsObject();
            root["id"] = "INV-" + Text(20000 + i);
            mutation(i, root);
            session.Lint(RawDocument.ParseJson(root.ToJsonString(), "t" + Text(i) + ".json"), i, 0);
        }

        return [.. (session.Complete(count)?.Findings ?? []).Where(f => f.RuleId == RuleId)];
    }
}
