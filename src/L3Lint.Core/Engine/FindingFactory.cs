using L3Lint.Core.Rules;

namespace L3Lint.Core.Engine;

/// <summary>
/// Pipeline stage 9: turns drafts into findings with effective severity, rendered message, source path,
/// location, mechanical patch and sources (ADR-0016, ADR-0022).
/// </summary>
internal static class FindingFactory
{
    public static Finding Build(in FindingDraft draft, TransactionState state, CompiledRule? compiled, DocumentContext document)
    {
        var rule = draft.Rule;
        var run = state.Run;
        var severity = draft.Severity ?? rule.Severity;
        if (compiled?.SeverityCap is { } cap && severity > cap)
        {
            severity = cap;
        }

        if (rule.Profiles.Count > 0 && !run.Options.StrictProfiles && severity == Severity.Error
            && !rule.Profiles.Contains(run.Profile.Id, StringComparer.Ordinal))
        {
            severity = Severity.Warning;
        }

        severity = run.Selection.Override(rule.Id, severity);

        var template = draft.MessageVariant is { } variant && rule.Messages.TryGetValue(variant, out var alternative) ? alternative : rule.Message;
        var resolved = draft.SourcePointer is null ? state.Paths.Resolve(draft.Path) : null;
        var source = draft.SourcePointer ?? resolved?.Pointer;
        var location = draft.Location ?? resolved?.Location;
        if (location is null && source is not null && document.Document is { } raw && (source.Length == 0 || source.StartsWith('/')))
        {
            location = raw.Locate(source);
        }

        location ??= document.Document is { Kind: Input.InputKind.Csv } ? new Input.SourceLocation(document.FirstLine, 1) : null;

        var patch = draft.Fix is { } fix && document.PatchRoot.ValueKind != System.Text.Json.JsonValueKind.Undefined
            ? PatchTranslator.Translate(fix, state.Paths, document.PatchRoot, document.TargetsCanonical)
            : null;

        return new Finding
        {
            RuleId = rule.Id,
            Severity = severity,
            Impact = rule.Impact,
            Brand = rule.Brand,
            TransactionIndex = draft.TransactionIndex ?? state.TransactionIndex,
            Path = new FieldPath(draft.Path, source),
            Location = location,
            Message = MessageRenderer.Render(template, draft.Arguments, draft.Actual, draft.Expected),
            Expected = draft.Expected,
            Actual = draft.Actual,
            Fix = rule.Fix,
            Patch = patch,
            NeedsHuman = draft.Fix?.NeedsHuman ?? false,
            Confidence = rule.Confidence,
            Sources = rule.Sources,
        };
    }

    /// <summary>Drops findings replaced by a more specific rule at the same path (ADR-0032).</summary>
    public static List<FindingDraft> ApplySupersession(List<FindingDraft> drafts)
    {
        HashSet<(string RuleId, string Path)>? removed = null;
        foreach (var draft in drafts)
        {
            foreach (var superseded in draft.Rule.Supersedes)
            {
                (removed ??= []).Add((superseded, draft.Path));
            }
        }

        return removed is null ? drafts : [.. drafts.Where(d => !removed.Contains((d.Rule.Id, d.Path)))];
    }
}
