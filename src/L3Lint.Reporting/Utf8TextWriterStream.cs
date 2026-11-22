using System.Text;

namespace L3Lint.Reporting;

/// <summary>
/// A write-only stream that decodes UTF-8 bytes onto a <see cref="TextWriter"/>, so a <c>Utf8JsonWriter</c> can
/// stream a report to any output without first building it as one string.
/// </summary>
/// <remarks>The decoder keeps a multi-byte character that is split across two writes until its last byte arrives.</remarks>
internal sealed class Utf8TextWriterStream(TextWriter output) : Stream
{
    private readonly Decoder _decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
    private char[] _chars = new char[4096];

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var needed = _decoder.GetCharCount(buffer, flush: false);
        if (needed > _chars.Length)
        {
            _chars = new char[Math.Max(needed, _chars.Length * 2)];
        }

        var written = _decoder.GetChars(buffer, _chars, flush: false);
        output.Write(_chars, 0, written);
    }

    /// <inheritdoc/>
    public override void Flush() => output.Flush();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
}
