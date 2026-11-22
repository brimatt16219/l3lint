using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using L3Lint.Core.Checks;
using L3Lint.Core.Engine;
using L3Lint.Core.Model;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Core.Packs;

/// <summary>
/// Loads rule packs (FR-019): verifies the manifest hashes, validates the YAML against
/// <c>rulepack.schema.json</c>, reads the lists, then compiles each rule. Any problem is a
/// <see cref="RulePackException"/> (exit code 3). Packs never run code: <c>custom</c> checks resolve only to
/// compiled classes (NFR-006).
/// </summary>
internal static partial class RulePackLoader
{
    private const string RulesSuffix = ".rules.yaml";

    public static LoadedPack Load(IRulePackSource source)
    {
        var origin = source.Origin;
        if (!source.Files.Contains(PackManifest.FileName, StringComparer.Ordinal))
        {
            throw Error(Format(Resources.Strings.PackNoManifest, origin), origin, PackManifest.FileName, null);
        }

        PackManifest manifest;
        try
        {
            using var stream = source.Open(PackManifest.FileName);
            manifest = PackManifest.Read(stream);
        }
        catch (JsonException ex)
        {
            throw new RulePackException(Format(Resources.Strings.PackBadManifest, origin, ex.Message), origin, PackManifest.FileName, null, ex);
        }

        VerifyHashes(source, manifest);

        var rulesFiles = source.Files.Where(f => f.EndsWith(RulesSuffix, StringComparison.Ordinal) && !f.Contains('/', StringComparison.Ordinal)).ToList();
        if (rulesFiles.Count != 1)
        {
            throw Error(Resources.Strings.PackRulesFileCount, origin, null, null);
        }

        var rulesFile = rulesFiles[0];
        JsonElement root;
        try
        {
            using var stream = source.Open(rulesFile);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            root = JsonElementFromYaml.Convert(YamlJson.Parse(reader.ReadToEnd()));
        }
        catch (YamlFormatException ex)
        {
            throw new RulePackException(Format(Resources.Strings.PackYamlError, rulesFile, ex.Line, ex.Column, ex.Message), origin, rulesFile, null, ex);
        }

        var errors = SchemaValidator.For("rulepack").Validate(root);
        if (errors.Count > 0)
        {
            var first = errors[0];
            throw new RulePackException(Format(Resources.Strings.PackSchemaError, rulesFile, first.Pointer, SchemaMessages.Describe(first)), origin, rulesFile, first.Pointer);
        }

        var header = root.GetProperty("pack");
        var id = header.GetProperty("id").GetString()!;
        var version = header.GetProperty("version").GetString()!;
        if (id != manifest.Pack || version != manifest.Version)
        {
            throw Error(Format(Resources.Strings.PackManifestMismatch, manifest.Pack, manifest.Version, id, version), origin, PackManifest.FileName, null);
        }

        var lists = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var partial = new List<string>();
        if (header.TryGetProperty("lists", out var listElement))
        {
            foreach (var list in listElement.EnumerateObject())
            {
                var path = list.Value.ValueKind == JsonValueKind.String ? list.Value.GetString()! : list.Value.GetProperty("path").GetString()!;
                if (list.Value.ValueKind == JsonValueKind.Object && list.Value.TryGetProperty("status", out var status) && status.GetString() == "partial")
                {
                    partial.Add(list.Name);
                }

                if (!source.Files.Contains(path, StringComparer.Ordinal))
                {
                    throw Error(Format(Resources.Strings.PackMissingFile, path), origin, rulesFile, "/pack/lists/" + list.Name);
                }

                using var stream = source.Open(path);
                lists[list.Name] = PackListReader.Read(stream);
            }
        }

        return new LoadedPack
        {
            Source = source,
            Id = id,
            Version = version,
            Brand = ParseBrand(header.GetProperty("brand").GetString()!),
            PublishedOn = ParseDate(header.GetProperty("publishedOn").GetString()!),
            RulesFile = rulesFile,
            Root = root,
            Lists = lists,
            PartialLists = partial,
        };
    }

    public static IReadOnlyList<CompiledRule> Compile(LoadedPack pack, CheckRegistry registry, ListLookup lists)
    {
        var rules = new List<CompiledRule>();
        var index = 0;
        foreach (var element in pack.Root.GetProperty("rules").EnumerateArray())
        {
            var pointer = "/rules/" + index.ToString(CultureInfo.InvariantCulture);
            index++;
            try
            {
                rules.Add(CompileRule(pack, element, registry, lists));
            }
            catch (FormatException ex)
            {
                throw new RulePackException(Format(Resources.Strings.PackRuleError, pack.RulesFile, pointer, ex.Message), pack.Source.Origin, pack.RulesFile, pointer, ex);
            }
            catch (ArgumentException ex)
            {
                throw new RulePackException(Format(Resources.Strings.PackRuleError, pack.RulesFile, pointer, ex.Message), pack.Source.Origin, pack.RulesFile, pointer, ex);
            }
        }

        return rules;
    }

    public static RulePackInfo Describe(LoadedPack pack, IReadOnlyList<CompiledRule> rules) => new(
        pack.Id,
        pack.Version,
        pack.Brand,
        pack.PublishedOn,
        rules.Count,
        rules.SelectMany(r => r.Metadata.Sources).Select(u => u.AbsoluteUri).Distinct(StringComparer.Ordinal).Count(),
        rules.Select(r => r.Metadata.EffectiveFrom).Where(d => d is not null).Min(),
        rules.Any(r => r.Metadata.EffectiveUntil is null) ? null : rules.Select(r => r.Metadata.EffectiveUntil).Max(),
        pack.Source.Origin);

    private static CompiledRule CompileRule(LoadedPack pack, JsonElement rule, CheckRegistry registry, ListLookup lists)
    {
        var id = rule.GetProperty("id").GetString()!;
        var expectedScope = pack.Brand switch
        {
            CardBrand.Visa => "VISA",
            CardBrand.Mastercard => "MC",
            CardBrand.Amex => "AMEX",
            CardBrand.Discover => "DISC",
            _ => "GEN",
        };
        if (!id.StartsWith(expectedScope + "-", StringComparison.Ordinal))
        {
            throw new FormatException(Format(Resources.Strings.PackRuleScope, id, pack.Id, expectedScope));
        }

        var checkElement = rule.GetProperty("check");
        var kind = checkElement.GetProperty("kind").GetString()!;
        IRuleCheck? check = null;
        IBatchCheck? batch = null;
        if (kind == "custom")
        {
            var name = checkElement.GetProperty("check").GetString()!;
            if (registry.TryGetBatch(name, out var batchCheck))
            {
                batch = batchCheck;
            }
            else if (registry.TryGetCustom(name, out var custom))
            {
                check = new CustomCheckAdapter(custom, []);
            }
            else
            {
                throw new FormatException(Format(Resources.Strings.PackUnknownCheck, name));
            }
        }
        else
        {
            check = CompileCheck(kind, checkElement);
        }

        var applicability = ReadApplicability(rule);
        var compiledApplicability = new CompiledApplicability(applicability);
        var listNames = compiledApplicability.ListNames.Concat(check?.ListNames ?? []).ToList();
        foreach (var listName in listNames)
        {
            if (!lists.Exists(listName))
            {
                throw new FormatException(Format(Resources.Strings.PackUnknownList, listName));
            }
        }

        var message = rule.GetProperty("message").GetString()!;
        var messages = rule.TryGetProperty("messages", out var variants)
            ? variants.EnumerateObject().ToDictionary(v => v.Name, v => v.Value.GetString()!, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var template in messages.Values.Append(message))
        {
            ValidatePlaceholders(template);
        }

        var metadata = new RuleMetadata
        {
            Id = id,
            Title = rule.GetProperty("title").GetString()!,
            Requirement = Optional(rule, "requirement"),
            Rationale = Optional(rule, "rationale"),
            Notes = Optional(rule, "notes"),
            PackId = pack.Id,
            PackVersion = pack.Version,
            Brand = pack.Brand,
            Level = ParseLevel(rule.GetProperty("level").GetString()!),
            Severity = SeverityNames.Parse(rule.GetProperty("severity").GetString()!),
            Confidence = ParseConfidence(rule.GetProperty("confidence").GetString()!),
            Impact = ParseImpact(rule.GetProperty("impact").GetString()!),
            EffectiveFrom = rule.TryGetProperty("effective", out var effective) && effective.TryGetProperty("from", out var from) ? ParseDate(from.GetString()!) : null,
            EffectiveUntil = rule.TryGetProperty("effective", out var effective2) && effective2.TryGetProperty("until", out var until) ? ParseDate(until.GetString()!) : null,
            AppliesTo = applicability,
            Profiles = Strings(rule, "profiles"),
            Supersedes = Strings(rule, "supersedes"),
            Check = check?.Description ?? new RuleCheck("custom", [], [new("check", checkElement.GetProperty("check").GetString()!)]),
            Message = message,
            Messages = messages,
            Fix = rule.GetProperty("fix").GetString()!,
            Sources = [.. rule.GetProperty("sources").EnumerateArray().Select(s => new Uri(s.GetString()!))],
            Examples = ReadExamples(pack, rule),
            IsBatch = batch is not null,
        };

        var cap = listNames.Any(lists.IsPartial) ? Severity.Warning : (Severity?)null;
        return new CompiledRule(metadata, compiledApplicability, check, batch, cap);
    }

    private static IRuleCheck CompileCheck(string kind, JsonElement check)
    {
        IReadOnlyList<FieldSelector> Selectors()
        {
            if (check.TryGetProperty("field", out var field))
            {
                return [FieldSelector.Parse(field.GetString()!)];
            }

            return [.. check.GetProperty("fields").EnumerateArray().Select(f => FieldSelector.Parse(f.GetString()!))];
        }

        bool Flag(string name) => check.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
        int? Int(string name) => check.TryGetProperty(name, out var value) ? value.GetInt32() : null;
        decimal? Number(string name) => check.TryGetProperty(name, out var value) ? value.GetDecimal() : null;
        string? Text(string name) => check.TryGetProperty(name, out var value) ? value.GetString() : null;

        switch (kind)
        {
            case "required":
                return new RequiredCheck(Selectors(), Flag("allowZero"), Flag("positiveOnly"), Text("report") == "once");
            case "maxLength":
                if (check.TryGetProperty("limits", out var limits))
                {
                    return new MaxLengthCheck([.. limits.EnumerateArray().Select(l => new LengthLimit(
                        FieldSelector.Parse(l.GetProperty("field").GetString()!),
                        l.TryGetProperty("max", out var max) ? max.GetInt32() : null,
                        l.TryGetProperty("profileKey", out var key) ? key.GetString() : null,
                        l.TryGetProperty("brands", out var brands) ? [.. brands.EnumerateArray().Select(b => ParseBrand(b.GetString()!) ?? CardBrand.Unknown)] : []))]);
                }

                return new MaxLengthCheck([new LengthLimit(FieldSelector.Parse(Text("field")!), Int("max"), Text("profileKey"), [])]);
            case "pattern":
                return new PatternCheck(Selectors(), CompileRegex(Text("regex")!));
            case "range":
                return new RangeCheck(FieldSelector.Parse(Text("field")!), Number("min"), Number("max"), Flag("exclusive"));
            case "inList":
                var listNames = check.TryGetProperty("list", out var list)
                    ? (list.ValueKind == JsonValueKind.String ? [list.GetString()!] : list.EnumerateArray().Select(l => l.GetString()!).ToList())
                    : [];
                var values = check.TryGetProperty("values", out var inline) ? inline.EnumerateArray().Select(v => v.GetString()!).ToList() : [];
                var allowMissing = !check.TryGetProperty("allowMissing", out var allow) || allow.ValueKind == JsonValueKind.True;
                return new InListCheck(FieldSelector.Parse(Text("field")!), listNames, values, Flag("caseInsensitive"), Text("mode") == "deny", allowMissing);
            case "ratio":
                return new RatioCheck(FieldSelector.Parse(Text("numerator")!), FieldSelector.Parse(Text("denominator")!), Number("min")!.Value, Number("max")!.Value);
            case "maxCount":
                var collection = Text("collection")!;
                if (collection is not ("/lineItems" or "/amex/adviceText"))
                {
                    throw new FormatException(Format(Resources.Strings.PackBadCollection, collection));
                }

                return new MaxCountCheck(collection, Int("max"), Text("profileKey"));
            case "anyOf":
                return new AnyOfCheck(Selectors());
            default:
                throw new FormatException(Format(Resources.Strings.PackUnknownKind, kind));
        }
    }

    private static Regex CompileRegex(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException ex)
        {
            throw new FormatException(Format(Resources.Strings.PackBadRegex, pattern, ex.Message), ex);
        }
    }

    private static RuleApplicability ReadApplicability(JsonElement rule)
    {
        if (!rule.TryGetProperty("appliesTo", out var applies))
        {
            return RuleApplicability.Always;
        }

        IReadOnlyList<string> ListRefs(string name) => applies.TryGetProperty(name, out var value)
            ? (value.ValueKind == JsonValueKind.String ? [value.GetString()!] : [.. value.EnumerateArray().Select(v => v.GetString()!)])
            : [];

        IReadOnlyList<RuleCondition> Conditions(string name) => applies.TryGetProperty(name, out var value)
            ? [.. value.EnumerateArray().Select(ReadCondition)]
            : [];

        return new RuleApplicability
        {
            CardProducts = applies.TryGetProperty("cardProducts", out var products)
                ? [.. products.EnumerateArray().Select(p => CanonicalNames.TryParse(p.GetString(), out CardProduct product) ? product : CardProduct.Unknown)]
                : [],
            MccIn = ListRefs("mccIn"),
            MccNotIn = ListRefs("mccNotIn"),
            When = Conditions("when"),
            Unless = Conditions("unless"),
        };
    }

    private static RuleCondition ReadCondition(JsonElement condition)
    {
        var field = condition.GetProperty("field").GetString()!;
        foreach (var property in condition.EnumerateObject())
        {
            if (property.Name == "field")
            {
                continue;
            }

            IReadOnlyList<string> operands = property.Value.ValueKind == JsonValueKind.Array
                ? [.. property.Value.EnumerateArray().Select(ScalarText)]
                : [ScalarText(property.Value)];
            return new RuleCondition(field, property.Name, operands);
        }

        throw new FormatException(Format(Resources.Strings.PackBadCondition, field));
    }

    private static string ScalarText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => value.GetDecimal().ToString(CultureInfo.InvariantCulture),
        _ => value.GetString() ?? string.Empty,
    };

    private static RuleExamples? ReadExamples(LoadedPack pack, JsonElement rule)
    {
        if (!rule.TryGetProperty("examples", out var examples))
        {
            return null;
        }

        string ReadFile(string path)
        {
            if (!pack.Source.Files.Contains(path, StringComparer.Ordinal))
            {
                throw new FormatException(Format(Resources.Strings.PackMissingFile, path));
            }

            using var stream = pack.Source.Open(path);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        var passFile = examples.GetProperty("pass").GetString()!;
        var failFile = examples.GetProperty("fail").GetString()!;
        var context = RuleExampleContext.None;
        if (examples.TryGetProperty("context", out var ctx))
        {
            context = new RuleExampleContext(
                ctx.TryGetProperty("asOf", out var asOf) ? ParseDate(asOf.GetString()!) : null,
                ctx.TryGetProperty("processor", out var processor) ? processor.GetString() : null,
                ctx.TryGetProperty("strictProfiles", out var strict) && strict.ValueKind == JsonValueKind.True,
                ctx.TryGetProperty("rates", out var rates) ? ReadFile(rates.GetString()!) : null);
        }

        return new RuleExamples(
            passFile,
            failFile,
            ReadFile(passFile),
            ReadFile(failFile),
            Optional(examples, "passNote"),
            Optional(examples, "failNote"),
            context);
    }

    private static void VerifyHashes(IRulePackSource source, PackManifest manifest)
    {
        var files = source.Files.Where(f => f != PackManifest.FileName).ToHashSet(StringComparer.Ordinal);
        foreach (var listed in manifest.Files.Keys)
        {
            if (!files.Contains(listed))
            {
                throw Error(Format(Resources.Strings.PackManifestMissingFile, listed), source.Origin, PackManifest.FileName, null);
            }
        }

        foreach (var file in files.Order(StringComparer.Ordinal))
        {
            if (!manifest.Files.TryGetValue(file, out var expected))
            {
                throw Error(Format(Resources.Strings.PackManifestExtraFile, file), source.Origin, file, null);
            }

            using var stream = source.Open(file);
            var actual = PackHashing.Sha256(stream);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw Error(Format(Resources.Strings.PackChecksumMismatch, file), source.Origin, file, null);
            }
        }
    }

    private static void ValidatePlaceholders(string template)
    {
        foreach (Match match in PlaceholderPattern().Matches(template))
        {
            var name = match.Groups[1].Value;
            if (!CheckArguments.Known.Contains(name))
            {
                throw new FormatException(Format(Resources.Strings.PackUnknownPlaceholder, name));
            }
        }
    }

    private static IReadOnlyList<string> Strings(JsonElement rule, string name) =>
        rule.TryGetProperty(name, out var value) ? [.. value.EnumerateArray().Select(v => v.GetString()!)] : [];

    private static string? Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static CardBrand? ParseBrand(string text) =>
        text == "general" ? null : CanonicalNames.TryParse(text, out CardBrand brand) ? brand : null;

    private static DateOnly ParseDate(string text) => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static RuleLevel ParseLevel(string text) => text switch
    {
        "L2" => RuleLevel.L2,
        "L3" => RuleLevel.L3,
        _ => RuleLevel.General,
    };

    private static Confidence ParseConfidence(string text) => text switch
    {
        "public-brand" => Confidence.PublicBrand,
        "processor-doc" => Confidence.ProcessorDoc,
        "inferred" => Confidence.Inferred,
        _ => Confidence.Policy,
    };

    private static Impact ParseImpact(string text) => text switch
    {
        "blocksLevel3" => Impact.BlocksLevel3,
        "blocksLevel2" => Impact.BlocksLevel2,
        "riskVisaValidation" => Impact.RiskVisaValidation,
        "truncation" => Impact.Truncation,
        "noBenefit" => Impact.NoBenefit,
        _ => Impact.Hygiene,
    };

    private static RulePackException Error(string message, string origin, string? file, string? pointer) => new(message, origin, file, pointer);

    private static string Format(string template, params object?[] args) => string.Format(CultureInfo.InvariantCulture, template, args);

    [GeneratedRegex(@"\{([A-Za-z]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}
