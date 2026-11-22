using System.Globalization;
using System.Text.Json.Nodes;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Yaml;

namespace L3Lint.Adapters.Tests.Support;

/// <summary>One adapter fixture: the input, its expect.yaml and the hand-written canonical transactions.</summary>
internal sealed class Fixture
{
    private Fixture(string folder, string name, JsonObject expect)
    {
        Folder = folder;
        Name = name;
        Expect = expect;
    }

    /// <summary>Gets the adapter folder name, for example "stripe".</summary>
    public string Folder { get; }

    /// <summary>Gets the fixture base name, for example "01-complete-level3".</summary>
    public string Name { get; }

    public JsonObject Expect { get; }

    public string Id => Folder + "/" + Name;

    public string Format => Expect["format"]!.GetValue<string>();

    public bool IsTie => Format == "tie";

    public IReadOnlyList<string> Candidates => Expect["candidates"] is JsonArray list ? [.. list.Select(c => c!.GetValue<string>())] : [];

    public bool IsCsv => File.Exists(PathOf(".csv"));

    public JsonObject Context => Expect["context"]!.AsObject();

    public static string FixtureRoot => Repo.PathOf("fixtures", "adapters");

    /// <summary>Gets every fixture id as "folder/name", sorted.</summary>
    public static IReadOnlyList<string> AllIds() =>
    [
        .. Directory.EnumerateFiles(FixtureRoot, "*.expect.yaml", SearchOption.AllDirectories)
            .Select(path => Path.GetFileName(Path.GetDirectoryName(path)!) + "/" + Path.GetFileName(path)[..^".expect.yaml".Length])
            .Order(StringComparer.Ordinal),
    ];

    public static Fixture Load(string id)
    {
        var slash = id.IndexOf('/', StringComparison.Ordinal);
        var folder = id[..slash];
        var name = id[(slash + 1)..];
        var yaml = File.ReadAllText(Path.Combine(FixtureRoot, folder, name + ".expect.yaml"));
        return new Fixture(folder, name, YamlJson.Parse(yaml)!.AsObject());
    }

    public string PathOf(string suffix) => Path.Combine(FixtureRoot, Folder, Name + suffix);

    public RawDocument Document() => IsCsv
        ? RawDocument.FromCsv(File.ReadAllBytes(PathOf(".csv")), Name + ".csv")
        : RawDocument.ParseJson(File.ReadAllBytes(PathOf(".json")), Name + ".json");

    public AdapterOptions AdapterOptions() => new()
    {
        DefaultCurrency = Context["defaultCurrency"]?.GetValue<string>(),
        CsvMap = IsCsv ? File.ReadAllText(PathOf(".map.yaml")) : null,
        CsvMapName = IsCsv ? Name + ".map.yaml" : null,
    };

    public LintOptions LintOptions()
    {
        CanonicalNames.TryParse(Context["brand"]!.GetValue<string>(), out CardBrand brand).ShouldBeTrue();
        CanonicalNames.TryParse(Context["cardProduct"]!.GetValue<string>(), out CardProduct product).ShouldBeTrue();
        return new LintOptions
        {
            Brand = brand,
            CardProduct = product,
            Mcc = Context["mcc"]!.GetValue<string>(),
            AsOf = DateOnly.ParseExact(Context["asOf"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Format = Format,
            Adapter = AdapterOptions(),
        };
    }

    /// <summary>Gets the expected canonical transactions (one object, or an array for CSV).</summary>
    public IReadOnlyList<JsonNode> ExpectedCanonical()
    {
        var file = Expect["canonical"]!.GetValue<string>();
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureRoot, Folder, file)))!;
        return node is JsonArray array ? [.. array.Select(n => n!)] : [node];
    }

    /// <summary>Gets the expected findings as "transaction ruleId path" strings, sorted.</summary>
    public IReadOnlyList<string> ExpectedFindings() =>
    [
        .. (Expect["expect"]!["findings"]!.AsArray())
            .Select(f => Key(f!["transaction"]?.GetValue<long>() ?? 0, f["ruleId"]!.GetValue<string>(), f["path"]!.GetValue<string>()))
            .Order(StringComparer.Ordinal),
    ];

    public static string Key(long transaction, string ruleId, string path) =>
        transaction.ToString(CultureInfo.InvariantCulture) + " " + ruleId + " " + path;

    public override string ToString() => Id;
}
