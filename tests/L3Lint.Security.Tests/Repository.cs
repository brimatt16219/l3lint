namespace L3Lint.Security.Tests;

/// <summary>Finds the repository root by walking up from the test binaries to the folder that holds L3Lint.slnx.</summary>
internal static class Repository
{
    private static readonly Lazy<string> RootPath = new(() =>
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("L3Lint.slnx was not found above " + AppContext.BaseDirectory);
    });

    public static string Root => RootPath.Value;
}
