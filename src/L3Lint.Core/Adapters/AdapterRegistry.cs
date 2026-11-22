using System.Globalization;
using L3Lint.Core.Input;

namespace L3Lint.Core.Adapters;

/// <summary>
/// Default <see cref="IAdapterRegistry"/>. Detection picks the best adapter when it scores at least 0.8 and the
/// runner-up is more than 0.2 behind (Architecture tab, stage 2).
/// </summary>
internal sealed class AdapterRegistry : IAdapterRegistry
{
    public const double MinimumScore = 0.8;
    public const double TieMargin = 0.2;

    public AdapterRegistry(IEnumerable<IPayloadAdapter> adapters)
    {
        var list = new List<IPayloadAdapter> { new CanonicalAdapter() };
        foreach (var adapter in adapters)
        {
            if (list.Any(a => string.Equals(a.Name, adapter.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.AdapterDuplicate, adapter.Name), nameof(adapters));
            }

            list.Add(adapter);
        }

        Adapters = list;
    }

    public IReadOnlyList<IPayloadAdapter> Adapters { get; }

    public IReadOnlyList<DetectionScore> Score(RawDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return [.. Adapters
            .Where(a => a.Kind == document.Kind)
            .Select(a => new DetectionScore(a.Name, Math.Clamp(a.Detect(document), 0.0, 1.0)))
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Format, StringComparer.Ordinal)];
    }

    public IPayloadAdapter Resolve(string? format, RawDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!string.IsNullOrEmpty(format) && !string.Equals(format, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var named = Adapters.FirstOrDefault(a => string.Equals(a.Name, format, StringComparison.OrdinalIgnoreCase))
                ?? throw new FormatDetectionException(
                    string.Format(CultureInfo.InvariantCulture, Resources.Strings.FormatUnknown, format, string.Join(", ", Adapters.Select(a => a.Name))),
                    document.SourceName,
                    [.. Adapters.Select(a => a.Name)]);
            if (named.Kind != document.Kind)
            {
                throw new FormatDetectionException(
                    string.Format(CultureInfo.InvariantCulture, Resources.Strings.FormatKindMismatch, named.Name, document.Kind),
                    document.SourceName,
                    [named.Name]);
            }

            return named;
        }

        var scores = Score(document);
        var best = scores.Count > 0 ? scores[0] : default;
        if (scores.Count == 0 || best.Score < MinimumScore)
        {
            throw new FormatDetectionException(Resources.Strings.FormatNotDetected, document.SourceName, [.. scores.Where(s => s.Score > 0).Select(Describe)]);
        }

        if (scores.Count > 1 && best.Score - scores[1].Score <= TieMargin)
        {
            throw new FormatDetectionException(
                Resources.Strings.FormatTie,
                document.SourceName,
                [.. scores.Where(s => best.Score - s.Score <= TieMargin).Select(Describe)]);
        }

        return Adapters.First(a => a.Name == best.Format);
    }

    private static string Describe(DetectionScore score) =>
        score.Format + " (" + score.Score.ToString("0.00", CultureInfo.InvariantCulture) + ")";
}
