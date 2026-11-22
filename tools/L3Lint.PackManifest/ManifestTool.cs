using L3Lint.Core.Packs;
using Manifest = L3Lint.Core.Packs.PackManifest;

namespace L3Lint.PackManifest;

/// <summary>Computes SHA-256 manifests for rule pack folders (clarification 14, ADR-0024).</summary>
internal static class ManifestTool
{
    private static readonly string[] ShippedPacks = ["General", "Visa", "Mastercard", "Amex", "Discover"];

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var check = args.Contains("--check", StringComparer.Ordinal);
        var folders = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
        if (folders.Count == 0)
        {
            var root = FindRepositoryRoot();
            folders.AddRange(ShippedPacks.Select(p => Path.Combine(root, "src", "L3Lint.Rules." + p, "Pack")));
        }

        var stale = 0;
        foreach (var folder in folders)
        {
            var manifest = Compute(folder);
            var path = Path.Combine(folder, Manifest.FileName);
            var text = manifest.Write();
            var current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
            if (current == text)
            {
                output.WriteLine($"up to date: {path}");
                continue;
            }

            if (check)
            {
                error.WriteLine($"stale: {path}");
                stale++;
                continue;
            }

            File.WriteAllText(path, text);
            output.WriteLine($"written: {path}");
        }

        return stale == 0 ? 0 : 1;
    }

    public static Manifest Compute(string folder)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(folder, file).Replace('\\', '/');
            if (relative == Manifest.FileName)
            {
                continue;
            }

            files[relative] = PackHashing.Sha256(File.ReadAllBytes(file));
        }

        var rulesFile = files.Keys.Single(f => f.EndsWith(".rules.yaml", StringComparison.Ordinal) && !f.Contains('/', StringComparison.Ordinal));
        var (id, version) = ReadHeader(Path.Combine(folder, rulesFile));
        return new Manifest { Pack = id, Version = version, Files = files };
    }

    private static (string Id, string Version) ReadHeader(string rulesFile)
    {
        string? id = null;
        string? version = null;
        foreach (var line in File.ReadLines(rulesFile))
        {
            var trimmed = line.Trim();
            if (id is null && trimmed.StartsWith("id:", StringComparison.Ordinal) && line.StartsWith("  ", StringComparison.Ordinal))
            {
                id = trimmed[3..].Trim();
            }
            else if (version is null && trimmed.StartsWith("version:", StringComparison.Ordinal) && line.StartsWith("  ", StringComparison.Ordinal))
            {
                version = trimmed[8..].Trim();
            }

            if (trimmed == "rules:")
            {
                break;
            }
        }

        return (id ?? throw new InvalidDataException("pack.id not found in " + rulesFile), version ?? throw new InvalidDataException("pack.version not found in " + rulesFile));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L3Lint.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("L3Lint.slnx not found above " + AppContext.BaseDirectory);
    }
}
