using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core;

/// <summary>
/// A run over many documents that shares one set of options and the batch rules (GEN-DQ-007).
/// </summary>
/// <remarks>
/// Methods are thread-safe. Callers assign transaction indexes so results stay deterministic at any thread
/// count (NFR-001); call <see cref="Complete"/> once, after every document.
/// </remarks>
public interface ILintSession
{
    /// <summary>Gets the options of the run.</summary>
    LintOptions Options { get; }

    /// <summary>
    /// Lints one document.
    /// </summary>
    /// <param name="document">The parsed input.</param>
    /// <param name="firstIndex">The run index of the document's first transaction.</param>
    /// <param name="input">The 0-based input ordinal, for output.</param>
    /// <returns>The document's result.</returns>
    /// <exception cref="InputException">The format is unknown or ambiguous, or the adapter cannot read the document.</exception>
    LintResult Lint(RawDocument document, int firstIndex, int input);

    /// <summary>
    /// Lints one transaction.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <param name="index">The run index of the transaction.</param>
    /// <returns>The transaction's result.</returns>
    TransactionResult Lint(Transaction transaction, int index);

    /// <summary>
    /// Ends the run and returns the batch findings.
    /// </summary>
    /// <param name="index">The index to give the batch result, usually the transaction count.</param>
    /// <returns>The batch result, or <see langword="null"/> when no batch rule fired.</returns>
    TransactionResult? Complete(int index);
}
