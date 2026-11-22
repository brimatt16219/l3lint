using System.Collections.Frozen;
using System.Reflection;

namespace L3Lint.Core.Schemas;

/// <summary>
/// The JSON Schemas (2020-12) that define l3lint's contracts: canonical payload, results, configuration,
/// rule packs, CSV maps, rate tables, processor profiles and the lock file.
/// </summary>
/// <remarks>The schemas are embedded in the assembly, so printing or validating them needs no file or network access.</remarks>
public static class SchemaCatalog
{
    private static readonly FrozenDictionary<string, string> Texts = Load();

    /// <summary>Gets the schema names in a stable order.</summary>
    public static IReadOnlyList<string> Names { get; } = ["canonical", "results", "config", "rulepack", "csvmap", "rates", "profile", "lock"];

    /// <summary>
    /// Gets the text of a schema.
    /// </summary>
    /// <param name="name">One of <see cref="Names"/>.</param>
    /// <param name="text">The schema JSON text when found.</param>
    /// <returns><see langword="true"/> when the name is known.</returns>
    public static bool TryGetText(string? name, out string text)
    {
        if (name is not null && Texts.TryGetValue(name, out var found))
        {
            text = found;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets the text of a schema.
    /// </summary>
    /// <param name="name">One of <see cref="Names"/>.</param>
    /// <returns>The schema JSON text.</returns>
    /// <exception cref="ArgumentException">The name is not one of <see cref="Names"/>.</exception>
    public static string GetText(string name) =>
        TryGetText(name, out var text) ? text : throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, Resources.Strings.SchemaUnknown, name, string.Join(", ", Names)), nameof(name));

    private static FrozenDictionary<string, string> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "canonical", "results", "config", "rulepack", "csvmap", "rates", "profile", "lock" })
        {
            var resource = $"L3Lint.Schemas.{name}.schema.json";
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded resource {resource} is missing.");
            using var reader = new StreamReader(stream);
            texts[name] = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        return texts.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
