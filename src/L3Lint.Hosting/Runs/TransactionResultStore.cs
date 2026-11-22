using System.Collections;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using L3Lint.Core;

namespace L3Lint.Hosting.Runs;

/// <summary>
/// Holds a run's transaction results in order without keeping them all as objects (NFR-003: bounded memory for a
/// 1 GB NDJSON file).
/// </summary>
/// <remarks>
/// Results are added in final order. Each full block is serialized to JSON and compressed; only the last, partial
/// block stays as objects, so a run smaller than one block never serializes anything. Reading decompresses one
/// block at a time. The round trip keeps every value the writers read, so reports are byte-identical either way
/// (tests/L3Lint.Corpus.Tests, ADR-0059). Writing happens on one thread before any reading starts.
/// </remarks>
internal sealed class TransactionResultStore : IReadOnlyList<TransactionResult>
{
    /// <summary>The number of results per compressed block.</summary>
    public const int DefaultBlockSize = 1024;

    private readonly int _blockSize;
    private readonly List<byte[]> _blocks = [];
    private readonly Lock _gate = new();
    private List<TransactionResult> _open;
    private int _cachedBlock = -1;
    private TransactionResult[] _cached = [];

    /// <summary>Initializes a new instance.</summary>
    /// <param name="blockSize">Results per compressed block; tests use small blocks to exercise the round trip.</param>
    public TransactionResultStore(int blockSize = DefaultBlockSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(blockSize, 1);
        _blockSize = blockSize;
        _open = new List<TransactionResult>(Math.Min(blockSize, DefaultBlockSize));
    }

    /// <inheritdoc/>
    public int Count { get; private set; }

    /// <inheritdoc/>
    public TransactionResult this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            var block = index / _blockSize;
            if (block == _blocks.Count)
            {
                return _open[index % _blockSize];
            }

            lock (_gate)
            {
                if (_cachedBlock != block)
                {
                    _cached = Read(_blocks[block]);
                    _cachedBlock = block;
                }

                return _cached[index % _blockSize];
            }
        }
    }

    /// <summary>Appends a result after every result added so far.</summary>
    /// <param name="result">The result, with its final index.</param>
    public void Add(TransactionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _open.Add(result);
        Count++;
        if (_open.Count == _blockSize)
        {
            Seal();
        }
    }

    /// <inheritdoc/>
    public IEnumerator<TransactionResult> GetEnumerator()
    {
        foreach (var block in _blocks)
        {
            foreach (var result in Read(block))
            {
                yield return result;
            }
        }

        foreach (var result in _open)
        {
            yield return result;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Seal()
    {
        using var buffer = new MemoryStream();
        using (var compressed = new ZLibStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        {
            JsonSerializer.Serialize(compressed, _open, TransactionResultStoreJson.Default.ListTransactionResult);
        }

        _blocks.Add(buffer.ToArray());
        _open = new List<TransactionResult>(_blockSize);
    }

    private static TransactionResult[] Read(byte[] block)
    {
        using var compressed = new ZLibStream(new MemoryStream(block, writable: false), CompressionMode.Decompress);
        return JsonSerializer.Deserialize(compressed, TransactionResultStoreJson.Default.TransactionResultArray) ?? [];
    }
}

/// <summary>Source-generated serialization for <see cref="TransactionResultStore"/> blocks.</summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(List<TransactionResult>))]
[JsonSerializable(typeof(TransactionResult[]))]
internal sealed partial class TransactionResultStoreJson : JsonSerializerContext;
