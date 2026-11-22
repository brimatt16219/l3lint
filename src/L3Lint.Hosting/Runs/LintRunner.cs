using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Channels;
using L3Lint.Core;
using L3Lint.Core.Input;
using L3Lint.Core.Rules;
using L3Lint.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace L3Lint.Hosting.Runs;

/// <summary>
/// Lints files, folders, globs, NDJSON streams and standard input in parallel and builds a <see cref="LintReport"/>
/// (pipeline stages 1 to 10, FR-001, US-04).
/// </summary>
/// <remarks>
/// <para>
/// A reader task streams documents (one NDJSON record at a time, so a 1 GB file never sits in memory) into a
/// bounded channel of capacity 256; <see cref="LintRunRequest.Parallelism"/> workers lint them; a collector
/// restores input order before anything is reported, so output is identical at any thread count (NFR-001).
/// CSV files are linted on the reader task because one file holds many transactions.
/// </para>
/// <para>Logs carry paths, counts and event IDs only, never payload values (NFR-005).</para>
/// </remarks>
/// <example>
/// <code>
/// var runner = new LintRunner(linter);
/// LintReport report = await runner.RunAsync(new LintRunRequest { Inputs = ["fixtures/**/*.json"] });
/// Console.WriteLine(report.Summary.Errors);
/// </code>
/// </example>
public sealed partial class LintRunner
{
    private const int ChannelCapacity = 256;
    private static readonly string[] NdjsonExtensions = [".ndjson", ".jsonl"];

    private readonly ILinter _linter;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="linter">The linter.</param>
    /// <param name="logger">A logger; payload values are never logged.</param>
    public LintRunner(ILinter linter, ILogger<LintRunner>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(linter);
        _linter = linter;
        _logger = (ILogger?)logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Runs the request.
    /// </summary>
    /// <param name="request">What to lint.</param>
    /// <param name="cancellationToken">Stops the run.</param>
    /// <returns>The report, with the exit code set.</returns>
    public async Task<LintReport> RunAsync(LintRunRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (sources, unmatched) = InputExpander.Expand(request.Inputs, request.WorkingDirectory);
        var session = _linter.CreateSession(request.Options);
        var state = new RunState(sources.Count, request.ResultBlockSize);
        foreach (var pattern in unmatched)
        {
            state.AddError(new InputError(pattern, InputErrorKind.Malformed, Format(Resources.Strings.InputNotFound, pattern)));
        }

        var work = Channel.CreateBounded<Item>(new BoundedChannelOptions(ChannelCapacity) { SingleWriter = true });
        var done = Channel.CreateUnbounded<Item>(new UnboundedChannelOptions { SingleReader = true });
        var reader = Task.Run(() => ReadAllAsync(sources, request, session, state, work.Writer, done.Writer, cancellationToken), cancellationToken);
        var workers = Enumerable.Range(0, Math.Max(1, request.Parallelism))
            .Select(_ => Task.Run(() => WorkAsync(session, work.Reader, done.Writer, cancellationToken), cancellationToken))
            .ToArray();
        var collector = Task.Run(() => CollectAsync(done.Reader, state, cancellationToken), cancellationToken);

        await reader.ConfigureAwait(false);
        await Task.WhenAll(workers).ConfigureAwait(false);
        done.Writer.Complete();
        await collector.ConfigureAwait(false);

        var batch = session.Complete(state.Transactions.Count);
        var packs = _linter.RulePacks.ToDictionary(p => p.Id, p => p.Version, StringComparer.Ordinal);
        var report = BuildReport(request, sources, state, batch, packs);
        return report with { Rules = FiredRules(report) };
    }

    /// <summary>Looks up the metadata of every rule that produced a finding, ordered by ID, for SARIF and Markdown.</summary>
    private List<RuleMetadata> FiredRules(LintReport report) =>
        [.. report.Summary.ByRule.Keys.Select(id => _linter.TryGetRule(id, out var rule) ? rule : null).OfType<RuleMetadata>()];

    private async Task ReadAllAsync(
        List<InputSource> sources,
        LintRunRequest request,
        ILintSession session,
        RunState state,
        ChannelWriter<Item> work,
        ChannelWriter<Item> done,
        CancellationToken cancellationToken)
    {
        try
        {
            for (var input = 0; input < sources.Count; input++)
            {
                await ReadSourceAsync(sources[input], input, request, session, state, work, done, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            work.Complete();
        }
    }

    private async Task ReadSourceAsync(
        InputSource source,
        int input,
        LintRunRequest request,
        ILintSession session,
        RunState state,
        ChannelWriter<Item> work,
        ChannelWriter<Item> done,
        CancellationToken cancellationToken)
    {
        var limits = request.Options.Limits;
        var csv = string.Equals(request.Options.Format, "csv", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(source.FullPath), ".csv", StringComparison.OrdinalIgnoreCase);
        var ndjson = !csv && NdjsonExtensions.Contains(Path.GetExtension(source.FullPath), StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!source.IsStandardInput && !csv && ndjson)
            {
                await ReadNdjsonAsync(source, input, limits, state, work, done, cancellationToken).ConfigureAwait(false);
                return;
            }

            byte[] bytes;
            if (source.IsStandardInput)
            {
                bytes = await InputFile.ReadStandardInputAsync(request.StandardInput, limits, source.DisplayPath, cancellationToken).ConfigureAwait(false);
                ndjson = !csv && InputFile.LooksLikeNdjson(bytes);
            }
            else
            {
                var length = new FileInfo(source.FullPath).Length;
                if (length > limits.MaxFileBytes)
                {
                    throw new InputException(InputErrorKind.TooLarge, Format(Core.Resources.Strings.InputTooLarge, length, limits.MaxFileBytes), source.DisplayPath);
                }

                bytes = await File.ReadAllBytesAsync(source.FullPath, cancellationToken).ConfigureAwait(false);
            }

            LogInputRead(_logger, source.DisplayPath, bytes.Length);
            state.SetHash(input, Convert.ToHexStringLower(SHA256.HashData(bytes)));
            if (ndjson)
            {
                await SplitNdjsonAsync(bytes, source, input, limits, state, work, done, cancellationToken).ConfigureAwait(false);
            }
            else if (csv)
            {
                var document = RawDocument.FromCsv(bytes, source.DisplayPath, limits);
                var first = state.Reserve(0);
                var result = session.Lint(document, first, input);
                state.Reserve(result.Transactions.Count);
                await done.WriteAsync(new Item(state.NextSequence(), input, first, null, result, null), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await EnqueueJsonAsync(bytes, source.DisplayPath, 1, input, limits, state, work, done, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (InputException ex)
        {
            LogInputError(_logger, source.DisplayPath, ex.Kind);
            await done.WriteAsync(new Item(state.NextSequence(), input, -1, null, null, InputError.From(ex, source.DisplayPath)), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            LogInputError(_logger, source.DisplayPath, InputErrorKind.Malformed);
            var error = new InputError(source.DisplayPath, InputErrorKind.Malformed, Format(Resources.Strings.InputUnreadable, source.DisplayPath, ex.Message));
            await done.WriteAsync(new Item(state.NextSequence(), input, -1, null, null, error), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReadNdjsonAsync(InputSource source, int input, InputLimits limits, RunState state, ChannelWriter<Item> work, ChannelWriter<Item> done, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var stream = File.OpenRead(source.FullPath);
        await using (stream.ConfigureAwait(false))
        {
            var line = 0;
            var total = 0L;
            await foreach (var record in NdjsonLines.ReadAsync(stream, hash, limits.MaxFileBytes, cancellationToken).ConfigureAwait(false))
            {
                line++;
                total += record.Length;
                if (record.TooLarge)
                {
                    // ADR-0048: an oversized record is an input error for its line; the rest of the file still lints.
                    var tooLarge = new InputException(InputErrorKind.TooLarge, Format(Core.Resources.Strings.InputTooLarge, record.Length, limits.MaxFileBytes), source.DisplayPath, line, 1);
                    await done.WriteAsync(new Item(state.NextSequence(), input, -1, null, null, InputError.From(tooLarge, source.DisplayPath)), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (record.Bytes.Length == 0 || record.Bytes.Span.Trim((byte)' ').IsEmpty)
                {
                    continue;
                }

                await EnqueueJsonAsync(record.Bytes, source.DisplayPath, line, input, limits, state, work, done, cancellationToken).ConfigureAwait(false);
            }

            LogInputRead(_logger, source.DisplayPath, total);
        }

        state.SetHash(input, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    private static async Task SplitNdjsonAsync(byte[] bytes, InputSource source, int input, InputLimits limits, RunState state, ChannelWriter<Item> work, ChannelWriter<Item> done, CancellationToken cancellationToken)
    {
        foreach (var (record, line) in InputFile.SplitLines(bytes))
        {
            await EnqueueJsonAsync(record, source.DisplayPath, line, input, limits, state, work, done, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task EnqueueJsonAsync(ReadOnlyMemory<byte> bytes, string name, int firstLine, int input, InputLimits limits, RunState state, ChannelWriter<Item> work, ChannelWriter<Item> done, CancellationToken cancellationToken)
    {
        RawDocument document;
        try
        {
            document = RawDocument.ParseJson(bytes, name, limits, firstLine);
        }
        catch (InputException ex)
        {
            await done.WriteAsync(new Item(state.NextSequence(), input, -1, null, null, InputError.From(ex, name)), cancellationToken).ConfigureAwait(false);
            return;
        }

        // A JSON document holds at most one transaction, so it reserves one run index.
        var index = state.Reserve(1);
        await work.WriteAsync(new Item(state.NextSequence(), input, index, document, null, null), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WorkAsync(ILintSession session, ChannelReader<Item> work, ChannelWriter<Item> done, CancellationToken cancellationToken)
    {
        await foreach (var item in work.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            Item outcome;
            try
            {
                outcome = item with { Document = null, Result = session.Lint(item.Document!, item.FirstIndex, item.Input) };
            }
            catch (InputException ex)
            {
                outcome = item with { Document = null, Error = InputError.From(ex, item.Document!.SourceName) };
            }

            await done.WriteAsync(outcome, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task CollectAsync(ChannelReader<Item> done, RunState state, CancellationToken cancellationToken)
    {
        var pending = new SortedDictionary<long, Item>();
        long next = 0;
        await foreach (var item in done.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            pending[item.Sequence] = item;
            while (pending.Remove(next, out var ready))
            {
                state.Accept(ready);
                next++;
            }
        }
    }

    private static LintReport BuildReport(LintRunRequest request, List<InputSource> sources, RunState state, TransactionResult? batch, Dictionary<string, string> packs)
    {
        var batchFindings = batch is null
            ? []
            : batch.Findings.Select(f => f with { TransactionIndex = state.FinalIndex(f.TransactionIndex) }).ToList();
        var transactions = state.Transactions;

        // One streaming pass: the findings are never collected into one list (NFR-003).
        var summary = LintSummary.From(transactions.SelectMany(t => t.Findings).Concat(batchFindings), transactions.Count, request.Options.FailOn);
        var total = summary.Errors + summary.Warnings + summary.Information;
        var omitted = 0;
        if (request.MaxFindings is { } max && total > max)
        {
            (transactions, batchFindings, omitted) = Truncate(transactions, batchFindings, max, request.ResultBlockSize);
        }

        var exitCode = state.InternalError ? 3 : state.Errors.Count > 0 ? 2 : summary.Failed ? 1 : 0;
        return new LintReport
        {
            ToolVersion = EngineInfo.Version,
            RulePacks = new SortedDictionary<string, string>(packs, StringComparer.Ordinal),
            AsOf = request.Options.AsOf,
            Profile = request.Profile,
            Inputs = [.. sources.Select((s, i) => new ReportInput(s.DisplayPath, state.FormatOf(i), state.HashOf(i), state.CountOf(i)))],
            Transactions = transactions,
            BatchFindings = batchFindings,
            InputErrors = state.Errors,
            Notes = request.Notes,
            Summary = summary,
            FindingsOmitted = omitted,
            FailOn = request.Options.FailOn,
            ExitCode = exitCode,
            ConfigHash = request.ConfigHash,
        };
    }

    private static (TransactionResultStore Transactions, List<Finding> Batch, int Omitted) Truncate(TransactionResultStore transactions, List<Finding> batch, int max, int blockSize)
    {
        var budget = max;
        var omitted = 0;
        var kept = new TransactionResultStore(blockSize);
        foreach (var transaction in transactions)
        {
            var take = Math.Min(budget, transaction.Findings.Count);
            omitted += transaction.Findings.Count - take;
            budget -= take;
            kept.Add(take == transaction.Findings.Count ? transaction : transaction with { Findings = [.. transaction.Findings.Take(take)] });
        }

        var keptBatch = batch.Take(budget).ToList();
        omitted += batch.Count - keptBatch.Count;
        return (kept, keptBatch, omitted);
    }

    private static string Format(string template, params object?[] args) => string.Format(CultureInfo.InvariantCulture, template, args);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Read input {Path} ({Bytes} bytes).")]
    private static partial void LogInputRead(ILogger logger, string path, long bytes);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Input {Path} could not be linted ({Kind}).")]
    private static partial void LogInputError(ILogger logger, string path, InputErrorKind kind);

    /// <summary>A unit of work or a finished result, tagged with its position in input order.</summary>
    private sealed record Item(long Sequence, int Input, int FirstIndex, RawDocument? Document, LintResult? Result, InputError? Error);

    /// <summary>Run-wide state touched by the reader (indexes, hashes) and the collector (results), never both at once for one field.</summary>
    private sealed class RunState(int inputs, int blockSize)
    {
        private readonly string?[] _hashes = new string?[inputs];
        private readonly string?[] _formats = new string?[inputs];
        private readonly int[] _counts = new int[inputs];
        private readonly Dictionary<int, int> _finalIndex = [];
        private long _sequence;
        private int _nextIndex;

        public TransactionResultStore Transactions { get; } = new(blockSize);

        public List<InputError> Errors { get; } = [];

        public bool InternalError { get; private set; }

        public long NextSequence() => Interlocked.Increment(ref _sequence) - 1;

        /// <summary>Reserves run indexes in input order and returns the first one.</summary>
        public int Reserve(int count)
        {
            var first = _nextIndex;
            _nextIndex += count;
            return first;
        }

        public void SetHash(int input, string hash) => _hashes[input] = hash;

        public void AddError(InputError error) => Errors.Add(error);

        public void Accept(Item item)
        {
            if (item.Error is { } error)
            {
                Errors.Add(error);
                return;
            }

            var result = item.Result!;
            InternalError |= result.HasInternalError;
            Errors.AddRange(result.InputErrors);
            _formats[item.Input] = _formats[item.Input] is null || _formats[item.Input] == result.Format ? result.Format : "mixed";
            foreach (var transaction in result.Transactions)
            {
                var final = Transactions.Count;
                _finalIndex[transaction.Index] = final;
                _counts[item.Input]++;
                Transactions.Add(transaction.Index == final && transaction.Transaction is null
                    ? transaction
                    : transaction with
                    {
                        Index = final,
                        Transaction = null,
                        Findings = transaction.Index == final ? transaction.Findings : [.. transaction.Findings.Select(f => f with { TransactionIndex = final })],
                    });
            }
        }

        public int FinalIndex(int provisional) => _finalIndex.TryGetValue(provisional, out var final) ? final : provisional;

        public string FormatOf(int input) => _formats[input] ?? "unknown";

        public string HashOf(int input) => _hashes[input] ?? string.Empty;

        public int CountOf(int input) => _counts[input];
    }
}
