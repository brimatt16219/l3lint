using L3Lint.Core;
using L3Lint.Core.Estimates;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Tiers;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Reports built by hand, so every expected value in the tests follows from these lines and the spec rather than
/// from running a writer.
/// </summary>
internal static class TestReports
{
    public const string ConfigHash = "3f9a1c2b7d4e";

    public static readonly Uri Payroc = new("https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program");
    public static readonly Uri Versapay = new("https://www.versapay.com/cedp");

    public static RuleMetadata VisaL3004 { get; } = new()
    {
        Id = "VISA-L3-004",
        Title = "Commodity code is required for Product 3",
        Requirement = "Each line needs a commodity code, usually an 8-digit UNSPSC code, for Product 3.",
        Rationale = "The commodity code is one of the 12 Product 3 fields; it classifies what was bought.",
        PackId = "l3lint.rules.visa",
        PackVersion = "1.0.0",
        Brand = CardBrand.Visa,
        Level = RuleLevel.L3,
        Severity = Severity.Error,
        Confidence = Confidence.ProcessorDoc,
        Impact = Impact.BlocksLevel3,
        Check = new RuleCheck("required", ["/lineItems/*/commodityCode"], []),
        Message = "Line {index} has no commodity code; Visa needs one on every line for Product 3.",
        Fix = "Add the most specific 8-digit UNSPSC code for this item.",
        Sources = [Payroc],
    };

    public static RuleMetadata GenDq004 { get; } = new()
    {
        Id = "GEN-DQ-004",
        Title = "Filler customer code or PO",
        Requirement = "The customer code and purchase order number should be the buyer's real reference.",
        PackId = "l3lint.rules.general",
        PackVersion = "1.0.0",
        Level = RuleLevel.L2,
        Severity = Severity.Warning,
        Confidence = Confidence.Inferred,
        Impact = Impact.RiskVisaValidation,
        Check = new RuleCheck("custom", [], []),
        Message = "\"{actual}\" at {field} looks like filler, not the buyer's reference.",
        Fix = "Ask the buyer for their real reference.",
        Sources = [Payroc],
    };

    public static RuleMetadata GenDq007 { get; } = new()
    {
        Id = "GEN-DQ-007",
        Title = "Same description or product code across most of a batch",
        Requirement = "Across a batch, line data should vary as real invoices do.",
        PackId = "l3lint.rules.general",
        PackVersion = "1.0.0",
        Level = RuleLevel.L3,
        Severity = Severity.Warning,
        Confidence = Confidence.Inferred,
        Impact = Impact.RiskVisaValidation,
        Check = new RuleCheck("custom", [], []),
        Message = "\"{actual}\" appears at {field} on {count} of {transactions} transactions ({share}).",
        Fix = "Pull line data from the invoice, not a template.",
        Sources = [Payroc],
        IsBatch = true,
    };

    /// <summary>
    /// The small fixed report behind the snapshot tests. Two inputs: a Stripe file with one transaction (one error,
    /// one warning, a cost estimate) and an NDJSON file whose name has a space, holding a Mastercard transaction with
    /// no ID (one information finding from a rule with no metadata) and a clean Visa transaction. One batch warning
    /// points at the Mastercard transaction, and a third input could not be read.
    /// </summary>
    public static LintReport Small()
    {
        var error = new Finding
        {
            RuleId = "VISA-L3-004",
            Severity = Severity.Error,
            Impact = Impact.BlocksLevel3,
            Brand = CardBrand.Visa,
            TransactionIndex = 0,
            Path = new FieldPath("/lineItems/1/commodityCode", "/amount_details/line_items/1/payment_method_options/card/commodity_code"),
            Location = new SourceLocation(31, 9),
            Message = "Line 2 has no commodity code; Visa needs one on every line for Product 3.",
            Expected = "8-digit commodity code",
            Fix = "Add the most specific 8-digit UNSPSC code for this item.",
            Confidence = Confidence.ProcessorDoc,
            Sources = [Payroc],
        };
        var warning = new Finding
        {
            RuleId = "GEN-DQ-004",
            Severity = Severity.Warning,
            Impact = Impact.RiskVisaValidation,
            TransactionIndex = 0,
            Path = new FieldPath("/customer/reference", "/metadata/customer_reference"),
            Location = new SourceLocation(12, 27),
            Message = "\"111111\" at /customer/reference looks like filler, not the buyer's reference.",
            Actual = "111111",
            Fix = "Ask the buyer for their real reference.",
            Confidence = Confidence.Inferred,
            Sources = [Payroc],
        };
        var information = new Finding
        {
            RuleId = "GEN-DQ-001",
            Severity = Severity.Information,
            Impact = Impact.RiskVisaValidation,
            TransactionIndex = 1,
            Path = new FieldPath("/lineItems/0/description", "/lineItems/0/description"),
            Location = new SourceLocation(1, 120),
            Message = "Line 1 description \"A|B <C>\" is a placeholder; use *real* text.",
            Actual = "A|B <C>",
            Fix = "Use the real product or service name.",
            Confidence = Confidence.Inferred,
            Sources = [Versapay],
        };
        var batch = new Finding
        {
            RuleId = "GEN-DQ-007",
            Severity = Severity.Warning,
            Impact = Impact.RiskVisaValidation,
            TransactionIndex = 1,
            Path = new FieldPath("/lineItems/0/description", "/lineItems/0/description"),
            Location = new SourceLocation(1, 120),
            Message = "\"A|B <C>\" appears at /lineItems/0/description on 2 of 2 transactions (100%).",
            Actual = "A|B <C>",
            Fix = "Pull line data from the invoice, not a template.",
            Confidence = Confidence.Inferred,
            Sources = [Payroc],
        };

        var transactions = new List<TransactionResult>
        {
            new()
            {
                Index = 0,
                Input = 0,
                InputName = "invoices/INV-20417.stripe.json",
                Id = "INV-20417",
                Format = "stripe",
                Brand = CardBrand.Visa,
                Brands = [CardBrand.Visa],
                CardProduct = CardProduct.Purchasing,
                Mcc = "5047",
                AsOf = new DateOnly(2026, 10, 19),
                Findings = [warning, error],
                Tiers = [new TierAssessment(CardBrand.Visa, "product3", "belowProduct3", TierStatus.Blocked, 1)],
                CostEstimate = new CostEstimate("example-rates", new DateOnly(2026, 10, 1), "USD", [new CostEstimateLine(CardBrand.Visa, "level2", 6.10m, "level3", 5.12m, 0.98m)]),
            },
            new()
            {
                Index = 1,
                Input = 1,
                InputName = "invoices/batch 2.ndjson",
                Format = "canonical",
                Brand = CardBrand.Mastercard,
                Brands = [CardBrand.Mastercard],
                CardProduct = CardProduct.Corporate,
                AsOf = new DateOnly(2026, 10, 19),
                FirstLine = 1,
                Findings = [information],
                Tiers = [new TierAssessment(CardBrand.Mastercard, "dataRate3", "dataRate3", TierStatus.Meets, 0)],
            },
            new()
            {
                Index = 2,
                Input = 1,
                InputName = "invoices/batch 2.ndjson",
                Id = "INV-3",
                Format = "canonical",
                Brand = CardBrand.Visa,
                Brands = [CardBrand.Visa],
                CardProduct = CardProduct.Purchasing,
                AsOf = new DateOnly(2026, 10, 19),
                FirstLine = 2,
                Tiers = [new TierAssessment(CardBrand.Visa, "product3", "product3", TierStatus.Meets, 0)],
            },
        };

        List<Finding> all = [warning, error, information, batch];
        return new LintReport
        {
            ToolVersion = "1.0.0",
            RulePacks = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["l3lint.rules.general"] = "1.0.0",
                ["l3lint.rules.mastercard"] = "1.0.0",
                ["l3lint.rules.visa"] = "1.0.0",
            },
            Profile = "generic",
            Inputs =
            [
                new ReportInput("invoices/INV-20417.stripe.json", "stripe", new string('a', 64), 1),
                new ReportInput("invoices/batch 2.ndjson", "canonical", new string('b', 64), 2),
                new ReportInput("invoices/broken.json", "unknown", new string('c', 64), 0),
            ],
            Transactions = transactions,
            BatchFindings = [batch],
            InputErrors = [new InputError("invoices/broken.json", InputErrorKind.Malformed, "invoices/broken.json is not valid JSON: a value is missing after the comma.", 3)],
            Notes = ["Payload values may appear in logs because --log-values is set."],
            Summary = LintSummary.From(all, 3, Severity.Error),
            ExitCode = 2,
            ConfigHash = ConfigHash,
            Rules = [GenDq004, GenDq007, VisaL3004],
        };
    }

    /// <summary>A report with one transaction per finding, all in one input, for limit and size tests.</summary>
    public static LintReport Many(IReadOnlyList<Finding> findings, string inputPath = "invoices/many.ndjson")
    {
        var transactions = findings
            .Select((f, i) => new TransactionResult
            {
                Index = i,
                Input = 0,
                Id = "INV-" + i.ToString("D5", System.Globalization.CultureInfo.InvariantCulture),
                Brand = CardBrand.Visa,
                Brands = [CardBrand.Visa],
                AsOf = new DateOnly(2026, 10, 19),
                FirstLine = i + 1,
                Findings = [f with { TransactionIndex = i }],
            })
            .ToList();
        return new LintReport
        {
            ToolVersion = "1.0.0",
            RulePacks = new SortedDictionary<string, string>(StringComparer.Ordinal) { ["l3lint.rules.general"] = "1.0.0" },
            Inputs = [new ReportInput(inputPath, "canonical", new string('d', 64), transactions.Count)],
            Transactions = transactions,
            Summary = LintSummary.From(transactions.SelectMany(t => t.Findings), transactions.Count, Severity.Error),
            ExitCode = findings.Any(f => f.Severity == Severity.Error) ? 1 : 0,
            ConfigHash = ConfigHash,
            Rules = [GenDq004, VisaL3004],
        };
    }

    /// <summary>A finding with the given rule, severity and message on line <paramref name="line"/>.</summary>
    public static Finding Finding(string ruleId, Severity severity, string message, int line = 1, string? actual = null) => new()
    {
        RuleId = ruleId,
        Severity = severity,
        Impact = Impact.RiskVisaValidation,
        TransactionIndex = 0,
        Path = new FieldPath("/lineItems/0/description", "/lineItems/0/description"),
        Location = new SourceLocation(line, 5),
        Message = message,
        Actual = actual,
        Fix = "Use the real product or service name.",
        Confidence = Confidence.Inferred,
        Sources = [Versapay],
    };
}
