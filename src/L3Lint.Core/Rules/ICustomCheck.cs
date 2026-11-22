namespace L3Lint.Core.Rules;

/// <summary>
/// A compiled check that a rule pack names with <c>check: { kind: custom, check: Name }</c>.
/// </summary>
/// <remarks>
/// Packs are data only; a custom check resolves only to a class compiled into the tool or registered by the
/// host application, so loading a pack never runs code (NFR-006). Implementations are stateless and
/// thread-safe.
/// </remarks>
/// <example>
/// <code>
/// public sealed class ExplicitTaxCheck : ICustomCheck
/// {
///     public string Name =&gt; "ExplicitTax";
///
///     public void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink)
///     {
///         var tax = context.Transaction.Tax;
///         if (tax.Indicator != TaxIndicator.Exempt &amp;&amp; (tax.Amount ?? 0m) &lt;= 0m)
///         {
///             sink.Report(new FindingDraft(rule, "/tax/amount") { Actual = tax.Amount?.ToString(CultureInfo.InvariantCulture) });
///         }
///     }
/// }
/// </code>
/// </example>
public interface ICustomCheck
{
    /// <summary>Gets the name a pack uses to refer to this check.</summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the check for one rule and transaction.
    /// </summary>
    /// <param name="context">The transaction and its context.</param>
    /// <param name="rule">The metadata of the rule that names this check.</param>
    /// <param name="sink">Receives findings.</param>
    void Evaluate(in RuleContext context, RuleMetadata rule, IFindingSink sink);
}
