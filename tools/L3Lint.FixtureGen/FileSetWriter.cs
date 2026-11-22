using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace L3Lint.FixtureGen;

/// <summary>Writes generated files with LF endings, or in check mode reports files that differ.</summary>
internal sealed class FileSetWriter(string root, bool check, TextWriter output, TextWriter error)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private int _written;
    private int _stale;

    public void WriteJson(string relativePath, JsonNode node) => WriteText(relativePath, node.ToJsonString(Json) + "\n");

    public void WriteText(string relativePath, string text)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
        if (current == normalized)
        {
            return;
        }

        if (check)
        {
            error.WriteLine("stale: " + relativePath);
            _stale++;
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, normalized);
        _written++;
    }

    public int Finish()
    {
        output.WriteLine(check ? $"{_stale} stale file(s)" : $"{_written} file(s) written");
        return _stale == 0 ? 0 : 1;
    }
}
