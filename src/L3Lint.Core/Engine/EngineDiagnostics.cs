using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>
/// Findings the engine itself raises, outside the 78-rule catalog (ADR-0033). Their IDs use the reserved
/// <c>L3LINT-</c> prefix.
/// </summary>
internal static class EngineDiagnostics
{
    public const string RuleFailedId = "L3LINT-ENGINE-001";
    public const string BrandUnknownId = "L3LINT-CTX-001";

    public static RuleMetadata RuleFailed { get; } = Create(
        RuleFailedId,
        Resources.Strings.DiagRuleFailedTitle,
        Resources.Strings.DiagRuleFailedMessage,
        Resources.Strings.DiagRuleFailedFix,
        Severity.Error,
        Impact.Hygiene);

    public static RuleMetadata BrandUnknown { get; } = Create(
        BrandUnknownId,
        Resources.Strings.DiagBrandUnknownTitle,
        Resources.Strings.DiagBrandUnknownMessage,
        Resources.Strings.DiagBrandUnknownFix,
        Severity.Information,
        Impact.Hygiene);

    public static IReadOnlyList<RuleMetadata> All { get; } = [BrandUnknown, RuleFailed];

    private static RuleMetadata Create(string id, string title, string message, string fix, Severity severity, Impact impact) => new()
    {
        Id = id,
        Title = title,
        Requirement = title,
        PackId = "l3lint.engine",
        PackVersion = EngineInfo.Version,
        Level = RuleLevel.General,
        Severity = severity,
        Confidence = Confidence.Policy,
        Impact = impact,
        Check = new RuleCheck("engine", [], []),
        Message = message,
        Fix = fix,
        Sources = [new Uri(EngineInfo.DocsUrl + "articles/output-formats.html")],
    };
}
