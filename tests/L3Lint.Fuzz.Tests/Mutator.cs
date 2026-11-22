using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace L3Lint.Fuzz.Tests;

/// <summary>
/// Deterministic input mutations for the fuzz smoke tests. Every choice comes from one seeded
/// <see cref="Random"/>, so a run with the same seed produces the same inputs on every machine.
/// </summary>
/// <remarks>
/// The mutations follow the brief for T-43: bit flips, truncations, deep nesting, huge numbers, odd Unicode and
/// duplicate keys, plus type swaps, oversized arrays, YAML tags, anchors and aliases, and CSV quoting tricks. YAML
/// nesting stays at or below <see cref="MaxYamlNesting"/>: deeper YAML crashes the process with a stack overflow
/// (see docs/dev/security-review.md), which a separate child-process test in L3Lint.Security.Tests covers.
/// </remarks>
internal sealed class Mutator(int seed)
{
    public const int MaxYamlNesting = 300;

    /// <summary>Only the public test card numbers allowed by the fixture rules.</summary>
    private static readonly string[] TestCards = ["4111 1111 1111 1111", "5555555555554444", "3782 822463 10005", "6011-1111-1111-1117"];

    private static readonly string[] HugeNumbers =
    [
        "1e400", "-1e400", "1E+2147483648", "123456789012345678901234567890123456789", "79228162514264337593543950336",
        "-79228162514264337593543950336", "0.00000000000000000000000000000001", "-0", "-0.0", "4111111111111111",
        "9223372036854775808", "2147483648", "-2147483649", "1e-400", "0e0",
    ];

    private static readonly string[] OddStrings =
    [
        string.Empty, "\u0000", "\u202e\u202dabc", "\ufeffbom", "\u200b", "e\u0301\u0301\u0301", "\U0001F600", "\u65e5\u672c\u8a9e",
        "a\r\nb", "\t", "\\", "\"", "</script>", "]]>", "<!--", "&amp;", "{{index}}", "{actual}", "%s%n", "'; --", "=1+1",
        "\uffff", "\u00a0", new string('x', 4000), "Item", "N/A", "TEST", "4111 1111 1111 1111",
    ];

    private static readonly byte[] InterestingBytes = [0x00, 0xFF, 0xC0, 0x80, 0xED, 0xA0, 0xEF, 0xBB, 0xBF, (byte)'"', (byte)'{', (byte)'}', (byte)'[', (byte)']', (byte)':', (byte)',', (byte)'\n', (byte)'\r', (byte)'\\', (byte)'\'', (byte)'&', (byte)'*', (byte)'!', (byte)'-', (byte)'#'];

    private readonly Random _random = new(seed);

    /// <summary>Builds <paramref name="count"/> inputs: the seeds unchanged first, then stacked mutations of them.</summary>
    public List<byte[]> Inputs(string target, IReadOnlyList<byte[]> seeds, int count)
    {
        var inputs = new List<byte[]>(count);
        inputs.AddRange(seeds.Take(count));
        while (inputs.Count < count)
        {
            var seedInput = seeds[_random.Next(seeds.Count)];
            inputs.Add(Mutate(target, seedInput, seeds));
        }

        return inputs;
    }

    private byte[] Mutate(string target, byte[] input, IReadOnlyList<byte[]> seeds)
    {
        var result = input;
        var structured = _random.Next(3);
        for (var i = 0; i < structured; i++)
        {
            result = target switch
            {
                "json" => MutateJson(result),
                "csv" => MutateCsv(result),
                _ => MutateYaml(result),
            };
        }

        var raw = structured == 0 ? 1 + _random.Next(3) : _random.Next(2);
        for (var i = 0; i < raw; i++)
        {
            result = MutateBytes(result, seeds);
        }

        return result;
    }

    private byte[] MutateBytes(byte[] input, IReadOnlyList<byte[]> seeds)
    {
        var bytes = new List<byte>(input);
        switch (_random.Next(8))
        {
            case 0 when bytes.Count > 0:
                for (var i = 1 + _random.Next(8); i > 0; i--)
                {
                    var at = _random.Next(bytes.Count);
                    bytes[at] ^= (byte)(1 << _random.Next(8));
                }

                break;
            case 1 when bytes.Count > 0:
                // Truncation.
                return [.. input.Take(_random.Next(input.Length))];
            case 2 when bytes.Count > 0:
                for (var i = 1 + _random.Next(4); i > 0; i--)
                {
                    bytes[_random.Next(bytes.Count)] = InterestingBytes[_random.Next(InterestingBytes.Length)];
                }

                break;
            case 3 when bytes.Count > 1:
                var start = _random.Next(bytes.Count);
                bytes.RemoveRange(start, Math.Min(bytes.Count - start, 1 + _random.Next(64)));
                break;
            case 4 when bytes.Count > 1:
                var from = _random.Next(bytes.Count);
                var slice = bytes.Skip(from).Take(1 + _random.Next(128)).ToList();
                bytes.InsertRange(_random.Next(bytes.Count + 1), slice);
                break;
            case 5:
                var other = seeds[_random.Next(seeds.Count)];
                return [.. input.Take(_random.Next(input.Length + 1)), .. other.Skip(_random.Next(other.Length + 1))];
            case 6:
                var noise = new byte[1 + _random.Next(16)];
                _random.NextBytes(noise);
                bytes.InsertRange(_random.Next(bytes.Count + 1), noise);
                break;
            default:
                bytes.InsertRange(_random.Next(bytes.Count + 1), Encoding.UTF8.GetBytes(OddStrings[_random.Next(OddStrings.Length)]));
                break;
        }

        return [.. bytes];
    }

    private byte[] MutateJson(byte[] input)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(input, documentOptions: new JsonDocumentOptions { MaxDepth = 512, AllowDuplicateProperties = false });
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            // Earlier mutations made the text invalid JSON (or gave it duplicate keys); keep mutating it as bytes.
            return MutateBytes(input, [input]);
        }

        if (root is null)
        {
            return input;
        }

        var choice = _random.Next(9);
        if (choice == 0)
        {
            return Encoding.UTF8.GetBytes(DuplicateKeys(root));
        }

        if (choice == 1)
        {
            // NDJSON: several compact copies, sometimes with a blank or broken line between them.
            var line = root.ToJsonString();
            var lines = Enumerable.Range(0, 2 + _random.Next(4)).Select(_ => _random.Next(5) == 0 ? line[..(line.Length / 2)] : line);
            return Encoding.UTF8.GetBytes(string.Join(_random.Next(2) == 0 ? "\n" : "\r\n", lines) + "\n");
        }

        var containers = Containers(root).ToList();
        var (parent, key) = Pick(containers);
        switch (choice)
        {
            case 2:
                var depth = 1 + _random.Next(120);
                var wrapped = Detach(parent, key);
                for (var i = 0; i < depth; i++)
                {
                    wrapped = _random.Next(2) == 0 ? new JsonArray(wrapped) : new JsonObject { ["n"] = wrapped };
                }

                Set(parent, key, wrapped);
                break;
            case 3:
                Set(parent, key, JsonNode.Parse(HugeNumbers[_random.Next(HugeNumbers.Length)], documentOptions: default));
                break;
            case 4:
                Set(parent, key, JsonValue.Create(OddStrings[_random.Next(OddStrings.Length)]));
                break;
            case 5:
                Set(parent, key, SwapType(Get(parent, key)));
                break;
            case 6:
                if (parent is JsonObject removeFrom && key is string name)
                {
                    removeFrom.Remove(name);
                }

                break;
            case 7:
                Set(parent, key, JsonValue.Create(TestCards[_random.Next(TestCards.Length)]));
                break;
            default:
                if (root is JsonObject obj && obj["lineItems"] is JsonArray lines && lines.Count > 0)
                {
                    var copies = _random.Next(3) == 0 ? 1001 : 1 + _random.Next(120);
                    var template = lines[0]!.ToJsonString();
                    for (var i = 0; i < copies; i++)
                    {
                        lines.Add(JsonNode.Parse(template));
                    }
                }

                break;
        }

        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    private string DuplicateKeys(JsonNode root)
    {
        var builder = new StringBuilder();
        Write(root, builder);
        return builder.ToString();
    }

    // Writes JSON by hand so a property can appear twice, which JsonNode cannot hold.
    private void Write(JsonNode? node, StringBuilder builder)
    {
        switch (node)
        {
            case JsonObject obj:
                builder.Append('{');
                var first = true;
                foreach (var (key, value) in obj)
                {
                    var times = _random.Next(6) == 0 ? 2 : 1;
                    for (var i = 0; i < times; i++)
                    {
                        builder.Append(first ? string.Empty : ",").Append(JsonSerializer.Serialize(key)).Append(':');
                        Write(value, builder);
                        first = false;
                    }
                }

                builder.Append('}');
                break;
            case JsonArray array:
                builder.Append('[');
                for (var i = 0; i < array.Count; i++)
                {
                    builder.Append(i == 0 ? string.Empty : ",");
                    Write(array[i], builder);
                }

                builder.Append(']');
                break;
            default:
                builder.Append(node?.ToJsonString() ?? "null");
                break;
        }
    }

    private JsonNode? SwapType(JsonNode? value) => value switch
    {
        JsonObject => new JsonArray(1, "a", null),
        JsonArray => JsonValue.Create("array"),
        JsonValue v when v.GetValueKind() == JsonValueKind.String => JsonValue.Create(_random.Next(1000) / 10m),
        JsonValue => JsonValue.Create(value.ToJsonString()),
        _ => new JsonObject { ["x"] = true },
    };

    private static IEnumerable<(JsonNode Parent, object Key)> Containers(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, child) in obj.ToList())
                {
                    yield return (obj, key);
                    if (child is not null)
                    {
                        foreach (var inner in Containers(child))
                        {
                            yield return inner;
                        }
                    }
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    yield return (array, i);
                    if (array[i] is { } child)
                    {
                        foreach (var inner in Containers(child))
                        {
                            yield return inner;
                        }
                    }
                }

                break;
        }
    }

    private (JsonNode Parent, object Key) Pick(List<(JsonNode Parent, object Key)> slots) =>
        slots.Count == 0 ? (new JsonObject(), "x") : slots[_random.Next(slots.Count)];

    private static JsonNode? Get(JsonNode parent, object key) => parent is JsonObject obj ? obj[(string)key] : ((JsonArray)parent)[(int)key];

    private static JsonNode? Detach(JsonNode parent, object key)
    {
        var value = Get(parent, key);
        Set(parent, key, null);
        return value;
    }

    private static void Set(JsonNode parent, object key, JsonNode? value)
    {
        if (parent is JsonObject obj)
        {
            obj[(string)key] = value;
        }
        else
        {
            ((JsonArray)parent)[(int)key] = value;
        }
    }

    private byte[] MutateYaml(byte[] input)
    {
        var lines = Encoding.UTF8.GetString(input).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        if (lines.Count == 0)
        {
            return input;
        }

        var at = _random.Next(lines.Count);
        var line = lines[at];
        var indent = line.Length - line.TrimStart().Length;
        var pad = new string(' ', indent);
        switch (_random.Next(11))
        {
            case 0:
                // Duplicate a line, which usually repeats a mapping key.
                lines.Insert(at, line);
                break;
            case 1:
                lines.RemoveAt(at);
                break;
            case 2:
                lines[at] = ReplaceValue(line, HugeNumbers[_random.Next(HugeNumbers.Length)]);
                break;
            case 3:
                lines[at] = ReplaceValue(line, JsonSerializer.Serialize(OddStrings[_random.Next(OddStrings.Length)]));
                break;
            case 4:
                string[] tags = ["!!python/object/apply:os.system [\"x\"]", "!custom x", "!!binary aGVsbG8=", "!<tag:example.com,2026:t> x", "!!timestamp 2026-10-19", "!!set {a, b}", "!!omap [a: 1]"];
                lines[at] = ReplaceValue(line, tags[_random.Next(tags.Length)]);
                break;
            case 5:
                lines[at] = ReplaceValue(line, "&a " + (line.Contains(':', StringComparison.Ordinal) ? line[(line.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim() : "x"));
                lines.Insert(Math.Min(lines.Count, at + 1), pad + "alias" + at.ToString(CultureInfo.InvariantCulture) + ": *a");
                break;
            case 6:
                var depth = 1 + _random.Next(MaxYamlNesting);
                lines[at] = ReplaceValue(line, new string('[', depth) + "1" + new string(']', depth));
                break;
            case 7:
                lines.Insert(at, pad + "lol0: &l0 [lol, lol, lol, lol, lol, lol, lol, lol, lol]");
                for (var i = 1; i <= 8; i++)
                {
                    var previous = "*l" + (i - 1).ToString(CultureInfo.InvariantCulture);
                    lines.Insert(at + i, pad + "lol" + i.ToString(CultureInfo.InvariantCulture) + ": &l" + i.ToString(CultureInfo.InvariantCulture) + " [" + string.Join(", ", Enumerable.Repeat(previous, 9)) + "]");
                }

                break;
            case 8:
                // Shift indentation, which moves keys between mappings or breaks the document.
                lines[at] = new string(' ', Math.Max(0, indent + ((_random.Next(5) - 2) * 2))) + line.TrimStart();
                break;
            case 9:
                string[] scalars = ["", "~", "null", "yes", "no", "0x1F", "0o17", ".inf", "-.inf", ".nan", "2026-13-45", "'quoted'", "{}", "[]", "\"\\u0000\"", "|", ">", "*undefined", "- a", "? key"];
                lines[at] = ReplaceValue(line, scalars[_random.Next(scalars.Length)]);
                break;
            default:
                var other = _random.Next(lines.Count);
                (lines[at], lines[other]) = (lines[other], lines[at]);
                break;
        }

        return Encoding.UTF8.GetBytes(string.Join(_random.Next(4) == 0 ? "\r\n" : "\n", lines));
    }

    private static string ReplaceValue(string line, string value)
    {
        var colon = line.IndexOf(": ", StringComparison.Ordinal);
        if (colon >= 0)
        {
            return line[..(colon + 2)] + value;
        }

        var dash = line.IndexOf("- ", StringComparison.Ordinal);
        return dash >= 0 ? line[..(dash + 2)] + value : line + " " + value;
    }

    private byte[] MutateCsv(byte[] input)
    {
        var text = Encoding.UTF8.GetString(input).Replace("\r\n", "\n", StringComparison.Ordinal);
        var rows = text.Split('\n').Select(r => r.Split(',').ToList()).ToList();
        if (rows.Count == 0)
        {
            return input;
        }

        var row = rows[_random.Next(rows.Count)];
        var cell = _random.Next(Math.Max(1, row.Count));
        string[] values =
        [
            "", "\"", "\"\"", "\"a,b\"", "\"unterminated", "a\"b", "1e400", "-0", "NaN", "79228162514264337593543950336", "0.123456789",
            "=SUM(A1:A9)", "4111 1111 1111 1111", "\u00e9", "\u0000", "  12.50  ", "12,50", "1.2.3", "-5", "99999999999",
        ];
        switch (_random.Next(7))
        {
            case 0 when row.Count > 0:
                row[cell] = values[_random.Next(values.Length)];
                break;
            case 1 when row.Count > 0:
                row[cell] = OddStrings[_random.Next(OddStrings.Length)].Replace(",", string.Empty, StringComparison.Ordinal);
                break;
            case 2:
                var copy = rows[_random.Next(rows.Count)].ToList();
                var times = _random.Next(4) == 0 ? 1200 : 1 + _random.Next(20);
                for (var i = 0; i < times; i++)
                {
                    rows.Insert(_random.Next(1, rows.Count + 1), copy);
                }

                break;
            case 3 when row.Count > 0:
                row.RemoveAt(cell);
                break;
            case 4:
                row.Insert(cell, values[_random.Next(values.Length)]);
                break;
            case 5:
                rows.RemoveAt(_random.Next(rows.Count));
                break;
            default:
                return Encoding.UTF8.GetBytes(string.Join("\n", rows.Select(r => string.Join(_random.Next(2) == 0 ? ";" : "\t", r))));
        }

        return Encoding.UTF8.GetBytes(string.Join(_random.Next(3) == 0 ? "\r\n" : "\n", rows.Select(r => string.Join(",", r))));
    }
}
