using L3Lint.Core.Input;

namespace L3Lint.Core.Pci;

/// <summary>The output of the scrub stage: the document to map (masked when needed) and what was masked.</summary>
internal sealed record ScrubResult(RawDocument Document, IReadOnlyList<CardNumberMatch> Matches);
