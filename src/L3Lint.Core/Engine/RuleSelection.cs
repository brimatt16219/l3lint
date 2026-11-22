using System.Text.RegularExpressions;
using L3Lint.Core.Profiles;

namespace L3Lint.Core.Engine;

/// <summary>
/// Which rules run and at what severity, from options and the profile (FR-018). Patterns accept <c>*</c> and <c>?</c>.
/// </summary>
internal sealed class RuleSelection
{
    private readonly IReadOnlyList<Regex> _only;
    private readonly IReadOnlyList<Regex> _disabled;
    private readonly IReadOnlyList<(Regex Pattern, Severity Severity)> _profileSeverity;
    private readonly IReadOnlyList<(Regex Pattern, Severity Severity)> _userSeverity;

    public RuleSelection(LintOptions options, ProcessorProfile profile)
    {
        _only = [.. options.OnlyRules.Select(Glob)];
        _disabled = [.. options.DisabledRules.Concat(profile.DisabledRules).Select(Glob)];
        _profileSeverity = [.. profile.SeverityOverrides.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (Glob(p.Key), p.Value))];
        _userSeverity = [.. options.SeverityOverrides.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (Glob(p.Key), p.Value))];
    }

    public bool Includes(string ruleId) =>
        (_only.Count == 0 || _only.Any(p => p.IsMatch(ruleId))) && !_disabled.Any(p => p.IsMatch(ruleId));

    /// <summary>Applies profile then user overrides; an exact ID beats a glob because later, more specific matches win.</summary>
    public Severity Override(string ruleId, Severity severity)
    {
        severity = Apply(_profileSeverity, ruleId, severity);
        return Apply(_userSeverity, ruleId, severity);
    }

    public static bool IsValidPattern(string pattern) => pattern.Length > 0 && pattern.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c) || c is '-' or '*' or '?');

    private static Severity Apply(IReadOnlyList<(Regex Pattern, Severity Severity)> overrides, string ruleId, Severity severity)
    {
        Severity? glob = null;
        foreach (var (pattern, value) in overrides)
        {
            if (!pattern.IsMatch(ruleId))
            {
                continue;
            }

            if (pattern.ToString() == "^" + Regex.Escape(ruleId) + "$")
            {
                return value;
            }

            glob = value;
        }

        return glob ?? severity;
    }

    private static Regex Glob(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
}
