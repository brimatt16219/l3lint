using System.Text;

namespace L3Lint.Core.Engine;

/// <summary>Fills <c>{name}</c> placeholders in rule messages. Placeholder names are validated when a pack loads.</summary>
internal static class MessageRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string>? arguments, string? actual, string? expected)
    {
        if (!template.Contains('{', StringComparison.Ordinal))
        {
            return template;
        }

        var builder = new StringBuilder(template.Length + 32);
        var i = 0;
        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            if (open < 0)
            {
                builder.Append(template, i, template.Length - i);
                break;
            }

            var close = template.IndexOf('}', open + 1);
            if (close < 0)
            {
                builder.Append(template, i, template.Length - i);
                break;
            }

            builder.Append(template, i, open - i);
            var name = template[(open + 1)..close];
            builder.Append(Value(name, arguments, actual, expected) ?? template[open..(close + 1)]);
            i = close + 1;
        }

        return builder.ToString();
    }

    private static string? Value(string name, IReadOnlyDictionary<string, string>? arguments, string? actual, string? expected)
    {
        if (arguments is not null && arguments.TryGetValue(name, out var value))
        {
            return value;
        }

        return name switch
        {
            "actual" => actual ?? Resources.Strings.ValueAbsent,
            "expected" => expected ?? string.Empty,
            _ => null,
        };
    }
}
