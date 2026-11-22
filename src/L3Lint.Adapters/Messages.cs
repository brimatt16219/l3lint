using System.Globalization;

namespace L3Lint.Adapters;

/// <summary>Formats resource templates for notes and errors with the invariant culture (NFR-011).</summary>
internal static class Messages
{
    /// <summary>Formats a template from <see cref="Resources.Strings"/>.</summary>
    /// <param name="template">The template.</param>
    /// <param name="args">The values to insert.</param>
    /// <returns>The message.</returns>
    public static string Format(string template, params object?[] args) => string.Format(CultureInfo.InvariantCulture, template, args);
}
