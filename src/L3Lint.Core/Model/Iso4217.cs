using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;

namespace L3Lint.Core.Model;

/// <summary>
/// The embedded ISO 4217 currency table with minor units.
/// </summary>
/// <remarks>
/// Built from the public ISO 4217 list one (source and retrieval date in the data file header). The table
/// loads once, is immutable and is safe to use from any thread. Codes are matched exactly: "usd" is not a
/// valid code (GEN-FMT-002).
/// </remarks>
public static class Iso4217
{
    private const string ResourceName = "L3Lint.Data.iso4217.csv";

    private static readonly FrozenDictionary<string, Currency> Table = Load();

    /// <summary>Gets every currency in the table, ordered by code.</summary>
    public static IReadOnlyList<Currency> All { get; } = [.. Table.Values.OrderBy(c => c.Code, StringComparer.Ordinal)];

    /// <summary>
    /// Looks up a currency by its alpha-3 code.
    /// </summary>
    /// <param name="code">The code, for example USD. Matching is case-sensitive.</param>
    /// <param name="currency">The currency when found.</param>
    /// <returns><see langword="true"/> when the code is an active ISO 4217 currency with numeric minor units.</returns>
    public static bool TryGetCurrency(string? code, out Currency currency)
    {
        if (code is not null && Table.TryGetValue(code, out currency))
        {
            return true;
        }

        currency = default;
        return false;
    }

    private static FrozenDictionary<string, Currency> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
        using var reader = new StreamReader(stream);
        var entries = new Dictionary<string, Currency>(StringComparer.Ordinal);
        var headerSeen = false;
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (!headerSeen)
            {
                headerSeen = true;
                continue;
            }

            // Rows are code,numeric,minorUnits,"name"; only the first and third columns matter here.
            var parts = line.Split(',', 4);
            var code = parts[0];
            var exponent = int.Parse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture);
            entries[code] = new Currency(code, exponent);
        }

        return entries.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
