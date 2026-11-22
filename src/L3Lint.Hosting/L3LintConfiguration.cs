using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L3Lint.Core;
using L3Lint.Core.Model;
using L3Lint.Core.Schemas;
using L3Lint.Core.Yaml;

namespace L3Lint.Hosting;

/// <summary>
/// A parsed <c>.l3lint.yaml</c> (Interfaces tab, "Configuration file"): team defaults so CI and laptops lint the
/// same way.
/// </summary>
/// <remarks>
/// The file is validated against <c>config.schema.json</c>; unknown keys are errors. Relative paths resolve from
/// the folder that holds the file. Values here sit below command-line flags and payload values in precedence.
/// </remarks>
public sealed record L3LintConfiguration
{
    /// <summary>The file name l3lint looks for.</summary>
    public const string FileName = ".l3lint.yaml";

    /// <summary>Gets the full path of the file, or <see langword="null"/> for built-in defaults.</summary>
    public string? Path { get; init; }

    /// <summary>Gets the folder relative paths resolve from.</summary>
    public string BaseDirectory { get; init; } = Directory.GetCurrentDirectory();

    /// <summary>Gets the default brand: auto, a brand name, or all.</summary>
    public string? Brand { get; init; }

    /// <summary>Gets the default card product.</summary>
    public CardProduct? CardProduct { get; init; }

    /// <summary>Gets the default MCC.</summary>
    public string? Mcc { get; init; }

    /// <summary>Gets the default merchant country.</summary>
    public string? MerchantCountry { get; init; }

    /// <summary>Gets the processor profile name or file path.</summary>
    public string? Processor { get; init; }

    /// <summary>Gets a value indicating whether single-processor rules keep their catalog severity.</summary>
    public bool StrictProfiles { get; init; }

    /// <summary>Gets the input format.</summary>
    public string? Format { get; init; }

    /// <summary>Gets the CSV column map path, resolved against <see cref="BaseDirectory"/>.</summary>
    public string? CsvMap { get; init; }

    /// <summary>Gets the ISO 4217 currency for payloads that carry none (ADR-0056).</summary>
    public string? DefaultCurrency { get; init; }

    /// <summary>Gets the input limits.</summary>
    public Core.Input.InputLimits Limits { get; init; } = Core.Input.InputLimits.Default;

    /// <summary>Gets rules to run exclusively.</summary>
    public IReadOnlyList<string> OnlyRules { get; init; } = [];

    /// <summary>Gets rules to skip.</summary>
    public IReadOnlyList<string> DisabledRules { get; init; } = [];

    /// <summary>Gets severity overrides.</summary>
    public IReadOnlyDictionary<string, Severity> Severity { get; init; } = new Dictionary<string, Severity>();

    /// <summary>Gets placeholder terms to add.</summary>
    public IReadOnlyList<string> PlaceholdersAdd { get; init; } = [];

    /// <summary>Gets placeholder terms to remove.</summary>
    public IReadOnlyList<string> PlaceholdersRemove { get; init; } = [];

    /// <summary>Gets the batch thresholds.</summary>
    public BatchOptions Batch { get; init; } = new();

    /// <summary>Gets the arithmetic settings.</summary>
    public ArithmeticOptions Arithmetic { get; init; } = new();

    /// <summary>Gets a value indicating whether a card number stops processing.</summary>
    public bool StrictPci { get; init; }

    /// <summary>Gets canonical pointers exempt from card-number detection.</summary>
    public IReadOnlyList<string> PciAllowFields { get; init; } = [];

    /// <summary>Gets the fail-on level: error, warning, information or none.</summary>
    public string? FailOn { get; init; }

    /// <summary>Gets output formats, each "kind" or "kind=path".</summary>
    public IReadOnlyList<string> Outputs { get; init; } = [];

    /// <summary>Gets the most findings to print.</summary>
    public int? MaxFindings { get; init; }

    /// <summary>Gets the rate table path, resolved against <see cref="BaseDirectory"/>.</summary>
    public string? Rates { get; init; }

    /// <summary>Gets a short hash of the file's content, used as the SARIF category; "default" without a file.</summary>
    public string Hash { get; init; } = "default";

    /// <summary>Gets the built-in defaults (no file).</summary>
    public static L3LintConfiguration Default { get; } = new();

    /// <summary>
    /// Finds the nearest <c>.l3lint.yaml</c>, walking up from a folder.
    /// </summary>
    /// <param name="startDirectory">Where to start.</param>
    /// <returns>The file path, or <see langword="null"/> when none exists up to the file system root.</returns>
    public static string? FindNearest(string startDirectory)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = System.IO.Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Loads and validates a configuration file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="ConfigurationException">The file cannot be read, is not valid YAML, or does not match <c>config.schema.json</c>.</exception>
    public static L3LintConfiguration Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var full = System.IO.Path.GetFullPath(path);
        string text;
        try
        {
            text = File.ReadAllText(full);
        }
        catch (IOException ex)
        {
            throw new ConfigurationException(Message(Resources.Strings.ConfigUnreadable, full, ex.Message), full, null, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new ConfigurationException(Message(Resources.Strings.ConfigUnreadable, full, ex.Message), full, null, ex);
        }

        return Parse(text, full);
    }

    /// <summary>
    /// Parses configuration text.
    /// </summary>
    /// <param name="yaml">The YAML.</param>
    /// <param name="path">The file path, for messages and relative paths.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="ConfigurationException">The text is not valid YAML or does not match <c>config.schema.json</c>.</exception>
    public static L3LintConfiguration Parse(string yaml, string path)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        ArgumentNullException.ThrowIfNull(path);
        JsonElement root;
        try
        {
            root = JsonElementFromYaml.Convert(YamlJson.Parse(yaml) ?? new System.Text.Json.Nodes.JsonObject());
        }
        catch (YamlFormatException ex)
        {
            throw new ConfigurationException(Message(Resources.Strings.ConfigYaml, path, ex.Line, ex.Column, ex.Message), path, null, ex);
        }

        var errors = SchemaValidator.For("config").Validate(root);
        if (errors.Count > 0)
        {
            throw new ConfigurationException(Message(Resources.Strings.ConfigInvalid, path, errors[0].Pointer, SchemaMessages.Describe(errors[0])), path, errors[0].Pointer);
        }

        var baseDirectory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory();
        var context = Object(root, "context");
        var inputs = Object(root, "inputs");
        var limits = inputs is { } i ? Object(i, "limits") : null;
        var rules = Object(root, "rules");
        var placeholders = rules is { } r ? Object(r, "placeholders") : null;
        var batch = rules is { } r2 ? Object(r2, "batch") : null;
        var arithmetic = Object(root, "arithmetic");
        var pci = Object(root, "pci");
        var output = Object(root, "output");

        return new L3LintConfiguration
        {
            Path = System.IO.Path.GetFullPath(path),
            BaseDirectory = baseDirectory,
            Brand = Text(context, "brand"),
            CardProduct = Text(context, "cardProduct") is { } product && CanonicalNames.TryParse(product, out CardProduct parsed) ? parsed : null,
            Mcc = Text(context, "mcc"),
            MerchantCountry = Text(context, "merchantCountry"),
            Processor = Text(context, "processor"),
            StrictProfiles = Flag(context, "strictProfiles"),
            Format = Text(inputs, "format"),
            CsvMap = Resolve(path, baseDirectory, Text(inputs, "csvMap"), "/inputs/csvMap"),
            DefaultCurrency = Text(inputs, "defaultCurrency"),
            Limits = new Core.Input.InputLimits
            {
                MaxFileBytes = Number(limits, "maxFileBytes") is { } bytes ? (long)bytes : Core.Input.InputLimits.DefaultMaxFileBytes,
                MaxLineItems = Number(limits, "maxLineItems") is { } lines ? (int)lines : Core.Input.InputLimits.DefaultMaxLineItems,
                MaxJsonDepth = Number(limits, "maxJsonDepth") is { } depth ? (int)depth : Core.Input.InputLimits.DefaultMaxJsonDepth,
            },
            OnlyRules = Strings(rules, "only"),
            DisabledRules = Strings(rules, "disable"),
            Severity = rules is { } r3 && r3.TryGetProperty("severity", out var severity)
                ? severity.EnumerateObject().ToDictionary(p => p.Name, p => ParseSeverity(p.Value.GetString()!), StringComparer.Ordinal)
                : new Dictionary<string, Severity>(),
            PlaceholdersAdd = Strings(placeholders, "add"),
            PlaceholdersRemove = Strings(placeholders, "remove"),
            Batch = new BatchOptions
            {
                MinTransactions = Number(batch, "minTransactions") is { } min ? (int)min : 20,
                SameValueShare = Number(batch, "sameValueShare") ?? 0.80m,
            },
            Arithmetic = new ArithmeticOptions
            {
                LineTolerance = Number(arithmetic, "lineTolerance"),
                TotalTolerance = Number(arithmetic, "totalTolerance"),
                ToleranceMode = Text(arithmetic, "toleranceMode") == "accumulated" ? ToleranceMode.Accumulated : ToleranceMode.Strict,
                Rounding = Text(arithmetic, "rounding") == "toEven" ? MidpointRounding.ToEven : MidpointRounding.AwayFromZero,
            },
            StrictPci = Flag(pci, "strict"),
            PciAllowFields = Strings(pci, "allowFields"),
            FailOn = Text(output, "failOn"),
            Outputs = [.. Strings(output, "formats").Select((o, index) => ResolveOutput(path, baseDirectory, o, "/output/formats/" + index.ToString(CultureInfo.InvariantCulture)))],
            MaxFindings = Number(output, "maxFindings") is { } max ? (int)max : null,
            Rates = Resolve(path, baseDirectory, Text(root, "rates"), "/rates"),
            Hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(yaml.Replace("\r\n", "\n", StringComparison.Ordinal))))[..12],
        };
    }

    private static Severity ParseSeverity(string text) =>
        WireNames.TryParseSeverity(text, out var severity) ? severity : Core.Severity.Error;

    private static string ResolveOutput(string configPath, string baseDirectory, string output, string pointer)
    {
        var equals = output.IndexOf('=', StringComparison.Ordinal);
        return equals < 0 ? output : output[..(equals + 1)] + Resolve(configPath, baseDirectory, output[(equals + 1)..], pointer);
    }

    private static string? Resolve(string configPath, string baseDirectory, string? path, string pointer)
    {
        if (path is null)
        {
            return null;
        }

        try
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, path));
        }
        catch (ArgumentException ex)
        {
            throw new ConfigurationException(Message(Resources.Strings.ConfigInvalid, configPath, pointer, Resources.Strings.ConfigBadPath), configPath, pointer, ex);
        }
    }

    private static JsonElement? Object(JsonElement? parent, string name) =>
        parent is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string? Text(JsonElement? parent, string name) =>
        parent is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Flag(JsonElement? parent, string name) =>
        parent is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static decimal? Number(JsonElement? parent, string name) =>
        parent is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null;

    private static string[] Strings(JsonElement? parent, string name) =>
        parent is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Select(v => v.GetString()!)]
            : [];

    private static string Message(string template, params object?[] args) => string.Format(CultureInfo.InvariantCulture, template, args);
}
