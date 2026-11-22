using System.Collections.Frozen;
using L3Lint.Core.Fields;
using L3Lint.Core.Profiles;

namespace L3Lint.Core.Engine;

/// <summary>Options resolved once per run: profile, lists, placeholder terms, rule selection and overrides.</summary>
internal sealed class RunContext
{
    public required LintOptions Options { get; init; }

    public required ProcessorProfile Profile { get; init; }

    public required ListLookup Lists { get; init; }

    public required FrozenSet<string> Placeholders { get; init; }

    public required RuleSelection Selection { get; init; }

    public required IReadOnlyList<PointerPattern> PciAllowFields { get; init; }

    public required DateOnly Today { get; init; }
}
