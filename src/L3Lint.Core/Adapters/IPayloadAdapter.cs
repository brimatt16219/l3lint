using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Turns one gateway's request body into canonical transactions and remembers where each value came from.
/// </summary>
/// <remarks>
/// <para>
/// Adapters must be stateless and thread-safe; the linter calls them from many workers at once. They
/// convert minor-unit integers to major units with the currency exponent, fill line sequence numbers from
/// array order, list computed values in <see cref="AdapterResult.DerivedFields"/>, and record ambiguity as
/// <see cref="AdapterNote"/> values instead of guessing (ADR-0008). Adapters never throw for bad payload
/// content: values they cannot convert become <see cref="AdapterNoteKind.Invalid"/> notes.
/// </para>
/// <para>
/// The document an adapter receives has already been scrubbed: card numbers are masked before mapping
/// (ADR-0004). CSV adapters, which parse their own text, mask cells themselves and return the matches in
/// <see cref="AdapterResult.CardNumberMatches"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class MyGatewayAdapter : IPayloadAdapter
/// {
///     public string Name =&gt; "mygateway";
///     public InputKind Kind =&gt; InputKind.Json;
///     public double Detect(RawDocument document) =&gt;
///         document.Json.TryGetProperty("my_line_items", out _) ? 0.95 : 0.0;
///     public IReadOnlyList&lt;AdapterResult&gt; Map(RawDocument document, AdapterOptions options) =&gt; ...;
///     public string? ToCanonicalPointer(string sourcePointer) =&gt; ...;
/// }
/// </code>
/// </example>
public interface IPayloadAdapter
{
    /// <summary>Gets the format name used with <c>--format</c>, for example "stripe".</summary>
    string Name { get; }

    /// <summary>Gets the document shape this adapter reads.</summary>
    InputKind Kind { get; }

    /// <summary>
    /// Scores how likely the document is in this adapter's format, using cheap shape probes.
    /// </summary>
    /// <param name="document">The parsed input.</param>
    /// <returns>A score from 0.0 (not this format) to 1.0 (certainly this format).</returns>
    double Detect(RawDocument document);

    /// <summary>
    /// Maps the document into canonical transactions.
    /// </summary>
    /// <param name="document">The parsed, scrubbed input.</param>
    /// <param name="options">Adapter options such as a default currency or a CSV column map.</param>
    /// <returns>One result per transaction; JSON formats return exactly one.</returns>
    /// <exception cref="InputException">The adapter cannot run at all, for example a CSV input without a column map.</exception>
    IReadOnlyList<AdapterResult> Map(RawDocument document, AdapterOptions options);

    /// <summary>
    /// Translates a pointer in this format to the canonical pointer it maps to.
    /// </summary>
    /// <param name="sourcePointer">A JSON Pointer into a document of this format.</param>
    /// <returns>The canonical pointer, or <see langword="null"/> when the source value does not map to a canonical field.</returns>
    /// <remarks>The card-number scrub uses this to tell code fields from free text and to honor <c>pci.allowFields</c>.</remarks>
    string? ToCanonicalPointer(string sourcePointer);
}
