using System.Text;

namespace L3Lint.Reporting.Tests;

/// <summary>
/// Reads the checked-in expected output under Expected/ (copied to the build output) and compares it with what a
/// writer produced, after normalizing line endings to LF (ADR-0053). The expected files are written by hand from the
/// spec and the fixture; nothing here creates or updates them.
/// </summary>
internal static class ExpectedFiles
{
    public static string Read(string fileName) =>
        Lf(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Expected", fileName), Encoding.UTF8));

    public static void ShouldMatch(string actual, string fileName) =>
        Lf(actual).ShouldBe(Read(fileName), "Writer output differs from Expected/" + fileName + ".");

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
