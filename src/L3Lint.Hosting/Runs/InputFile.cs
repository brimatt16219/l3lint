using System.Globalization;
using System.Text;
using L3Lint.Core;
using L3Lint.Core.Input;

namespace L3Lint.Hosting.Runs;

/// <summary>
/// One input read whole and split into documents, for commands that work on a single input (<c>fix</c>,
/// <c>convert</c>): a JSON file is one document, an NDJSON file one document per non-blank line, and a CSV file
/// one document holding every row.
/// </summary>
/// <param name="DisplayName">The path as the user typed it, or "-" for standard input.</param>
/// <param name="Kind">How the input was split.</param>
/// <param name="Documents">The parsed documents, in input order.</param>
public sealed record InputFile(string DisplayName, InputFileKind Kind, IReadOnlyList<RawDocument> Documents)
{
    private static readonly string[] NdjsonExtensions = [".ndjson", ".jsonl"];

    /// <summary>
    /// Reads and parses one input.
    /// </summary>
    /// <param name="input">A file path, or "-" for standard input.</param>
    /// <param name="workingDirectory">The folder relative paths resolve from.</param>
    /// <param name="standardInput">Opens standard input, for "-".</param>
    /// <param name="options">The lint options; <see cref="LintOptions.Format"/> and <see cref="LintOptions.Limits"/> apply.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>The documents.</returns>
    /// <exception cref="InputException">The input is missing, too large or not valid JSON or CSV.</exception>
    public static async Task<InputFile> ReadAsync(string input, string workingDirectory, Func<Stream>? standardInput, LintOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        ArgumentNullException.ThrowIfNull(options);
        var limits = options.Limits;
        var csv = string.Equals(options.Format, "csv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(input), ".csv", StringComparison.OrdinalIgnoreCase);
        bool ndjson;
        byte[] bytes;
        if (input == "-")
        {
            bytes = await ReadStandardInputAsync(standardInput, limits, input, cancellationToken).ConfigureAwait(false);
            ndjson = !csv && LooksLikeNdjson(bytes);
        }
        else
        {
            var full = Path.GetFullPath(Path.Combine(workingDirectory, input));
            if (!File.Exists(full))
            {
                throw new InputException(InputErrorKind.Malformed, Format(Resources.Strings.InputNotFound, input), input);
            }

            ndjson = !csv && NdjsonExtensions.Contains(Path.GetExtension(full), StringComparer.OrdinalIgnoreCase);
            var length = new FileInfo(full).Length;
            if (!ndjson && length > limits.MaxFileBytes)
            {
                throw new InputException(InputErrorKind.TooLarge, Format(Core.Resources.Strings.InputTooLarge, length, limits.MaxFileBytes), input);
            }

            bytes = await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false);
        }

        if (csv)
        {
            return new InputFile(input, InputFileKind.Csv, [RawDocument.FromCsv(bytes, input, limits)]);
        }

        if (!ndjson)
        {
            return new InputFile(input, InputFileKind.Json, [RawDocument.ParseJson(bytes, input, limits)]);
        }

        var documents = new List<RawDocument>();
        foreach (var (record, line) in SplitLines(bytes))
        {
            // ParseJson applies the size limit to each record and names the record's line (ADR-0048).
            documents.Add(RawDocument.ParseJson(record, input, limits, line));
        }

        return new InputFile(input, InputFileKind.Ndjson, documents);
    }

    /// <summary>Reads standard input up to the file size limit.</summary>
    internal static async Task<byte[]> ReadStandardInputAsync(Func<Stream>? standardInput, InputLimits limits, string displayName, CancellationToken cancellationToken)
    {
        if (standardInput is null)
        {
            throw new InputException(InputErrorKind.Unsupported, Resources.Strings.InputNoStandardInput, displayName);
        }

        var stream = standardInput();
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > limits.MaxFileBytes)
                {
                    throw new InputException(InputErrorKind.TooLarge, Format(Core.Resources.Strings.InputTooLarge, buffer.Length + read, limits.MaxFileBytes), displayName);
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
    }

    /// <summary>Standard input is NDJSON when it has two or more non-blank lines that each hold one object.</summary>
    internal static bool LooksLikeNdjson(byte[] bytes)
    {
        var lines = Encoding.UTF8.GetString(bytes).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        return lines.Count > 1 && lines.All(l => l.StartsWith('{') && l.EndsWith('}'));
    }

    /// <summary>Splits on LF, removes a trailing CR, skips blank lines and reports 1-based line numbers.</summary>
    internal static IEnumerable<(ReadOnlyMemory<byte> Record, int Line)> SplitLines(byte[] bytes)
    {
        var line = 0;
        var start = 0;
        for (var i = 0; i <= bytes.Length; i++)
        {
            if (i < bytes.Length && bytes[i] != (byte)'\n')
            {
                continue;
            }

            line++;
            var end = i > start && bytes[i - 1] == (byte)'\r' ? i - 1 : i;
            var record = new ReadOnlyMemory<byte>(bytes, start, end - start);
            start = i + 1;
            if (!record.Span.Trim((byte)' ').IsEmpty)
            {
                yield return (record, line);
            }
        }
    }

    private static string Format(string template, params object?[] args) => string.Format(CultureInfo.InvariantCulture, template, args);
}
