namespace L3Lint.Core.Rules;

/// <summary>
/// A rule decided over a whole run rather than one transaction, such as GEN-DQ-007.
/// </summary>
/// <remarks>
/// A fresh instance serves each run. The engine calls <see cref="Observe"/> once per transaction, from one
/// thread at a time, then <see cref="Complete"/> once. Implementations keep bounded aggregate counts, not
/// transactions (NFR-003), and must give the same answer for any observation order (NFR-001).
/// </remarks>
/// <example>
/// <code>
/// IBatchRule rule = batchCheck.Create(metadata, options);
/// foreach (var context in contexts) rule.Observe(context);
/// rule.Complete(sink);
/// </code>
/// </example>
public interface IBatchRule
{
    /// <summary>Gets the rule's metadata.</summary>
    RuleMetadata Metadata { get; }

    /// <summary>Records what the rule needs from one transaction.</summary>
    /// <param name="context">The transaction and its context.</param>
    void Observe(in RuleContext context);

    /// <summary>Reports findings once the run has ended.</summary>
    /// <param name="sink">Receives findings; each finding points at the first transaction that carries the value (ADR-0034).</param>
    void Complete(IFindingSink sink);
}
