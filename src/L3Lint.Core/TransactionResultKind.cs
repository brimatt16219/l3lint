namespace L3Lint.Core;

/// <summary>
/// Distinguishes per-transaction results from the trailing result that carries batch findings.
/// </summary>
public enum TransactionResultKind
{
    /// <summary>One transaction.</summary>
    Transaction,

    /// <summary>The batch findings of a run (GEN-DQ-007); each finding points at the transaction that carries the value.</summary>
    Batch,
}
