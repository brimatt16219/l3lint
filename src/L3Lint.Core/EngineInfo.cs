using System.Reflection;

namespace L3Lint.Core;

/// <summary>
/// Version and project links of the running engine, for outputs such as SARIF and JSON.
/// </summary>
public static class EngineInfo
{
    private static readonly Assembly Assembly = typeof(EngineInfo).Assembly;

    /// <summary>Gets the engine version, for example 1.0.0.</summary>
    public static string Version { get; } =
        (Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

    /// <summary>Gets the project home page.</summary>
    public static string ProjectUrl { get; } = Metadata("L3LintProjectUrl");

    /// <summary>Gets the documentation site root, ending with a slash.</summary>
    public static string DocsUrl { get; } = Metadata("L3LintDocsUrl");

    /// <summary>Gets the rule reference page for a rule.</summary>
    /// <param name="ruleId">The rule ID.</param>
    /// <returns>An absolute URL.</returns>
    public static Uri RuleHelpUri(string ruleId) => new(DocsUrl + "rules/" + ruleId + ".html");

    private static string Metadata(string key) =>
        Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value
        ?? throw new InvalidOperationException($"Assembly metadata {key} is missing.");
}
