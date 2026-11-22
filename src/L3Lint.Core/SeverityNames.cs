namespace L3Lint.Core;

/// <summary>Parses severity names that schemas have already validated.</summary>
internal static class SeverityNames
{
    public static Severity Parse(string text) =>
        WireNames.TryParseSeverity(text, out var severity) ? severity : throw new FormatException($"Unknown severity '{text}'.");
}
