namespace L3Lint.Architecture.Tests;

/// <summary>Finds the repository root by walking up from the test binaries to the folder that holds L3Lint.slnx.</summary>
public static class Repository
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    public static string Root => RootPath.Value;

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("L3Lint.slnx was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>Lists files under a repository folder, skipping build output.</summary>
    public static IEnumerable<string> Files(string folder, params string[] extensions)
    {
        var root = Path.Combine(Root, folder);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => extensions.Length == 0 || extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !IsBuildOutput(f))
            .Order(StringComparer.Ordinal);
    }

    public static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static bool IsBuildOutput(string path)
    {
        var segments = Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(s => s is "bin" or "obj" or "node_modules" or "_site" or "StrykerOutput" or "BenchmarkDotNet.Artifacts");
    }
}
