using System.Globalization;
using System.Text.RegularExpressions;
using L3Lint.Core;
using L3Lint.Core.Adapters;
using L3Lint.Core.Estimates;
using L3Lint.Core.Model;
using L3Lint.Core.Profiles;
using L3Lint.Core.Rules;

namespace L3Lint.Hosting;

/// <summary>
/// Combines the config file, command-line (or tool-call) overrides and the files they name into
/// <see cref="ResolvedSettings"/> (FR-009, FR-018, Interfaces tab "Precedence").
/// </summary>
/// <remarks>
/// Flags beat the payload, which beats the config file, which beats built-in defaults. Bad option values are
/// usage errors (<see cref="UsageException"/>, exit 2); invalid referenced files are configuration errors
/// (<see cref="ConfigurationException"/>, exit 3).
/// </remarks>
public static partial class SettingsResolver
{
    /// <summary>The format names <c>--format</c> accepts.</summary>
    public static IReadOnlyList<string> Formats { get; } = ["auto", "canonical", "stripe", "adyen", "braintree", "cybersource", "authorizenet", "checkoutcom", "csv"];

    /// <summary>The output kinds <c>--output</c> accepts.</summary>
    public static IReadOnlyList<string> OutputKinds { get; } = ["text", "json", "sarif", "junit", "markdown"];

    /// <summary>
    /// Resolves settings.
    /// </summary>
    /// <param name="configuration">The config file, or <see cref="L3LintConfiguration.Default"/>.</param>
    /// <param name="overrides">Flag values.</param>
    /// <param name="workingDirectory">The folder relative flag paths resolve from.</param>
    /// <returns>The settings.</returns>
    /// <exception cref="UsageException">An option value is invalid or a named file cannot be read.</exception>
    /// <exception cref="ConfigurationException">A named profile or rate table is invalid.</exception>
    public static ResolvedSettings Resolve(L3LintConfiguration configuration, LintOverrides overrides, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(overrides);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var format = (overrides.Format ?? configuration.Format ?? "auto").ToLowerInvariant();
        if (!Formats.Contains(format))
        {
            throw Usage(Resources.Strings.UsageFormat, format, string.Join(", ", Formats));
        }

        var currency = (overrides.Currency ?? configuration.DefaultCurrency)?.ToUpperInvariant();
        if (currency is not null && !Iso4217.TryGetCurrency(currency, out _))
        {
            throw Usage(Resources.Strings.UsageCurrency, currency);
        }

        var csvMapPath = overrides.CsvMap is { } map ? Path.GetFullPath(Path.Combine(workingDirectory, map)) : configuration.CsvMap;
        var csvMap = csvMapPath is null ? null : ReadText(csvMapPath, overrides.CsvMap is null ? configuration : null);
        if (format == "csv" && csvMap is null)
        {
            throw Usage(Resources.Strings.UsageMapRequired);
        }

        var (brand, allBrands) = ParseBrand(overrides.Brand, Resources.Strings.OptionBrand);
        var (defaultBrand, configAll) = ParseBrand(configuration.Brand, "context.brand");
        var profile = ResolveProfile(overrides.Processor ?? configuration.Processor, workingDirectory, configuration, overrides.Processor is not null);
        var ratesFromFlag = overrides.Rates is not null;
        var ratesPath = overrides.Rates is { } rates ? Path.GetFullPath(Path.Combine(workingDirectory, rates)) : configuration.Rates;
        var severity = new Dictionary<string, Severity>(configuration.Severity, StringComparer.Ordinal);
        foreach (var entry in overrides.Severity)
        {
            var (rule, level) = ParseSeverityOverride(entry);
            severity[rule] = level;
        }

        var options = new LintOptions
        {
            Brand = brand,
            AllBrands = allBrands || (overrides.Brand is null && configAll),
            CardProduct = ParseProduct(overrides.CardProduct),
            Mcc = ValidateMcc(overrides.Mcc),
            MerchantCountry = ValidateCountry(overrides.MerchantCountry),
            Defaults = new ContextDefaults
            {
                Brand = defaultBrand,
                CardProduct = configuration.CardProduct,
                Mcc = configuration.Mcc,
                MerchantCountry = configuration.MerchantCountry,
            },
            Processor = profile?.Id,
            ProcessorProfile = profile,
            AsOf = overrides.AsOf,
            FailOn = ParseFailOn(overrides.FailOn ?? configuration.FailOn ?? "error"),
            Arithmetic = configuration.Arithmetic,
            OnlyRules = ValidateSelectors(overrides.Only.Count > 0 ? overrides.Only : configuration.OnlyRules),
            DisabledRules = ValidateSelectors([.. configuration.DisabledRules, .. overrides.Disable]),
            SeverityOverrides = severity,
            StrictProfiles = overrides.StrictProfiles || configuration.StrictProfiles,
            StrictPci = overrides.StrictPci || configuration.StrictPci,
            PciAllowFields = configuration.PciAllowFields,
            PlaceholdersAdd = configuration.PlaceholdersAdd,
            PlaceholdersRemove = configuration.PlaceholdersRemove,
            Batch = configuration.Batch,
            Rates = ratesPath is null ? null : ParseRates(ratesPath, ratesFromFlag ? null : configuration),
            Format = format == "auto" ? null : format,
            Adapter = new AdapterOptions { CsvMap = csvMap, CsvMapName = csvMapPath, Limits = configuration.Limits, Verbose = overrides.Verbose, DefaultCurrency = currency },
            Limits = configuration.Limits,
        };

        var outputs = (overrides.Outputs.Count > 0 ? overrides.Outputs : configuration.Outputs)
            .Select(o => OutputTarget.Parse(o, workingDirectory))
            .ToList();
        foreach (var output in outputs.Where(o => !OutputKinds.Contains(o.Format)))
        {
            throw Usage(Resources.Strings.UsageOutput, output.Format, string.Join(", ", OutputKinds));
        }

        var lockFile = new[] { configuration.Path is null ? null : configuration.BaseDirectory, workingDirectory }
            .OfType<string>()
            .Select(d => Path.Combine(d, LockFile.FileName))
            .FirstOrDefault(File.Exists);

        var notes = new List<string>();
        if (configuration.Path is { } configPath)
        {
            notes.Add(string.Format(CultureInfo.InvariantCulture, Resources.Strings.NoteConfiguration, configPath));
        }

        return new ResolvedSettings
        {
            Options = options,
            Outputs = outputs.Count == 0 ? [new OutputTarget("text", null)] : outputs,
            MaxFindings = overrides.MaxFindings ?? configuration.MaxFindings,
            ProfileName = profile?.Id ?? "generic",
            ConfigHash = configuration.Hash,
            LocalPacks = [.. overrides.RulePacks.Select(p => Path.GetFullPath(Path.Combine(workingDirectory, p)))],
            LockFile = lockFile,
            Notes = notes,
        };
    }

    /// <summary>Parses a fail-on level.</summary>
    /// <param name="text">error, warning, information or none.</param>
    /// <returns>The severity, or <see langword="null"/> for none.</returns>
    /// <exception cref="UsageException">The text is not a level.</exception>
    public static Severity? ParseFailOn(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.Equals(text, "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return WireNames.TryParseSeverity(text, out var severity) ? severity : throw Usage(Resources.Strings.UsageFailOn, text);
    }

    private static (CardBrand? Brand, bool All) ParseBrand(string? text, string option)
    {
        if (text is null || string.Equals(text, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return (null, false);
        }

        if (string.Equals(text, "all", StringComparison.OrdinalIgnoreCase))
        {
            return (null, true);
        }

        return CanonicalNames.TryParse(text.ToLowerInvariant(), out CardBrand brand) && brand != CardBrand.Unknown
            ? (brand, false)
            : throw Usage(Resources.Strings.UsageBrand, option, text);
    }

    private static CardProduct? ParseProduct(string? text) =>
        text is null ? null
        : CanonicalNames.TryParse(text, out CardProduct product) && product != CardProduct.Unknown ? product
        : throw Usage(Resources.Strings.UsageCardProduct, text);

    private static string? ValidateMcc(string? mcc) =>
        mcc is null || MccPattern().IsMatch(mcc) ? mcc : throw Usage(Resources.Strings.UsageMcc, mcc);

    private static string? ValidateCountry(string? country) =>
        country is null || CountryPattern().IsMatch(country) ? country : throw Usage(Resources.Strings.UsageCountry, country);

    private static HashSet<string> ValidateSelectors(IEnumerable<string> selectors)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selector in selectors)
        {
            var upper = selector.Trim().ToUpperInvariant();
            if (!Core.Engine.RuleSelection.IsValidPattern(upper))
            {
                throw Usage(Resources.Strings.UsageRuleSelector, selector);
            }

            set.Add(upper);
        }

        return set;
    }

    private static (string Rule, Severity Level) ParseSeverityOverride(string entry)
    {
        var equals = entry.IndexOf('=', StringComparison.Ordinal);
        if (equals <= 0 || !WireNames.TryParseSeverity(entry[(equals + 1)..].Trim(), out var level))
        {
            throw Usage(Resources.Strings.UsageSeverity, entry);
        }

        var rule = entry[..equals].Trim().ToUpperInvariant();
        return Core.Engine.RuleSelection.IsValidPattern(rule) ? (rule, level) : throw Usage(Resources.Strings.UsageSeverity, entry);
    }

    private static ProcessorProfile? ResolveProfile(string? processor, string workingDirectory, L3LintConfiguration configuration, bool fromFlag)
    {
        if (processor is null)
        {
            return null;
        }

        if (ProcessorProfiles.TryGet(processor, out var profile))
        {
            return profile;
        }

        var source = fromFlag ? null : configuration;
        var path = Path.GetFullPath(Path.Combine(fromFlag ? workingDirectory : configuration.BaseDirectory, processor));
        if (!File.Exists(path))
        {
            var message = string.Format(CultureInfo.InvariantCulture, Resources.Strings.UsageProcessor, processor, string.Join(", ", ProcessorProfiles.All.Select(p => p.Id)));
            throw source?.Path is { } file ? new ConfigurationException(message, file, "/context/processor") : new UsageException(message);
        }

        try
        {
            return ProcessorProfiles.Parse(ReadText(path, source), path);
        }
        catch (RulePackException ex)
        {
            throw new ConfigurationException(ex.Message, path, ex.Pointer, ex);
        }
    }

    private static RateTable ParseRates(string path, L3LintConfiguration? source)
    {
        try
        {
            return RateTable.Parse(ReadText(path, source), path);
        }
        catch (RulePackException ex)
        {
            throw new ConfigurationException(ex.Message, path, ex.Pointer, ex);
        }
    }

    // A file named by a flag that cannot be read is a usage error (exit 2); one named by the config file is a
    // configuration error (exit 3) that points at the config file.
    private static string ReadText(string path, L3LintConfiguration? source)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = string.Format(CultureInfo.InvariantCulture, Resources.Strings.UsageUnreadable, path, ex.Message);
            throw source?.Path is { } file ? new ConfigurationException(message, file, null, ex) : new UsageException(message, ex);
        }
    }

    private static UsageException Usage(string template, params object?[] args) =>
        new(string.Format(CultureInfo.InvariantCulture, template, args));

    [GeneratedRegex("^[0-9]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex MccPattern();

    [GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex CountryPattern();
}
