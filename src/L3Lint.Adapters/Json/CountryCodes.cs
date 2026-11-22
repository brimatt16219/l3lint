using System.Collections.Frozen;
using System.Reflection;

namespace L3Lint.Adapters.Json;

/// <summary>
/// ISO 3166-1 alpha-3 and alpha-2 country codes, so gateways that send alpha-3 destinations (Braintree
/// <c>countryCodeAlpha3</c>, Adyen <c>destinationCountryCode</c>) map to the canonical alpha-2 code.
/// </summary>
/// <remarks>Loaded once from an embedded table; immutable and thread-safe. Matching is case-sensitive, as ISO codes are uppercase.</remarks>
internal static class CountryCodes
{
    private const string ResourceName = "L3Lint.Adapters.Data.iso3166.csv";

    private static readonly FrozenDictionary<string, string> ToAlpha2 = Load();
    private static readonly FrozenDictionary<string, string> ToAlpha3 =
        ToAlpha2.ToFrozenDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    /// <summary>Gets the number of countries in the table.</summary>
    public static int Count => ToAlpha2.Count;

    /// <summary>Converts an alpha-3 code such as USA to its alpha-2 code.</summary>
    public static bool TryGetAlpha2(string alpha3, out string alpha2) => ToAlpha2.TryGetValue(alpha3, out alpha2!);

    /// <summary>Converts an alpha-2 code such as US to its alpha-3 code.</summary>
    public static bool TryGetAlpha3(string alpha2, out string alpha3) => ToAlpha3.TryGetValue(alpha2, out alpha3!);

    private static FrozenDictionary<string, string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded resource " + ResourceName + " is missing.");
        using var reader = new StreamReader(stream);
        var alpha2 = new Dictionary<string, string>(StringComparer.Ordinal);
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

            var parts = line.Split(',');
            alpha2[parts[0]] = parts[1];
        }

        return alpha2.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
