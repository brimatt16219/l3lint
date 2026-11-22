using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace L3Lint.Hosting.Runs;

/// <summary>
/// Streams the lines of an NDJSON file without loading the file, hashing every byte as it goes (NFR-003). A
/// trailing carriage return is removed from each line. A line longer than the record limit is not collected: its
/// bytes are counted and dropped, and it comes back marked as too large (ADR-0048), so one huge line cannot fill memory.
/// </summary>
internal static class NdjsonLines
{
    public static async IAsyncEnumerable<NdjsonRecord> ReadAsync(Stream stream, IncrementalHash hash, long maxRecordBytes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        var line = new LineState(maxRecordBytes);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                var start = 0;
                for (var i = 0; i < read; i++)
                {
                    if (buffer[i] != (byte)'\n')
                    {
                        continue;
                    }

                    line.Append(buffer.AsSpan(start, i - start));
                    yield return line.Finish();
                    line = new LineState(maxRecordBytes);
                    start = i + 1;
                }

                line.Append(buffer.AsSpan(start, read - start));
            }

            if (line.Length > 0)
            {
                yield return line.Finish();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>The line being read: its bytes while within the limit, and always its length and last byte.</summary>
    private sealed class LineState(long maxRecordBytes)
    {
        private readonly ArrayBufferWriter<byte> _bytes = new();
        private byte _last;

        public long Length { get; private set; }

        public void Append(ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                return;
            }

            Length += bytes.Length;
            _last = bytes[^1];

            // One byte of slack for the carriage return that Finish removes.
            if (Length <= maxRecordBytes + 1)
            {
                _bytes.Write(bytes);
            }
        }

        public NdjsonRecord Finish()
        {
            var length = Length > 0 && _last == (byte)'\r' ? Length - 1 : Length;
            return length > maxRecordBytes
                ? new NdjsonRecord(ReadOnlyMemory<byte>.Empty, length, TooLarge: true)
                : new NdjsonRecord(_bytes.WrittenMemory[..(int)length], length, TooLarge: false);
        }
    }
}

/// <summary>One NDJSON line.</summary>
/// <param name="Bytes">The line without its line ending; empty when <paramref name="TooLarge"/> is set.</param>
/// <param name="Length">The line's length in bytes, without its line ending.</param>
/// <param name="TooLarge">Whether the line is longer than the record limit and was not kept.</param>
internal readonly record struct NdjsonRecord(ReadOnlyMemory<byte> Bytes, long Length, bool TooLarge);
