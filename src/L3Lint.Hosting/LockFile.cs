using System.Globalization;
using System.Text;
using System.Text.Json;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Hosting;

/// <summary>
/// Reads and writes <c>.l3lint.lock</c>, which pins rule pack versions so CI results stay reproducible
/// (clarification 14, ADR-0024, ADR-0041).
/// </summary>
public static class LockFile
{
    /// <summary>The lock file name.</summary>
    public const string FileName = ".l3lint.lock";

    /// <summary>
    /// Reads a lock file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>Pack ID to locked version.</returns>
    /// <exception cref="ConfigurationException">The file is unreadable or does not match <c>lock.schema.json</c>.</exception>
    public static IReadOnlyDictionary<string, string> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        JsonElement root;
        try
        {
            root = JsonElementFromYaml.Convert(YamlJson.Parse(File.ReadAllText(path)));
        }
        catch (YamlFormatException ex)
        {
            throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ConfigYaml, path, ex.Line, ex.Column, ex.Message), path, null, ex);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ConfigUnreadable, path, ex.Message), path, null, ex);
        }

        var errors = SchemaValidator.For("lock").Validate(root);
        if (errors.Count > 0)
        {
            throw new ConfigurationException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ConfigInvalid, path, errors[0].Pointer, SchemaMessages.Describe(errors[0])), path, errors[0].Pointer);
        }

        return root.GetProperty("packs").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
    }

    /// <summary>
    /// Renders lock file content for the given packs.
    /// </summary>
    /// <param name="packs">The loaded packs.</param>
    /// <returns>YAML text with LF line endings.</returns>
    public static string Render(IEnumerable<RulePackInfo> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        var builder = new StringBuilder();
        builder.Append("# Pins l3lint rule pack versions. A mismatch with the loaded packs stops the run with exit code 3.\n");
        builder.Append("version: 1\n");
        builder.Append("packs:\n");
        foreach (var pack in packs.OrderBy(p => p.Id, StringComparer.Ordinal))
        {
            builder.Append("  ").Append(pack.Id).Append(": ").Append(pack.Version).Append('\n');
        }

        return builder.ToString();
    }
}
