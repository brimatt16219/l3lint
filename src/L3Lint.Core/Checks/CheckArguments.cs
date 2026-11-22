using System.Globalization;

namespace L3Lint.Core.Checks;

/// <summary>Builds message placeholder values. Allocates only when a finding is reported.</summary>
internal static class CheckArguments
{
    /// <summary>The placeholders a message template may use; packs are validated against this set.</summary>
    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        "index", "field", "actual", "expected", "count", "max", "min", "length", "ratio", "total",
        "detail", "brand", "difference", "reported", "list", "share", "transactions", "first", "masked", "lines", "currency",
    };

    public static Dictionary<string, string> For(string pointer, int? displayIndex)
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal) { ["field"] = pointer };
        if (displayIndex is { } index)
        {
            arguments["index"] = index.ToString(CultureInfo.InvariantCulture);
        }

        return arguments;
    }

    public static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Formats a ratio as a percentage with up to 2 decimals, for example 0.0619 becomes 6.19%.</summary>
    public static string Percent(decimal ratio) =>
        Math.Round(ratio * 100m, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
