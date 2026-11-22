namespace L3Lint.FixtureGen;

/// <summary>Finds the repository root (the folder holding L3Lint.slnx).</summary>
internal static class Repository
{
    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("L3Lint.slnx not found above " + AppContext.BaseDirectory);
    }
}
