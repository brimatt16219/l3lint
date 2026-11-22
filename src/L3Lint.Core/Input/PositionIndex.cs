using System.Text;
using System.Text.Json;

namespace L3Lint.Core.Input;

/// <summary>
/// Maps JSON Pointers to the line and column of their value token in the original bytes.
/// </summary>
/// <remarks>
/// Built in one forward pass of <see cref="Utf8JsonReader"/>, only for documents that produce findings
/// (ADR-0021). Columns count UTF-16 code units from 1, matching SARIF's default column kind.
/// </remarks>
internal sealed class PositionIndex
{
    private readonly Dictionary<string, SourceLocation> _positions;

    private PositionIndex(Dictionary<string, SourceLocation> positions) => _positions = positions;

    public static PositionIndex Build(ReadOnlyMemory<byte> content, int firstLine)
    {
        var bytes = content.Span;
        var bomLength = bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? 3 : 0;
        var lineStarts = LineStarts(bytes);
        var positions = new Dictionary<string, SourceLocation>(StringComparer.Ordinal);
        var reader = new Utf8JsonReader(bytes[bomLength..], new JsonReaderOptions { MaxDepth = 1000 });
        var frames = new Stack<Frame>();
        string? pendingProperty = null;
        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        pendingProperty = reader.GetString();
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        frames.Pop();
                        break;
                    default:
                        var pointer = NextPointer(frames, pendingProperty);
                        pendingProperty = null;
                        positions.TryAdd(pointer, ToLocation(bytes, lineStarts, (int)reader.TokenStartIndex + bomLength, firstLine));
                        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                        {
                            frames.Push(new Frame(pointer, reader.TokenType == JsonTokenType.StartArray));
                        }

                        break;
                }
            }
        }
        catch (JsonException)
        {
            // The document parsed successfully before, so this only happens for content that is not JSON
            // (for example CSV); the positions found so far are still valid.
            return new PositionIndex(positions);
        }

        return new PositionIndex(positions);
    }

    public SourceLocation? Locate(string pointer)
    {
        var current = pointer;
        while (true)
        {
            if (_positions.TryGetValue(current, out var location))
            {
                return location;
            }

            if (current.Length == 0)
            {
                return null;
            }

            var slash = current.LastIndexOf('/');
            current = slash <= 0 ? string.Empty : current[..slash];
        }
    }

    private static string NextPointer(Stack<Frame> frames, string? property)
    {
        if (frames.Count == 0)
        {
            return string.Empty;
        }

        var parent = frames.Peek();
        if (parent.IsArray)
        {
            return parent.Pointer + "/" + parent.NextIndex++.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return parent.Pointer + "/" + JsonPointerText.Escape(property ?? string.Empty);
    }

    private static int[] LineStarts(ReadOnlySpan<byte> bytes)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == (byte)'\n')
            {
                starts.Add(i + 1);
            }
        }

        return [.. starts];
    }

    private static SourceLocation ToLocation(ReadOnlySpan<byte> bytes, int[] lineStarts, int offset, int firstLine)
    {
        var index = Array.BinarySearch(lineStarts, offset);
        var line = index >= 0 ? index : ~index - 1;
        var column = Encoding.UTF8.GetCharCount(bytes[lineStarts[line]..offset]) + 1;
        return new SourceLocation(firstLine + line, column);
    }

    private sealed class Frame(string pointer, bool isArray)
    {
        public string Pointer { get; } = pointer;

        public bool IsArray { get; } = isArray;

        public int NextIndex { get; set; }
    }
}
