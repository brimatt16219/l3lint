using System.Globalization;

namespace L3Lint.Core.Packs;

/// <summary>
/// Reads a pack list: a CSV whose first column holds the values, after <c>#</c> comment lines and a header row.
/// Values written as a 4-digit range such as <c>3000-3999</c> expand to every code in the range (MCC lists).
/// </summary>
internal static class PackListReader
{
    public static IReadOnlyList<string> Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var values = new List<string>();
        var headerSeen = false;
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (!headerSeen)
            {
                headerSeen = true;
                continue;
            }

            var value = FirstColumn(trimmed);
            if (value.Length == 0)
            {
                continue;
            }

            if (TryRange(value, out var from, out var to, out var width))
            {
                for (var code = from; code <= to; code++)
                {
                    values.Add(code.ToString(new string('0', width), CultureInfo.InvariantCulture));
                }
            }
            else
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static string FirstColumn(string line)
    {
        if (line.StartsWith('"'))
        {
            var end = line.IndexOf('"', 1);
            return end > 0 ? line[1..end] : line.Trim('"');
        }

        var comma = line.IndexOf(',');
        return (comma < 0 ? line : line[..comma]).Trim();
    }

    private static bool TryRange(string value, out int from, out int to, out int width)
    {
        from = to = width = 0;
        var dash = value.IndexOf('-');
        if (dash <= 0 || dash != value.Length - dash - 1)
        {
            return false;
        }

        var left = value[..dash];
        var right = value[(dash + 1)..];
        width = left.Length;
        return left.All(char.IsAsciiDigit) && right.All(char.IsAsciiDigit)
            && int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out from)
            && int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out to)
            && from <= to;
    }
}
