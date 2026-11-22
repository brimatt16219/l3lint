using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Core.Profiles;

/// <summary>
/// The processor profiles embedded in l3lint, and a loader for custom profile files.
/// </summary>
/// <remarks>Embedded profiles load once and are immutable.</remarks>
public static class ProcessorProfiles
{
    private const string Prefix = "L3Lint.Profiles.";

    private static readonly FrozenDictionary<string, ProcessorProfile> Embedded = LoadEmbedded();

    /// <summary>Gets the default profile, which sets no processor-specific limits.</summary>
    public static ProcessorProfile Generic => Embedded["generic"];

    /// <summary>Gets every embedded profile, ordered by ID.</summary>
    public static IReadOnlyList<ProcessorProfile> All { get; } = [.. Embedded.Values.OrderBy(p => p.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Finds an embedded profile.
    /// </summary>
    /// <param name="id">The profile ID, for example "stripe".</param>
    /// <param name="profile">The profile when found.</param>
    /// <returns><see langword="true"/> when the ID is known.</returns>
    public static bool TryGet(string? id, out ProcessorProfile profile)
    {
        if (id is not null && Embedded.TryGetValue(id, out var found))
        {
            profile = found;
            return true;
        }

        profile = Generic;
        return false;
    }

    /// <summary>
    /// Parses a profile file.
    /// </summary>
    /// <param name="yaml">The profile YAML.</param>
    /// <param name="sourceName">The file name, used in errors.</param>
    /// <returns>The profile.</returns>
    /// <exception cref="RulePackException">The YAML is invalid or does not match <c>profile.schema.json</c>.</exception>
    public static ProcessorProfile Parse(string yaml, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        JsonElement element;
        try
        {
            element = JsonElementFromYaml.Convert(YamlJson.Parse(yaml));
        }
        catch (YamlFormatException ex)
        {
            throw new RulePackException(ex.Message, sourceName, sourceName, null, ex);
        }

        var errors = SchemaValidator.For("profile").Validate(element);
        if (errors.Count > 0)
        {
            var first = errors[0];
            throw new RulePackException(
                string.Format(CultureInfo.InvariantCulture, Resources.Strings.ProfileInvalid, sourceName, first.Pointer, SchemaMessages.Describe(first)),
                sourceName,
                sourceName,
                first.Pointer);
        }

        return Read(element);
    }

    private static ProcessorProfile Read(JsonElement root)
    {
        var header = root.GetProperty("profile");
        var limits = new Dictionary<string, ProfileLimit>(StringComparer.Ordinal);
        if (root.TryGetProperty("limits", out var limitElement))
        {
            foreach (var limit in limitElement.EnumerateObject())
            {
                limits[limit.Name] = new ProfileLimit(
                    limit.Value.GetProperty("value").GetInt32(),
                    new Uri(limit.Value.GetProperty("source").GetString()!),
                    limit.Value.TryGetProperty("note", out var note) ? note.GetString() : null);
            }
        }

        var disabled = new List<string>();
        var severity = new Dictionary<string, Severity>(StringComparer.Ordinal);
        if (root.TryGetProperty("rules", out var rules))
        {
            if (rules.TryGetProperty("disable", out var disable))
            {
                disabled.AddRange(disable.EnumerateArray().Select(e => e.GetString()!));
            }

            if (rules.TryGetProperty("severity", out var map))
            {
                foreach (var entry in map.EnumerateObject())
                {
                    severity[entry.Name] = SeverityNames.Parse(entry.Value.GetString()!);
                }
            }
        }

        return new ProcessorProfile
        {
            Id = header.GetProperty("id").GetString()!,
            Title = header.GetProperty("title").GetString()!,
            Description = header.TryGetProperty("description", out var description) ? description.GetString() : null,
            Sources = [.. header.GetProperty("sources").EnumerateArray().Select(s => new Uri(s.GetString()!))],
            Limits = limits.ToFrozenDictionary(StringComparer.Ordinal),
            DisabledRules = disabled,
            SeverityOverrides = severity.ToFrozenDictionary(StringComparer.Ordinal),
        };
    }

    private static FrozenDictionary<string, ProcessorProfile> LoadEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var profiles = new Dictionary<string, ProcessorProfile>(StringComparer.Ordinal);
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(Prefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var profile = Parse(reader.ReadToEnd(), name[Prefix.Length..]);
            profiles[profile.Id] = profile;
        }

        return profiles.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
