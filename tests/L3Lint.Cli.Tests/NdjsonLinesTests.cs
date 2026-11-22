using System.Security.Cryptography;
using System.Text;
using L3Lint.Hosting.Runs;

namespace L3Lint.Cli.Tests;

/// <summary>The streaming NDJSON reader: line endings, the last line, and the per-record limit (ADR-0048, NFR-003).</summary>
public sealed class NdjsonLinesTests
{
    [Fact]
    public async Task Lines_lose_their_line_endings_and_the_last_line_needs_no_newline()
    {
        var records = await ReadAsync("{\"a\":1}\r\n\n{\"b\":2}\n{\"c\":3}", maxRecordBytes: 100);

        records.Select(r => Encoding.UTF8.GetString(r.Bytes.Span)).ShouldBe(["{\"a\":1}", string.Empty, "{\"b\":2}", "{\"c\":3}"]);
        records.ShouldAllBe(r => !r.TooLarge);
    }

    [Fact]
    public async Task A_line_over_the_limit_is_counted_but_not_kept()
    {
        var huge = new string('x', 5000);
        var records = await ReadAsync("{\"a\":1}\r\n" + huge + "\r\n{\"b\":2}\n", maxRecordBytes: 2000);

        records.Count.ShouldBe(3);
        records[1].TooLarge.ShouldBeTrue();
        records[1].Length.ShouldBe(5000);
        records[1].Bytes.IsEmpty.ShouldBeTrue();
        Encoding.UTF8.GetString(records[2].Bytes.Span).ShouldBe("{\"b\":2}");
    }

    [Fact]
    public async Task A_line_exactly_at_the_limit_is_kept_with_or_without_a_carriage_return()
    {
        var exact = new string('y', 10);
        var records = await ReadAsync(exact + "\r\n" + exact + "\n" + exact + "z\n", maxRecordBytes: 10);

        records.Select(r => r.TooLarge).ShouldBe([false, false, true]);
        Encoding.UTF8.GetString(records[0].Bytes.Span).ShouldBe(exact);
        Encoding.UTF8.GetString(records[1].Bytes.Span).ShouldBe(exact);
    }

    [Fact]
    public async Task Every_byte_is_hashed_whatever_is_kept()
    {
        var text = "{\"a\":1}\n" + new string('x', 300) + "\n";
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var tooLarge = 0;

        await foreach (var record in NdjsonLines.ReadAsync(new OneByteAtATimeStream(Encoding.UTF8.GetBytes(text)), hash, 100, TestContext.Current.CancellationToken))
        {
            tooLarge += record.TooLarge ? 1 : 0;
        }

        tooLarge.ShouldBe(1);
        Convert.ToHexStringLower(hash.GetHashAndReset()).ShouldBe(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))));
    }

    private static async Task<List<NdjsonRecord>> ReadAsync(string text, long maxRecordBytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var records = new List<NdjsonRecord>();

        // One byte per read splits every line across reads, as a slow pipe or a 64 KB buffer boundary would.
        await foreach (var record in NdjsonLines.ReadAsync(new OneByteAtATimeStream(Encoding.UTF8.GetBytes(text)), hash, maxRecordBytes, TestContext.Current.CancellationToken))
        {
            records.Add(record with { Bytes = record.Bytes.ToArray() });
        }

        return records;
    }

    /// <summary>A stream that returns at most one byte per read.</summary>
    private sealed class OneByteAtATimeStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
    }
}
