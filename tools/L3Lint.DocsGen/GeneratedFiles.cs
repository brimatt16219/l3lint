namespace L3Lint.DocsGen;

/// <summary>
/// Collects generated files and documentation problems, then writes the files (LF line endings) or, in check mode,
/// reports every file that differs, is missing or should no longer exist.
/// </summary>
internal sealed class GeneratedFiles(string root)
{
    private readonly SortedDictionary<string, string> _files = new(StringComparer.Ordinal);
    private readonly List<(string Folder, string Pattern)> _ownedFolders = [];
    private readonly List<string> _problems = [];

    public void Add(string relativePath, string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.EndsWith('\n'))
        {
            normalized += "\n";
        }

        if (!_files.TryAdd(relativePath, normalized))
        {
            throw new InvalidOperationException("Generated twice: " + relativePath);
        }
    }

    /// <summary>Declares that every file matching the pattern in the folder is generated, so others are stale.</summary>
    public void Own(string relativeFolder, string searchPattern) => _ownedFolders.Add((relativeFolder, searchPattern));

    /// <summary>Records a documentation gap, such as a rule or option without a description (NFR-014).</summary>
    public void Problem(string message) => _problems.Add(message);

    public int Finish(bool check, TextWriter output, TextWriter error)
    {
        var written = 0;
        var stale = 0;
        foreach (var (relative, text) in _files)
        {
            var path = FullPath(relative);
            var current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
            if (current == text)
            {
                continue;
            }

            if (check)
            {
                error.WriteLine((current is null ? "missing: " : "stale: ") + relative);
                stale++;
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            written++;
        }

        foreach (var (folder, pattern) in _ownedFolders)
        {
            var directory = FullPath(folder);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (_files.ContainsKey(relative))
                {
                    continue;
                }

                if (check)
                {
                    error.WriteLine("not generated: " + relative);
                    stale++;
                }
                else
                {
                    File.Delete(file);
                    output.WriteLine("deleted: " + relative);
                }
            }
        }

        foreach (var problem in _problems)
        {
            error.WriteLine("undocumented: " + problem);
        }

        output.WriteLine(check
            ? $"{_files.Count} generated file(s) checked, {stale} stale, {_problems.Count} documentation problem(s)."
            : $"{_files.Count} generated file(s), {written} written, {_problems.Count} documentation problem(s).");
        return stale == 0 && _problems.Count == 0 ? 0 : 1;
    }

    private string FullPath(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
}
