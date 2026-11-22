namespace L3Lint.Adapters.Tests.Support;

/// <summary>Finds files in the repository by walking up from the test output folder to the folder holding L3Lint.slnx.</summary>
internal static class Repo
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    public static string Root => RootPath.Value;

    public static string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("L3Lint.slnx was not found above " + AppContext.BaseDirectory);
    }
}
