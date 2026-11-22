namespace L3Lint.Core.Rules;

/// <summary>
/// A rule's embedded passing and failing examples (FR-020, US-05).
/// </summary>
/// <param name="PassFile">Pack-relative path of the passing example.</param>
/// <param name="FailFile">Pack-relative path of the failing example.</param>
/// <param name="PassJson">The passing canonical payload.</param>
/// <param name="FailJson">The failing canonical payload.</param>
/// <param name="PassNote">What makes the passing example pass.</param>
/// <param name="FailNote">What makes the failing example fail.</param>
/// <param name="Context">Context the examples need, such as an as-of date or a processor profile.</param>
public sealed record RuleExamples(
    string PassFile,
    string FailFile,
    string PassJson,
    string FailJson,
    string? PassNote,
    string? FailNote,
    RuleExampleContext Context);
