using L3Lint.Core.Fixes;
using L3Lint.Core.Input;
using L3Lint.Core.Model;

namespace L3Lint.Core;

/// <summary>
/// One rule result on one transaction: what is wrong, where, why it matters, how to fix it and the source
/// that says so.
/// </summary>
/// <remarks>
/// Findings are advisory (D5): an error means the payload is likely to downgrade, not that a network will
/// reject it. Every value shown here has already been masked for card numbers (NFR-007).
/// </remarks>
public sealed record Finding
{
    /// <summary>Gets the rule ID, for example VISA-L3-004, or an engine diagnostic ID such as L3LINT-ENGINE-001.</summary>
    public required string RuleId { get; init; }

    /// <summary>Gets the effective severity after profile scoping and overrides.</summary>
    public required Severity Severity { get; init; }

    /// <summary>Gets what the finding means for the interchange tier.</summary>
    public required Impact Impact { get; init; }

    /// <summary>Gets the brand whose pack produced the finding, or <see langword="null"/> for general rules.</summary>
    public CardBrand? Brand { get; init; }

    /// <summary>Gets the 0-based index of the transaction in the run.</summary>
    public required int TransactionIndex { get; init; }

    /// <summary>Gets the canonical and source paths.</summary>
    public required FieldPath Path { get; init; }

    /// <summary>Gets the line and column in the input file, when known.</summary>
    public SourceLocation? Location { get; init; }

    /// <summary>Gets the plain-language message.</summary>
    public required string Message { get; init; }

    /// <summary>Gets what the rule expected, when it can be stated.</summary>
    public string? Expected { get; init; }

    /// <summary>Gets the value found, already masked; <see langword="null"/> when the value is absent.</summary>
    public string? Actual { get; init; }

    /// <summary>Gets the fix, stated as an action.</summary>
    public required string Fix { get; init; }

    /// <summary>Gets a mechanical JSON Patch for this finding, when one exists.</summary>
    public FixPatch? Patch { get; init; }

    /// <summary>Gets a value indicating whether a person must decide the fix (for example a proposed unit mapping).</summary>
    public bool NeedsHuman { get; init; }

    /// <summary>Gets the confidence label of the rule.</summary>
    public required Confidence Confidence { get; init; }

    /// <summary>Gets the public sources of the rule.</summary>
    public required IReadOnlyList<Uri> Sources { get; init; }
}
