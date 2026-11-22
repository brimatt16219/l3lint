using System.Diagnostics.CodeAnalysis;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core;

/// <summary>
/// The l3lint engine: lints commercial card payloads against the loaded rule packs.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="ILinter"/> is a thread-safe singleton (US-09). After startup it does no I/O: rule packs, lists,
/// profiles and the currency table load once. Payload problems never throw; they become findings. Only
/// programming errors (a <see langword="null"/> argument) and input errors on raw documents
/// (<see cref="InputException"/>) throw.
/// </para>
/// <para>Every surface (CLI, MCP server, GitHub Action) calls this same interface, so results match (G3).</para>
/// </remarks>
/// <example>
/// <code>
/// ILinter linter = serviceProvider.GetRequiredService&lt;ILinter&gt;();
/// var transaction = new Transaction
/// {
///     Amount = 280.36m,
///     Currency = "USD",
///     Card = new CardInfo { Brand = CardBrand.Visa, Product = CardProduct.Purchasing },
///     LineItems = [ new LineItem { Description = "Nitrile exam gloves L 100", Quantity = 10, UnitCost = 12.50m } ],
/// };
/// LintResult result = linter.Lint(transaction, new LintOptions { Mcc = "5047" });
/// if (result.Summary.Errors &gt; 0)
/// {
///     foreach (var finding in result.Findings) Console.WriteLine($"{finding.RuleId} {finding.Path.Canonical}: {finding.Message}");
/// }
/// </code>
/// </example>
public interface ILinter
{
    /// <summary>Gets the engine version.</summary>
    string EngineVersion { get; }

    /// <summary>Gets the loaded rule packs.</summary>
    IReadOnlyList<RulePackInfo> RulePacks { get; }

    /// <summary>Gets the adapters and format detection.</summary>
    IAdapterRegistry Adapters { get; }

    /// <summary>
    /// Lints one canonical transaction.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <param name="options">Options; <see langword="null"/> uses the defaults the linter was built with.</param>
    /// <returns>The result with one transaction.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Signature fixed by the spec's library API (Interfaces tab).")]
    LintResult Lint(Transaction transaction, LintOptions? options = null);

    /// <summary>
    /// Lints a raw document, detecting its format unless <see cref="LintOptions.Format"/> names one.
    /// </summary>
    /// <param name="document">The parsed input.</param>
    /// <param name="options">Options; <see langword="null"/> uses the defaults the linter was built with.</param>
    /// <returns>The result for every transaction in the document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatDetectionException">The format is unknown or ambiguous.</exception>
    /// <exception cref="InputException">The adapter cannot read the document at all, for example CSV without a column map.</exception>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Signature fixed by the spec's library API (Interfaces tab).")]
    LintResult Lint(RawDocument document, LintOptions? options = null);

    /// <summary>
    /// Maps a raw document to canonical transactions without linting (detect, scrub card numbers, map), for
    /// <c>convert</c> and <c>map_payload</c>.
    /// </summary>
    /// <param name="document">The parsed input.</param>
    /// <param name="options">Options; only <see cref="LintOptions.Format"/>, <see cref="LintOptions.Adapter"/>, <see cref="LintOptions.Limits"/> and <see cref="LintOptions.PciAllowFields"/> matter.</param>
    /// <returns>The canonical transactions with their path maps; card numbers are masked.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatDetectionException">The format is unknown or ambiguous.</exception>
    /// <exception cref="InputException">The adapter cannot read the document at all.</exception>
    MapResult Map(RawDocument document, LintOptions? options = null);

    /// <summary>
    /// Lints a stream of transactions, yielding each result as it is ready, then one batch result when batch
    /// rules fired (ADR-0034).
    /// </summary>
    /// <param name="transactions">The transactions.</param>
    /// <param name="options">Options; <see langword="null"/> uses the defaults.</param>
    /// <param name="cancellationToken">Stops the enumeration.</param>
    /// <returns>Results in input order.</returns>
    IAsyncEnumerable<TransactionResult> LintManyAsync(IAsyncEnumerable<Transaction> transactions, LintOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a session for callers that drive many documents themselves and want batch rules.
    /// </summary>
    /// <param name="options">Options; <see langword="null"/> uses the defaults.</param>
    /// <returns>A thread-safe session.</returns>
    ILintSession CreateSession(LintOptions? options = null);

    /// <summary>
    /// Gets a rule's metadata.
    /// </summary>
    /// <param name="ruleId">The rule ID, for example VISA-L3-004, or an engine diagnostic ID.</param>
    /// <returns>The metadata.</returns>
    /// <exception cref="KeyNotFoundException">No rule has that ID.</exception>
    RuleMetadata Explain(string ruleId);

    /// <summary>
    /// Finds a rule's metadata.
    /// </summary>
    /// <param name="ruleId">The rule ID (case-insensitive).</param>
    /// <param name="rule">The metadata when found.</param>
    /// <returns><see langword="true"/> when found.</returns>
    bool TryGetRule(string ruleId, [NotNullWhen(true)] out RuleMetadata? rule);

    /// <summary>
    /// Lists rules, ordered by ID.
    /// </summary>
    /// <param name="query">Filters; <see langword="null"/> lists every rule.</param>
    /// <returns>The matching rules.</returns>
    IReadOnlyList<RuleMetadata> Rules(RuleQuery? query = null);
}
