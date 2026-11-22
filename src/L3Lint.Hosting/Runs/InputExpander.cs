using System.Text;
using System.Text.RegularExpressions;

namespace L3Lint.Hosting.Runs;

/// <summary>One input to read: a file, or standard input.</summary>
/// <param name="FullPath">The full path, or "-" for standard input.</param>
/// <param name="DisplayPath">The path as reported in outputs (relative to the working folder, forward slashes).</param>
internal sealed record InputSource(string FullPath, string DisplayPath)
{
    public bool IsStandardInput => FullPath == "-";
}

/// <summary>
/// Expands command-line inputs (FR-001): files, folders (recursively, .json, .ndjson, .jsonl and .csv files),
/// globs with <c>*</c>, <c>?</c> and <c>**</c>, and "-" for standard input. Results are sorted ordinally so runs
/// are deterministic on every operating system.
/// </summary>
internal static class InputExpander
{
    private static readonly string[] Extensions = [".json", ".ndjson", ".jsonl", ".csv"];

    public static (List<InputSource> Sources, List<string> Unmatched) Expand(IEnumerable<string> inputs, string workingDirectory)
    {
        var sources = new List<InputSource>();
        var unmatched = new List<string>();
        var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            var found = ExpandOne(input, workingDirectory).ToList();
            if (found.Count == 0)
            {
                unmatched.Add(input);
            }

            foreach (var source in found.Where(s => s.IsStandardInput || seen.Add(s.FullPath)))
            {
                sources.Add(source);
            }
        }

        return (sources, unmatched);
    }

    private static IEnumerable<InputSource> ExpandOne(string input, string workingDirectory)
    {
        if (input == "-")
        {
            return [new InputSource("-", "-")];
        }

        var full = Path.GetFullPath(Path.Combine(workingDirectory, input));
        if (File.Exists(full))
        {
            return [new InputSource(full, Normalize(input))];
        }

        if (Directory.Exists(full))
        {
            return Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories)
                .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Select(f => new InputSource(f, Relative(workingDirectory, f)))
                .OrderBy(s => s.DisplayPath, StringComparer.Ordinal);
        }

        return input.IndexOfAny(['*', '?']) >= 0 ? Glob(input, workingDirectory) : [];
    }

    private static IEnumerable<InputSource> Glob(string pattern, string workingDirectory)
    {
        var normalized = Normalize(pattern);
        var segments = normalized.Split('/');
        var fixedCount = segments.TakeWhile(s => s.IndexOfAny(['*', '?']) < 0).Count();
        var baseDirectory = Path.GetFullPath(Path.Combine(workingDirectory, string.Join('/', segments.Take(fixedCount))));
        if (!Directory.Exists(baseDirectory))
        {
            return [];
        }

        var regex = ToRegex(string.Join('/', segments.Skip(fixedCount)));
        return Directory.EnumerateFiles(baseDirectory, "*", SearchOption.AllDirectories)
            .Where(f => regex.IsMatch(Normalize(Path.GetRelativePath(baseDirectory, f))))
            .Select(f => new InputSource(f, Relative(workingDirectory, f)))
            .OrderBy(s => s.DisplayPath, StringComparer.Ordinal);
    }

    private static Regex ToRegex(string glob)
    {
        var builder = new StringBuilder("^");
        for (var i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
            {
                var slash = i + 2 < glob.Length && glob[i + 2] == '/';
                builder.Append(slash ? "(.*/)?" : ".*");
                i += slash ? 2 : 1;
            }
            else if (c == '*')
            {
                builder.Append("[^/]*");
            }
            else if (c == '?')
            {
                builder.Append("[^/]");
            }
            else
            {
                builder.Append(Regex.Escape(c.ToString()));
            }
        }

        return new Regex(builder.Append('$').ToString(), RegexOptions.CultureInvariant | (OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase : RegexOptions.None), TimeSpan.FromSeconds(1));
    }

    private static string Relative(string workingDirectory, string path) => Normalize(Path.GetRelativePath(workingDirectory, path));

    private static string Normalize(string path) => path.Replace('\\', '/');
}
