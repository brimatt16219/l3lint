namespace L3Lint.Core.Rules;

/// <summary>
/// A rule the engine evaluates against each transaction.
/// </summary>
/// <remarks>
/// <para>
/// Rules are stateless singletons and are called from many threads at once (US-09). Most rules come from
/// rule pack YAML; library users can add their own with <c>services.AddL3Lint(o =&gt; o.AddRule&lt;T&gt;())</c>.
/// A rule that throws is isolated: the engine records an internal error finding and runs the others.
/// </para>
/// <para>Rules report problems as findings through the sink and never throw for bad payload data.</para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class PoPrefixRule : IRule
/// {
///     public RuleMetadata Metadata { get; } = new()
///     {
///         Id = "GEN-ACME-001", Title = "PO numbers start with PO-", PackId = "acme", PackVersion = "1.0.0",
///         Level = RuleLevel.L2, Severity = Severity.Warning, Confidence = Confidence.Policy,
///         Impact = Impact.Hygiene, Message = "PO {actual} does not start with PO-.",
///         Fix = "Send the PO number as printed on the buyer's order.",
///         Check = new RuleCheck("custom", ["/order/purchaseOrderNumber"], []),
///         Sources = [new Uri("https://example.com/acme-po-policy")],
///     };
///
///     public bool AppliesTo(in RuleContext context) =&gt; context.Transaction.Order.PurchaseOrderNumber is not null;
///
///     public void Evaluate(in RuleContext context, IFindingSink sink)
///     {
///         var po = context.Transaction.Order.PurchaseOrderNumber!;
///         if (!po.StartsWith("PO-", StringComparison.Ordinal))
///         {
///             sink.Report(new FindingDraft(Metadata, "/order/purchaseOrderNumber") { Actual = po });
///         }
///     }
/// }
/// </code>
/// </example>
public interface IRule
{
    /// <summary>Gets the rule's metadata.</summary>
    RuleMetadata Metadata { get; }

    /// <summary>
    /// Decides whether the rule applies to this transaction beyond brand, date and selection, which the engine checks first.
    /// </summary>
    /// <param name="context">The transaction and its context.</param>
    /// <returns><see langword="true"/> to evaluate the rule.</returns>
    bool AppliesTo(in RuleContext context);

    /// <summary>
    /// Evaluates the rule and reports each problem.
    /// </summary>
    /// <param name="context">The transaction and its context.</param>
    /// <param name="sink">Receives findings.</param>
    void Evaluate(in RuleContext context, IFindingSink sink);
}
