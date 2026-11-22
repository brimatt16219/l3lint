using System.Text.Json;
using L3Lint.Core.Input;

namespace L3Lint.Core.Engine;

/// <summary>Where findings of one transaction point: the source document, its patch target and the line it starts on.</summary>
internal sealed class DocumentContext
{
    /// <summary>The scrubbed source document, or null for in-memory transactions.</summary>
    public RawDocument? Document { get; init; }

    /// <summary>The JSON document patches apply to: the source, or the canonical form for CSV.</summary>
    public required JsonElement PatchRoot { get; init; }

    /// <summary>True when patches target the canonical document (CSV input).</summary>
    public bool TargetsCanonical { get; init; }

    /// <summary>The 1-based line where the transaction starts.</summary>
    public int FirstLine { get; init; } = 1;

    public string? Format { get; init; }

    public int Input { get; init; }

    public long OrderBase { get; init; }
}
