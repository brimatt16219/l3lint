namespace L3Lint.Hosting;

/// <summary>
/// One requested output: a format, written to a file or to standard output.
/// </summary>
/// <param name="Format">text, json, sarif, junit or markdown.</param>
/// <param name="Path">The file to write, or <see langword="null"/> for standard output.</param>
public sealed record OutputTarget(string Format, string? Path)
{
    /// <summary>
    /// Parses "kind" or "kind=path".
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="workingDirectory">The folder relative paths resolve from.</param>
    /// <returns>The target.</returns>
    public static OutputTarget Parse(string text, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(text);
        var equals = text.IndexOf('=', StringComparison.Ordinal);
        return equals < 0
            ? new OutputTarget(text.Trim().ToLowerInvariant(), null)
            : new OutputTarget(text[..equals].Trim().ToLowerInvariant(), System.IO.Path.GetFullPath(System.IO.Path.Combine(workingDirectory, text[(equals + 1)..].Trim())));
    }
}
