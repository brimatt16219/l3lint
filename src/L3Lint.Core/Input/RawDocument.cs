using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace L3Lint.Core.Input;

/// <summary>
/// One input document as received: the original bytes plus, for JSON, the parsed tree.
/// </summary>
/// <remarks>
/// <para>
/// A document is immutable and safe to share across threads. Parsing happens once, when the document is
/// created, so malformed input fails early with an <see cref="InputException"/>. The linter never changes
/// a document; when it must mask a card number it works on a masked copy (ADR-0004).
/// </para>
/// <para>
/// The original bytes are kept only to compute the input SHA-256 and the line and column of each value for
/// SARIF (ADR-0021). They are never written to any output.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var document = RawDocument.ParseJson(File.ReadAllBytes("payload.json"), "payload.json");
/// LintResult result = linter.Lint(document);
/// </code>
/// </example>
public sealed class RawDocument
{
    private readonly ReadOnlyMemory<byte> _content;
    private readonly bool _masked;
    private readonly Lazy<string> _sha256;
    private readonly Lazy<PositionIndex> _positions;

    private RawDocument(string sourceName, InputKind kind, ReadOnlyMemory<byte> content, JsonElement json, int firstLine, bool masked = false)
    {
        SourceName = sourceName;
        Kind = kind;
        _content = content;
        _masked = masked;
        Json = json;
        FirstLine = firstLine;
        _sha256 = new Lazy<string>(() => Convert.ToHexStringLower(SHA256.HashData(_content.Span)), LazyThreadSafetyMode.PublicationOnly);
        _positions = new Lazy<PositionIndex>(() => PositionIndex.Build(_content, firstLine), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the file name, "-" for stdin, or another label for the input.</summary>
    public string SourceName { get; }

    /// <summary>Gets the shape of the document.</summary>
    public InputKind Kind { get; }

    /// <summary>Gets the 1-based line in the source file where this document starts (above 1 for NDJSON records).</summary>
    public int FirstLine { get; }

    /// <summary>Gets the parsed JSON object. Undefined for CSV documents.</summary>
    public JsonElement Json { get; }

    /// <summary>Gets the length of the original input in bytes.</summary>
    public int Length => _content.Length;

    /// <summary>Gets the lowercase hex SHA-256 of the original bytes.</summary>
    public string Sha256 => _sha256.Value;

    /// <summary>Gets a value indicating whether card numbers in <see cref="Json"/> were masked (FR-033).</summary>
    public bool IsMasked => _masked;

    /// <summary>
    /// Gets the text, for adapters that read text formats such as CSV. For a masked document this is the masked
    /// JSON, never the original bytes, so a card number cannot leak through it (NFR-007).
    /// </summary>
    /// <returns>The input decoded as UTF-8 without a byte order mark, or the masked JSON.</returns>
    public string GetText() => _masked ? Json.GetRawText() : Encoding.UTF8.GetString(SkipBom(_content.Span));

    internal ReadOnlyMemory<byte> Content => _content;

    /// <summary>
    /// Parses one JSON object.
    /// </summary>
    /// <param name="utf8">The UTF-8 bytes. A byte order mark is allowed.</param>
    /// <param name="sourceName">The file name or a label used in messages and outputs.</param>
    /// <param name="limits">Input limits; <see cref="InputLimits.Default"/> when <see langword="null"/>.</param>
    /// <param name="firstLine">1-based line where this document starts in its file (NDJSON records start later).</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="InputException">The bytes are too large, not well-formed, too deep, repeat a property, or are not an object.</exception>
    public static RawDocument ParseJson(ReadOnlyMemory<byte> utf8, string sourceName, InputLimits? limits = null, int firstLine = 1)
    {
        ArgumentNullException.ThrowIfNull(sourceName);
        ArgumentOutOfRangeException.ThrowIfLessThan(firstLine, 1);
        limits ??= InputLimits.Default;
        if (utf8.Length > limits.MaxFileBytes)
        {
            // Reported at the line where the document starts, so an oversized NDJSON record names its line (ADR-0048).
            throw new InputException(InputErrorKind.TooLarge, Format(Resources.Strings.InputTooLarge, utf8.Length, limits.MaxFileBytes), sourceName, firstLine, 1);
        }

        var body = utf8.Span.StartsWith(Utf8Bom) ? utf8[Utf8Bom.Length..] : utf8;

        // The JSON reader checks UTF-8 only when a string is decoded, so broken bytes inside a string would
        // otherwise fail later, in whichever stage reads that value first.
        if (!System.Text.Unicode.Utf8.IsValid(body.Span))
        {
            var (line, column) = FirstInvalidUtf8(body.Span);
            throw new InputException(InputErrorKind.Malformed, Resources.Strings.InputInvalidUtf8, sourceName, line + firstLine - 1, column);
        }

        var options = new JsonDocumentOptions
        {
            MaxDepth = limits.MaxJsonDepth,
            AllowDuplicateProperties = false,
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
        };

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(body, options);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            var line = (int)(ex.LineNumber ?? 0) + firstLine;
            var column = (int)(ex.BytePositionInLine ?? 0) + 1;
            var kind = ex.Message.Contains("depth", StringComparison.OrdinalIgnoreCase) ? InputErrorKind.TooDeep
                : ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ? InputErrorKind.DuplicateProperty
                : InputErrorKind.Malformed;
            var message = kind switch
            {
                InputErrorKind.TooDeep => Format(Resources.Strings.InputTooDeep, limits.MaxJsonDepth),
                InputErrorKind.DuplicateProperty => Resources.Strings.InputDuplicateProperty,
                _ => Resources.Strings.InputMalformedJson,
            };
            throw new InputException(kind, message, sourceName, line, column, ex);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InputException(InputErrorKind.NotAnObject, Resources.Strings.InputNotAnObject, sourceName, firstLine, 1);
        }

        return new RawDocument(sourceName, InputKind.Json, utf8, root, firstLine);
    }

    /// <summary>
    /// Parses one JSON object from text.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="sourceName">The file name or a label used in messages and outputs.</param>
    /// <returns>The parsed document, checked against <see cref="InputLimits.Default"/>.</returns>
    /// <exception cref="InputException">The text is too large, not well-formed, too deep, repeats a property, or is not an object.</exception>
    public static RawDocument ParseJson(string json, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseJson(Encoding.UTF8.GetBytes(json), sourceName);
    }

    /// <summary>
    /// Wraps CSV text. Parsing happens in the CSV adapter, which needs the column map.
    /// </summary>
    /// <param name="utf8">The UTF-8 bytes of the CSV file.</param>
    /// <param name="sourceName">The file name or a label used in messages and outputs.</param>
    /// <param name="limits">Input limits; <see cref="InputLimits.Default"/> when <see langword="null"/>.</param>
    /// <returns>The document.</returns>
    /// <exception cref="InputException">The bytes are larger than the file size limit.</exception>
    public static RawDocument FromCsv(ReadOnlyMemory<byte> utf8, string sourceName, InputLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(sourceName);
        limits ??= InputLimits.Default;
        if (utf8.Length > limits.MaxFileBytes)
        {
            throw new InputException(InputErrorKind.TooLarge, Format(Resources.Strings.InputTooLarge, utf8.Length, limits.MaxFileBytes), sourceName);
        }

        return new RawDocument(sourceName, InputKind.Csv, utf8, default, 1);
    }

    /// <summary>
    /// Finds the line and column of the value at a JSON Pointer in the original bytes.
    /// </summary>
    /// <param name="pointer">An RFC 6901 pointer into this document.</param>
    /// <returns>The position of the value, of its nearest existing ancestor when the value is absent, or <see langword="null"/> for CSV documents.</returns>
    /// <remarks>The position index is built on first use and then cached.</remarks>
    public SourceLocation? Locate(string? pointer)
    {
        if (Kind != InputKind.Json || pointer is null)
        {
            return null;
        }

        return _positions.Value.Locate(pointer);
    }

    /// <summary>Creates a document that holds a masked tree but keeps the original bytes for positions and hashing.</summary>
    internal RawDocument WithMaskedJson(JsonElement maskedJson) => new(SourceName, Kind, _content, maskedJson, FirstLine, masked: true);

    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private static ReadOnlySpan<byte> SkipBom(ReadOnlySpan<byte> bytes) => bytes.StartsWith(Utf8Bom) ? bytes[Utf8Bom.Length..] : bytes;

    /// <summary>The 1-based line and byte column of the first byte that does not start a valid UTF-8 sequence.</summary>
    private static (int Line, int Column) FirstInvalidUtf8(ReadOnlySpan<byte> bytes)
    {
        var line = 1;
        var lineStart = 0;
        var index = 0;
        while (index < bytes.Length && Rune.DecodeFromUtf8(bytes[index..], out var rune, out var consumed) == System.Buffers.OperationStatus.Done)
        {
            if (rune.Value == '\n')
            {
                line++;
                lineStart = index + 1;
            }

            index += consumed;
        }

        return (line, index - lineStart + 1);
    }

    private static string Format(string template, params object[] args) => string.Format(CultureInfo.InvariantCulture, template, args);
}
