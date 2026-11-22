using System.Text.Json;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Packs;

/// <summary>A pack whose files passed the manifest and schema checks, before its rules are compiled.</summary>
internal sealed class LoadedPack
{
    public required IRulePackSource Source { get; init; }

    public required string Id { get; init; }

    public required string Version { get; init; }

    public required CardBrand? Brand { get; init; }

    public required DateOnly PublishedOn { get; init; }

    public required string RulesFile { get; init; }

    public required JsonElement Root { get; init; }

    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Lists { get; init; }

    public required IReadOnlyList<string> PartialLists { get; init; }
}
