namespace L3Lint.Core.Adapters;

/// <summary>One adapter's detection score for a document.</summary>
/// <param name="Format">The adapter name.</param>
/// <param name="Score">The score from 0.0 to 1.0.</param>
public readonly record struct DetectionScore(string Format, double Score);
