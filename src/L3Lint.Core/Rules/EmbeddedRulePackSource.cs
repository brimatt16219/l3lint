using System.Reflection;
using L3Lint.Core.Tiers;

namespace L3Lint.Core.Rules;

/// <summary>
/// A rule pack whose files are embedded resources named <c>L3Lint.Pack.{folder}/{path}</c>.
/// </summary>
/// <remarks>Each shipped pack assembly derives from this class and adds its compiled checks.</remarks>
public abstract class EmbeddedRulePackSource : IRulePackSource
{
    private readonly Assembly _assembly;
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="assembly">The assembly that embeds the pack.</param>
    /// <param name="folder">The pack folder name used in resource names, for example "visa".</param>
    protected EmbeddedRulePackSource(Assembly assembly, string folder)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(folder);
        _assembly = assembly;
        _prefix = $"L3Lint.Pack.{folder}/";
        Files = [.. assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(_prefix, StringComparison.Ordinal))
            .Select(n => n[_prefix.Length..].Replace('\\', '/'))
            .OrderBy(n => n, StringComparer.Ordinal)];
    }

    /// <inheritdoc/>
    public string Origin => "embedded";

    /// <inheritdoc/>
    public IReadOnlyList<string> Files { get; }

    /// <inheritdoc/>
    public virtual IReadOnlyList<ICustomCheck> CustomChecks => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<IBatchCheck> BatchChecks => [];

    /// <inheritdoc/>
    public virtual ITierPolicy? TierPolicy => null;

    /// <inheritdoc/>
    public Stream Open(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        return _assembly.GetManifestResourceStream(_prefix + relativePath)
            ?? _assembly.GetManifestResourceStream(_prefix + relativePath.Replace('/', '\\'))
            ?? throw new FileNotFoundException(relativePath);
    }
}
