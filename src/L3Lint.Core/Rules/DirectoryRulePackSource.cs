using L3Lint.Core.Tiers;

namespace L3Lint.Core.Rules;

/// <summary>
/// A local rule pack folder loaded with <c>--rule-pack</c>, for rule authors.
/// </summary>
/// <remarks>
/// The folder needs a <c>manifest.json</c> like a shipped pack (ADR-0024). Rules may name only checks that
/// are compiled into the tool; a local pack never loads code. Files are read once, at startup.
/// </remarks>
public sealed class DirectoryRulePackSource : IRulePackSource
{
    private readonly string _root;
    private readonly HashSet<string> _files;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="folder">The pack folder.</param>
    /// <exception cref="DirectoryNotFoundException">The folder does not exist.</exception>
    public DirectoryRulePackSource(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        _root = Path.GetFullPath(folder);
        if (!Directory.Exists(_root))
        {
            throw new DirectoryNotFoundException(folder);
        }

        Files = [.. Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_root, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.Ordinal)];
        _files = new HashSet<string>(Files, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public string Origin => _root;

    /// <inheritdoc/>
    public IReadOnlyList<string> Files { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ICustomCheck> CustomChecks => [];

    /// <inheritdoc/>
    public IReadOnlyList<IBatchCheck> BatchChecks => [];

    /// <inheritdoc/>
    public ITierPolicy? TierPolicy => null;

    /// <inheritdoc/>
    /// <remarks>Only the files found in the folder at construction can be opened, so no path can reach outside it.</remarks>
    public Stream Open(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        var normalized = Path.GetRelativePath(_root, full).Replace('\\', '/');
        if (!_files.Contains(normalized))
        {
            throw new FileNotFoundException(relativePath);
        }

        return File.OpenRead(full);
    }
}
